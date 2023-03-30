
using System.Threading.Tasks;
using Coflnet.Sky.Chat.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Coflnet.Sky.PlayerName.Client.Api;

namespace Coflnet.Sky.Chat.Services;

public interface IMuteService
{
    Task<Mute> MuteUser(Mute mute, string clientToken);
    Task<UnMute> UnMuteUser(UnMute unmute, string clientToken);
}

public class TfmMuteService : IMuteService
{
    private ChatBackgroundService backgroundService;
    private ILogger<TfmMuteService> logger;

    public TfmMuteService(ChatBackgroundService backgroundService, ILogger<TfmMuteService> logger)
    {
        this.backgroundService = backgroundService;
        this.logger = logger;
    }

    public async Task<Mute> MuteUser(Mute mute, string clientToken)
    {
        var client = backgroundService.GetClient(clientToken);
        if (client.Name.Contains("tfm"))
            return mute;

        var apiClient = new RestClient("https://sky.coflnet.com/");
        var request = new RestRequest("tfm/chat/mute", Method.Post);
        var tfm = backgroundService.GetClientByName("tfm");
        if (tfm == null)
            return mute;
        var parameters = new
        {
            uuid = mute.Uuid,
            muter = 267680402594988033,
            until = (long)(mute.Expires - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds,
            reason = mute.Message,
            key = tfm.WebhookAuth,
        };
        request.AddJsonBody(parameters);
        _ = Task.Run(async () =>
        {
            try
            {
                var response = await apiClient.ExecuteAsync(request).ConfigureAwait(false);
                logger.LogInformation("mute response: {response}", response.Content);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while sending mute to tfm");
            }
        }).ConfigureAwait(false);
        return mute;
    }
    public async Task<UnMute> UnMuteUser(UnMute unmute, string clientToken)
    {
        var client = backgroundService.GetClient(clientToken);
        if (client.Name.Contains("tfm"))
            return unmute;
        var apiClient = new RestClient("https://sky.coflnet.com");
        var request = new RestRequest("tfm/chat/unmute", Method.Post);
        var tfm = backgroundService.GetClientByName("tfm");
        if (tfm == null)
            return unmute;
        var parameters = new
        {
            uuid = unmute.Uuid,
            unmuter = 267680402594988033,
            reason = unmute.Reason,
            key = tfm.WebhookAuth,
        };
        request.AddJsonBody(parameters);
        var response = await apiClient.ExecuteAsync(request);
        logger.LogInformation("unmute response: {response}", response.Content);
        return unmute;
    }
}

public class MuteService : IMuteService
{
    private ChatDbContext db;
    private ChatBackgroundService backgroundService;

    public MuteService(ChatDbContext db, ChatBackgroundService backgroundService)
    {
        this.db = db;
        this.backgroundService = backgroundService;
    }

    /// <summary>
    /// Add a mute to an user
    /// </summary>
    /// <param name="mute"></param>
    /// <param name="clientToken"></param>
    /// <returns></returns>
    public async Task<Mute> MuteUser(Mute mute, string clientToken)
    {
        var client = backgroundService.GetClient(clientToken);
        mute.ClientId = client.Id;
        var muteText = mute.Message + mute.Reason;
        if (muteText.Contains("rule "))
        {
            // rule violation
            var mutes = await db.Mute.Where(u => u.Uuid == mute.Uuid && !u.Status.HasFlag(MuteStatus.CANCELED)).ToListAsync();
            var firstMessage = await db.Messages.Where(u => u.Sender == mute.Uuid).OrderBy(m => m.Id).FirstOrDefaultAsync();
            double nextLength = GetMuteTime(mutes, firstMessage.Timestamp);
            mute.Expires = DateTime.UtcNow + TimeSpan.FromHours(nextLength);
        }
        db.Add(mute);
        await db.SaveChangesAsync();
        return mute;
    }

    public static double GetMuteTime(List<Mute> mutes, DateTime firstMessageTime)
    {
        var currentTime = 1;
        foreach (var item in mutes)
        {
            var text = item.Reason + item.Message;
            if (text.ToLower().StartsWith("tfm"))
                continue;
            if (text.Contains("rule 1"))
                currentTime *= 10;
            else if (text.Contains("rule 2"))
                currentTime *= 3;
        }
        var timeSinceJoin = firstMessageTime - DateTime.UtcNow;
        var reduction = Math.Max(1, Math.Pow(0.7, timeSinceJoin.TotalDays / 30));
        var nextLength = currentTime / (reduction);
        return Math.Max(nextLength, 1);
    }

    public async Task<UnMute> UnMuteUser(UnMute unmute, string clientToken)
    {
        var client = backgroundService.GetClient(clientToken);
        var mute = await GetMute(unmute.Uuid);
        if (mute == null)
            throw new ApiException("no_mute_found", $"There was no active mute for the user {unmute.Uuid}");

        await DisableMute(unmute, client, mute);
        return unmute;
    }

    /// <summary>
    /// Retrieves a mute or null
    /// </summary>
    /// <param name="uuid"></param>
    /// <returns></returns>
    public async Task<Mute> GetMute(string uuid)
    {
        return await db.Mute.Where(u => u.Uuid == uuid && u.Expires > DateTime.UtcNow && !u.Status.HasFlag(MuteStatus.CANCELED)).OrderByDescending(m => m.Expires).FirstOrDefaultAsync();
    }

    private async Task DisableMute(UnMute unmute, Client client, Mute mute)
    {
        mute.Status |= MuteStatus.CANCELED;
        mute.UnMuteClientId = client.Id;
        mute.UnMuter = unmute.UnMuter;
        await db.SaveChangesAsync();
    }
}


public class MuteProducer : IMuteService
{
    IConfiguration config;
    private IPlayerNameApi playerNameApi;
    public MuteProducer(IConfiguration config, IPlayerNameApi playerNameApi)
    {
        this.config = config;
        this.playerNameApi = playerNameApi;
    }

    public async Task<Mute> MuteUser(Mute mute, string clientToken)
    {
        string name = await GetName(mute.Uuid);
        var message = $"🔇 User {name} was muted by {await GetName(mute.Muter)} for `{mute.Reason}` until <t:{new DateTimeOffset(mute.Expires).ToUnixTimeSeconds()}> message: {mute.Message}";
        await ProduceMessage(message);
        return mute;
    }

    private async Task ProduceMessage(string message)
    {
        using var producer = GetProducer();
        await producer.ProduceAsync(config["TOPICS:DISCORD_MESSAGE"], new() { Value = JsonConvert.SerializeObject(new { message, channel = "mutes" }) }).ConfigureAwait(false);
    }

    /// <summary>
    /// Produce unmute
    /// </summary>
    /// <param name="unmute"></param>
    /// <param name="clientToken"></param>
    /// <returns></returns>
    public async Task<UnMute> UnMuteUser(UnMute unmute, string clientToken)
    {
        string name = await GetName(unmute.Uuid);
        var message = $"🔈 User {name} was unmuted by {await GetName(unmute.UnMuter)} for `{unmute.Reason}`";
        await ProduceMessage(message);
        return unmute;
    }

    private IProducer<string, string> GetProducer()
    {
        ProducerConfig producerConfig = new ProducerConfig
        {
            BootstrapServers = config["KAFKA_HOST"],
            LingerMs = 0
        };
        var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        return producer;
    }

    private async Task<string> GetName(string id)
    {
        try
        {
            var name = await playerNameApi.PlayerNameNameUuidGetAsync(id);
            if (!string.IsNullOrEmpty(name))
                return name;
        }
        catch (Exception)
        {
            Console.WriteLine("could not get name for mute " + id);
        }

        return id;
    }
}