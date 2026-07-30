using System;
using Coflnet.Sky.Chat.Models;
using NUnit.Framework;

namespace Coflnet.Sky.Chat.Services;

public class ModerationRetentionTests
{
    [TestCase(null, 96)]
    [TestCase("", 96)]
    [TestCase("invalid", 96)]
    [TestCase("0", 96)]
    [TestCase("168", 96)]
    [TestCase("24", 24)]
    public void QuoteRetentionFailsClosedAtNinetySixHours(
        string configured,
        int expectedHours)
    {
        Assert.That(
            ChatBackgroundService.GetModerationQuoteRetention(configured),
            Is.EqualTo(TimeSpan.FromHours(expectedHours)));
    }

    [Test]
    public void WebhooksRequireHttpsAndAuthentication()
    {
        var client = new ModelClient
        {
            WebHook = "https://example.test/chat"
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                ChatBackgroundService.IsWebhookAllowed(null),
                Is.False);
            Assert.That(
                ChatBackgroundService.IsWebhookAllowed(client),
                Is.False);
            client.WebhookAuth = "secret";
            Assert.That(
                ChatBackgroundService.IsWebhookAllowed(client),
                Is.True);
            client.WebHook = "http://example.test/chat";
            Assert.That(
                ChatBackgroundService.IsWebhookAllowed(client),
                Is.False);
        });
    }

    [Test]
    public void RuleMetadataSurvivesQuotedTextRemoval()
    {
        var mute = new Mute
        {
            Rule = MuteRule.BE_NICE,
            Message = null,
            Reason = null,
            Expires = DateTime.MaxValue
        };

        Assert.That(
            MuteService.GetMuteTime(new() { mute }),
            Is.EqualTo(10));
    }

    [Test]
    public void QuoteCleanupSelectsOnlyExpiredRowsWithFreeText()
    {
        var cutoff = DateTime.UtcNow.AddHours(-96);
        var isExpired =
            ChatBackgroundService.QuotedEvidenceExpired(cutoff).Compile();

        Assert.Multiple(() =>
        {
            Assert.That(isExpired(new Mute
            {
                Timestamp = cutoff.AddTicks(-1),
                Message = "quoted message"
            }), Is.True);
            Assert.That(isExpired(new Mute
            {
                Timestamp = cutoff,
                Message = "quoted message"
            }), Is.False);
            Assert.That(isExpired(new Mute
            {
                Timestamp = cutoff.AddTicks(-1),
                Message = null,
                Reason = null
            }), Is.False);
        });
    }

    [Test]
    public void SanctionCleanupMatchesTheExistingRepeatWindowBoundary()
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-MuteService.RepeatSanctionWindowDays);
        var isOutsideRepeatWindow =
            ChatBackgroundService
                .ExpiredSanctionOutsideRepeatWindow(cutoff)
                .Compile();

        Assert.Multiple(() =>
        {
            Assert.That(isOutsideRepeatWindow(new Mute
            {
                Expires = cutoff.AddTicks(-1)
            }), Is.True);
            Assert.That(isOutsideRepeatWindow(new Mute
            {
                Expires = cutoff
            }), Is.True);
            Assert.That(isOutsideRepeatWindow(new Mute
            {
                Expires = cutoff.AddTicks(1)
            }), Is.False);
            Assert.That(isOutsideRepeatWindow(new Mute
            {
                Expires = now.AddDays(1)
            }), Is.False);
            Assert.That(isOutsideRepeatWindow(new Mute
            {
                Expires = DateTime.MaxValue
            }), Is.False);
        });
    }

    [Test]
    public void TransitionalSkyModReasonIsNormalizedExactly()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                MuteService.GetRule(new Mute { Reason = "1 player message uuid" }),
                Is.EqualTo(MuteRule.BE_NICE));
            Assert.That(
                MuteService.GetRule(new Mute { Reason = "2 player message uuid" }),
                Is.EqualTo(MuteRule.NO_UNSOLICITED_ADVERTISING));
            Assert.That(
                MuteService.GetRule(new Mute
                {
                    Rule = MuteRule.BE_NICE,
                    Reason = "2 ignored by the new client"
                }),
                Is.EqualTo(MuteRule.BE_NICE));
            Assert.That(
                MuteService.GetRule(new Mute
                {
                    Reason = "2 player quoted rule 1 uuid",
                    Message = "Violating rule 2 with \"quoted rule 1\""
                }),
                Is.EqualTo(MuteRule.NO_UNSOLICITED_ADVERTISING));
        });
    }
}
