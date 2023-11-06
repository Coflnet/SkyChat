
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
using Coflnet.Kafka;
using System.Collections.Concurrent;

namespace Coflnet.Sky.Chat.Services;

public interface IMuteService
{
    Task<Mute> MuteUser(Mute mute, string clientToken);
    Task<UnMute> UnMuteUser(UnMute unmute, string clientToken);
}

public interface IMuteList
{
    Task<IEnumerable<Mute>> GetMutes(string authorization);
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

public class MuteService : IMuteService, IMuteList
{
    private ChatDbContext db;
    private ChatBackgroundService backgroundService;
    private static ConcurrentDictionary<string, Mute> muteCache = new ConcurrentDictionary<string, Mute>();

    public MuteService(ChatDbContext db, ChatBackgroundService backgroundService)
    {
        this.db = db;
        this.backgroundService = backgroundService;
    }

    public Task<IEnumerable<Mute>> GetMutes(string authorization)
    {
        var _ = backgroundService.GetClient(authorization);
        return Task.FromResult(muteCache.Values.AsEnumerable());
    }

    /// <summary>
    /// Add a mute to an user
    /// </summary>
    /// <param name="mute"></param>
    /// <param name="clientToken"></param>
    /// <returns></returns>
    public async Task<Mute> MuteUser(Mute mute, string clientToken)
    {
        if (mute == null)
            throw new ApiException("invalid_mute", "The mute was null");
        ArgumentException.ThrowIfNullOrEmpty(mute.Uuid, nameof(mute.Uuid));
        var client = backgroundService.GetClient(clientToken);
        if (client.Name.Contains("tfm") && mute.Message.Contains("AUTOMUTE"))
            return mute;
        mute.ClientId = client.Id;
        var minTime = DateTime.Now - TimeSpan.FromHours(6);
        var recentMutes = await db.Mute.Where(u => u.Muter == mute.Muter && !u.Status.HasFlag(MuteStatus.CANCELED) && u.Timestamp > minTime).ToListAsync();
        if (recentMutes.Count > 5 && mute.Muter != "384a029294fc445e863f2c42fe9709cb")
            throw new ApiException("too_many_mutes", "You have muted too many people recently");
        var muteText = mute.Message + mute.Reason;
        if (muteText.Contains("rule ") || mute.Expires == default)
        {
            // rule violation
            var mutes = await db.Mute.Where(u => u.Uuid == mute.Uuid && !u.Status.HasFlag(MuteStatus.CANCELED)).ToListAsync();
            var firstMessage = await db.Messages.Where(u => u.Sender == mute.Uuid).OrderBy(m => m.Id).FirstOrDefaultAsync();
            if (firstMessage == null)
                throw new ApiException("invalid_mute", "The user has no previous messages");
            double nextLength = GetMuteTime(mutes, firstMessage.Timestamp);
            mute.Expires = DateTime.UtcNow + TimeSpan.FromHours(nextLength);
        }
        db.Add(mute);
        await db.SaveChangesAsync();
        await UpdateMuteCache();
        return mute;
    }

    private async Task UpdateMuteCache()
    {
        var allMutes = await db.Mute.Where(u => u.Expires > DateTime.UtcNow && !u.Status.HasFlag(MuteStatus.CANCELED)).ToListAsync();
        // put longest mutes in cache
        muteCache.Clear();
        foreach (var item in allMutes)
        {
            if (muteCache.TryGetValue(item.Uuid, out var currentMute))
            {
                if (currentMute.Expires < item.Expires)
                    muteCache[item.Uuid] = item;
            }
            else
                muteCache[item.Uuid] = item;
        }
    }

    public static double GetMuteTime(List<Mute> mutes, DateTime firstMessageTime)
    {
        var currentTime = 1;
        foreach (var item in mutes)
        {
            var text = (item.Reason + item.Message).ToLower();
            if (text.StartsWith("tfm"))
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
        await UpdateMuteCache();
        return unmute;
    }

    /// <summary>
    /// Retrieves a mute or null
    /// </summary>
    /// <param name="uuid"></param>
    /// <returns></returns>
    public async Task<Mute> GetMute(string uuid)
    {
        if (muteCache.Count == 0)
            await UpdateMuteCache();
        if (muteCache.TryGetValue(uuid, out var mute))
        {
            if (mute.Expires > DateTime.UtcNow && !mute.Status.HasFlag(MuteStatus.CANCELED))
                return mute;
        }
        return null;
        return await db.Mute.Where(u => u.Uuid == uuid && u.Expires > DateTime.UtcNow && !u.Status.HasFlag(MuteStatus.CANCELED)).OrderByDescending(m => m.Expires).FirstOrDefaultAsync();
    }

    private async Task DisableMute(UnMute unmute, ModelClient client, Mute mute)
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
    private KafkaCreator kafkaCreator;
    static bool createdTopic = false;
    public MuteProducer(IConfiguration config, IPlayerNameApi playerNameApi, KafkaCreator kafkaCreator)
    {
        this.config = config;
        this.playerNameApi = playerNameApi;
        this.kafkaCreator = kafkaCreator;
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
        if (!createdTopic)
        {
            createdTopic = true;
            await kafkaCreator.CreateTopicIfNotExist(config["TOPICS:DISCORD_MESSAGE"]);
        }
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
        var producer = kafkaCreator.BuildProducer<string, string>();
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