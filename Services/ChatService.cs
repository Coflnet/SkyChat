using System.Threading.Tasks;
using Coflnet.Sky.Chat.Models;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Newtonsoft.Json;
using RestSharp;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Coflnet.Sky.PlayerName.Client.Api;

namespace Coflnet.Sky.Chat.Services;

/// <summary>
/// Core service handling validation and distribution of messages
/// </summary>
public class ChatService
{
    private ChatDbContext db;
    private ConnectionMultiplexer connection;
    private ChatBackgroundService backgroundService;
    private IPlayerNameApi playerNameApi;
    private static ConcurrentQueue<DbMessage> recentMessages = new ConcurrentQueue<DbMessage>();
    static HashSet<string> BadWords = new() { " cock ", "penis ", " ass ", "b.com", "my ah", "/ah ", "/auction", "@everyone", "@here", " retard", " qf ", " kys ", "nigger ", "nigga ", " fag ", "faggot", "quickerflipper" };
    static Prometheus.Counter messagesSent = Prometheus.Metrics.CreateCounter("sky_chat_messages_sent", "Count of messages distributed");
    private ILogger<ChatService> Logger;
    private EmojiService emojiService;
    private MuteService muteService;

    /// <summary>
    /// Creates a new instance of <see cref="ChatService"/>
    /// </summary>
    /// <param name="db"></param>
    /// <param name="connection"></param>
    /// <param name="backgroundService"></param>
    /// <param name="logger"></param>
    /// <param name="emojiService"></param>
    /// <param name="muteService"></param>
    /// <param name="playerNameApi"></param>
    public ChatService(ChatDbContext db,
        ConnectionMultiplexer connection,
        ChatBackgroundService backgroundService,
        ILogger<ChatService> logger,
        EmojiService emojiService,
        MuteService muteService,
        IPlayerNameApi playerNameApi)
    {
        this.db = db;
        this.connection = connection;
        this.backgroundService = backgroundService;
        Logger = logger;
        this.emojiService = emojiService;
        this.muteService = muteService;
        this.playerNameApi = playerNameApi;
    }

    /// <summary>
    /// Send a new message
    /// </summary>
    /// <param name="message"></param>
    /// <param name="clientToken"></param>
    /// <returns></returns>
    public async Task<bool> SendMessage(ChatMessage message, string clientToken)
    {
        var client = backgroundService.GetClient(clientToken);
        if (String.IsNullOrEmpty(message.Uuid))
            throw new ApiException("invalid_uuid", "The uuid of the sending player has to be set");
        if (message.ClientName == null)
            message.ClientName = client.Name;
        if (message.ClientName != client.Name)
            throw new ApiException("token_mismatch", "Client name does not match with the provided token");

        ThrowIfSpam(message);
        var dbMessage = new DbMessage()
        {
            ClientId = client.Id,
            Content = message.Message,
            Sender = message.Uuid,
            Timestamp = DateTime.UtcNow
        };
        recentMessages.Enqueue(dbMessage);
        if (recentMessages.Count >= 10)
            recentMessages.TryDequeue(out _);
        try
        {
            await AssertMessageSendable(message);
        }
        catch (ApiException)
        {
            db.Messages.Add(dbMessage);
            await db.SaveChangesAsync();
            throw;
        }

        await FillName(message);

        var pubsub = connection.GetSubscriber();
        var original = message.Message;
        message.Message = emojiService.ReplaceIn(message.Message);
        var replaced = JsonConvert.SerializeObject(message);
        message.Message = original;
        var response = await pubsub.PublishAsync("chat", replaced);
        Console.WriteLine($"Sent message to {response} clients");
        db.Messages.Add(dbMessage);
        await db.SaveChangesAsync();
        _ = Task.Run(async () => await backgroundService.SendWebhooks(message));
        messagesSent.Inc();
        return true;
    }

    private async Task FillName(ChatMessage message)
    {
        var tries = 0;
        while (string.IsNullOrEmpty(message.Name))
        {
            var result = await playerNameApi.PlayerNameNameUuidGetAsync(message.Uuid);
            message.Name = result;
            if (tries++ > 3)
            {
                message.Name = "invalid name";
                break;
            }
        }
    }

    private static void ThrowIfSpam(ChatMessage message)
    {
        var wasLastMessage = recentMessages.OrderByDescending(m => m.Timestamp).Take(1).Where(f => f.Sender == message.Uuid && f.Content == message.Message).Any();
        var alreadySentLong = recentMessages.Where(f => f.Sender == message.Uuid && f.Content == message.Message && message.Message.Length > 6).Any();
        if (wasLastMessage || alreadySentLong)
            throw new ApiException("message_spam", "Please don't send the same message twice");
    }

    private async Task AssertMessageSendable(ChatMessage message)
    {
        Mute mute = await muteService.GetMute(message.Uuid);
        if (mute != default)
            throw new ApiException("user_muted", GetMuteMessage(mute));
        var normalizedMsg = message.Message.ToLower() + ' ';
        if (BadWords.Any(word => normalizedMsg.Contains(word)))
            throw new ApiException("bad_words", "message contains bad words and was denied");

        if (normalizedMsg.Contains(" binmaster"))
            throw new ApiException("illegal_script", "Binmaster violates the hypixel terms of service. Violating the TOS can get your account banned and wiped. Also writing about it in flipper chat gets you muted by TFM.");
    }

    private static string GetMuteMessage(Mute mute)
    {
        return $"You are muted until {mute.Expires.ToString("F")} ({(DateTime.UtcNow - mute.Expires).ToString("d'd 'h'h 'm'm 's's'")}) because {mute.Message ?? "you violated a rule"}";
    }


    /// <summary>
    /// Creates a new client, generates a new secure api key before saving it into the db
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    public async Task<Client> CreateClient(Client client)
    {
        if (await db.Clients.Where(c => c.Name == client.Name).AnyAsync())
            throw new ApiException("client_exists", "A client with the same name already exists");
        var key = System.Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
        Console.WriteLine("new key stars with " + key.Substring(0, 4));
        client.ApiKey = key;
        db.Add(client);
        await db.SaveChangesAsync();
        return client;
    }
}
