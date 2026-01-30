using System.Threading.Tasks;
using Xunit;

namespace ChatGPTSharp.Tests
{
    public class ChatSessionIntegrationTests
    {
        [Fact]
        public async Task ChatSession_TracksConversationState()
        {
            var client = TestConfig.CreateClient();
            var session = client.CreateSession(systemPrompt: "Reply with OK only.");

            var first = await session.SendAsync("OK?");
            var firstConversationId = session.ConversationId;
            var firstMessageId = session.LastMessageId;

            Assert.False(string.IsNullOrWhiteSpace(firstConversationId));
            Assert.False(string.IsNullOrWhiteSpace(firstMessageId));
            Assert.Equal(firstConversationId, first.ConversationId);
            Assert.Equal(firstMessageId, first.MessageId);

            var second = await session.SendAsync("OK again?");

            Assert.Equal(firstConversationId, session.ConversationId);
            Assert.False(string.IsNullOrWhiteSpace(session.LastMessageId));
            Assert.NotEqual(firstMessageId, session.LastMessageId);
            Assert.Equal(firstConversationId, second.ConversationId);
            Assert.Equal(session.LastMessageId, second.MessageId);
        }
    }
}
