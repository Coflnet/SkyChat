using System;
using NUnit.Framework;

namespace Coflnet.Sky.Chat.Services;

/// <summary>
/// Tests for <see cref="ChatService"/>
/// </summary>
public class ChatServiceTests
{
    private Models.Mute rule1 = new() { Rule = Models.MuteRule.BE_NICE, Expires = DateTime.MaxValue };
    private Models.Mute rule2 = new() { Rule = Models.MuteRule.NO_UNSOLICITED_ADVERTISING, Expires = DateTime.MaxValue };
    /// <summary>
    /// New user should receive about one hour
    /// </summary>
    [Test]
    public void MuteTimeNewuser()
    {
        var time = MuteService.GetMuteTime(new());
        Assert.That(time, Is.EqualTo(1).Within(0.00001));
    }

    /// <summary>
    /// Two times rule 1 = 100 hours
    /// </summary>
    [Test]
    public void MuteTimeNewTwoTimesRule1()
    {
        var time = MuteService.GetMuteTime(new() { rule1, rule1 });
        Assert.That(time, Is.EqualTo(100).Within(0.00001));
    }
    [Test]
    public void MuteTimeNewThreeTimesRule2()
    {
        var time = MuteService.GetMuteTime(new() { rule2,rule2,rule2 });
        Assert.That(time, Is.EqualTo(27).Within(0.00001));
    }
    [Test]
    public void MuteTimeRuleOne()
    {
        var time = MuteService.GetMuteTime(new() { rule1 });
        Assert.That(time, Is.EqualTo(10).Within(0.00001));
    }

    /// <summary>
    /// DSA Art. 17 requires the mute notice to state nature/scope/duration of the
    /// restriction, the specific rule violated (by number and title) and a redress line.
    /// </summary>
    [Test]
    public void MuteMessageContainsDurationRuleAndRedressLine()
    {
        var mute = new Models.Mute
        {
            Rule = Models.MuteRule.BE_NICE,
            Expires = DateTime.UtcNow.AddHours(2),
            Muter = "some-moderator-uuid"
        };

        var message = ChatService.GetMuteMessage(mute);

        Assert.Multiple(() =>
        {
            Assert.That(message, Does.Contain(mute.Expires.ToString("F")), "must state until when the mute lasts");
            Assert.That(message, Does.Contain("public chat"), "must state the scope of the restriction");
            Assert.That(message, Does.Contain("rule 1"), "must name the rule number");
            Assert.That(message, Does.Contain("Be nice"), "must name the rule title");
            Assert.That(message, Does.Contain("support@coflnet.com"), "must offer a redress channel");
            Assert.That(message, Does.Contain("Your right to go to court is unaffected"));
        });
    }

    [Test]
    public void MuteMessageNamesRule2ByNumberAndTitle()
    {
        var mute = new Models.Mute
        {
            Rule = Models.MuteRule.NO_UNSOLICITED_ADVERTISING,
            Expires = DateTime.UtcNow.AddHours(1),
            Muter = "some-moderator-uuid"
        };

        var message = ChatService.GetMuteMessage(mute);

        Assert.That(message, Does.Contain("rule 2"));
        Assert.That(message, Does.Contain("Do not advertise something nobody asked for"));
    }

    /// <summary>
    /// Automated mutes are created by the repeat-offender filter in
    /// <see cref="ChatService"/> with <c>Muter == ChatService.AutomatedFilterMuterId</c>
    /// ("chat-service"). Any other <see cref="Models.Mute.Muter"/> value is a Minecraft
    /// uuid of a human moderator (e.g. set from <c>socket.SessionInfo.McUuid</c> by
    /// SkyModCommands' GlobalMuteCommand), since only moderators can call the mute
    /// endpoint that sets it. The notice must disclose which one applied.
    /// </summary>
    [Test]
    public void MuteMessageDisclosesAutomatedDecision()
    {
        var mute = new Models.Mute
        {
            Rule = Models.MuteRule.BE_NICE,
            Expires = DateTime.UtcNow.AddMinutes(15),
            Muter = ChatService.AutomatedFilterMuterId,
            Message = "You violated the chat rules"
        };

        var message = ChatService.GetMuteMessage(mute);

        Assert.That(message, Does.Contain("This decision was taken automatically by our chat filter."));
        Assert.That(message, Does.Not.Contain("human moderator"));
    }

    [Test]
    public void MuteMessageDisclosesHumanModeratorDecision()
    {
        var mute = new Models.Mute
        {
            Rule = Models.MuteRule.BE_NICE,
            Expires = DateTime.UtcNow.AddHours(10),
            Muter = "8f14e1c2f1a5497eabf3f4a0e5a7f2c1" // a moderator's Minecraft uuid
        };

        var message = ChatService.GetMuteMessage(mute);

        Assert.That(message, Does.Contain("The mute was issued by a human moderator."));
        Assert.That(message, Does.Not.Contain("automatically"));
    }

    /// <summary>
    /// After ChatBackgroundService's 96h retention sweep redacts Message to null, the
    /// notice must still be well formed and simply omit the quoted evidence.
    /// </summary>
    [Test]
    public void MuteMessageOmitsQuoteWhenMessageIsRedacted()
    {
        var redactedMute = new Models.Mute
        {
            Rule = Models.MuteRule.BE_NICE,
            Expires = DateTime.UtcNow.AddHours(5),
            Muter = "some-moderator-uuid",
            Message = null
        };

        var message = ChatService.GetMuteMessage(redactedMute);

        Assert.That(message, Does.Not.Contain("Message that triggered this"));
        Assert.That(message, Does.Contain("rule 1"), "the rule ground must still be present without the quote");
    }

    [Test]
    public void MuteMessageIncludesQuoteWhenMessageIsPresent()
    {
        var mute = new Models.Mute
        {
            Rule = Models.MuteRule.BE_NICE,
            Expires = DateTime.UtcNow.AddHours(5),
            Muter = "some-moderator-uuid",
            Message = "Violating rule 1 with \"go away noob\""
        };

        var message = ChatService.GetMuteMessage(mute);

        Assert.That(message, Does.Contain("Message that triggered this: \"Violating rule 1 with \"go away noob\"\""));
    }
}
