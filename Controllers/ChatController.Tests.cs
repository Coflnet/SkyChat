using Coflnet.Sky.Chat.Controllers;
using NUnit.Framework;

namespace Coflnet.Sky.Chat;

public class ChatControllerTests
{
    [Test]
    public void DeletionTokenFailsClosed()
    {
        var valid = new string('a', 32);

        Assert.Multiple(() =>
        {
            Assert.That(ChatController.HasValidDeletionToken(null, valid), Is.False);
            Assert.That(ChatController.HasValidDeletionToken("", valid), Is.False);
            Assert.That(ChatController.HasValidDeletionToken("short", "short"), Is.False);
            Assert.That(ChatController.HasValidDeletionToken(valid, new string('b', 32)), Is.False);
            Assert.That(ChatController.HasValidDeletionToken(valid, valid), Is.True);
        });
    }
}
