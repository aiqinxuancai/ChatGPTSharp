using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ChatGPTSharp.Model;
using Xunit;

namespace ChatGPTSharp.Tests
{
    public class ChatCompletionIntegrationTests
    {
        [Fact]
        public async Task SendMessage_ReturnsText()
        {
            var client = TestConfig.CreateClient();

            var result = await client.SendMessage("Reply with OK only.");

            Assert.False(string.IsNullOrWhiteSpace(result.Response));
            Assert.Contains("ok", result.Response!, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SendMessage_WithToolChoice_ProducesToolCall()
        {
            var client = TestConfig.CreateClient();

            var parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["location"] = new JsonObject
                    {
                        ["type"] = "string"
                    }
                },
                ["required"] = new JsonArray("location")
            };

            var tools = new List<ToolDefinition>
            {
                ToolDefinition.CreateFunction(
                    new ToolFunctionDefinition("get_time", "Get the time for a city", parameters))
            };

            var result = await client.SendMessage(
                "What time is it in Tokyo? Use the tool.",
                tools: tools,
                toolChoice: ToolChoice.ForFunction("get_time"));

            Assert.NotNull(result.ToolCalls);
            Assert.NotEmpty(result.ToolCalls!);
            Assert.Equal("get_time", result.ToolCalls!.First().Function.Name);
        }

        [Fact]
        public async Task SendMessageStream_ReturnsText()
        {
            var client = TestConfig.CreateClient();

            var chunks = new List<string>();
            var hasDone = false;

            await foreach (var chunk in client.SendMessageStream("Stream OK only."))
            {
                if (!string.IsNullOrEmpty(chunk.DeltaText))
                {
                    chunks.Add(chunk.DeltaText!);
                }
                hasDone = hasDone || chunk.IsDone;
            }

            var text = string.Concat(chunks);
            Assert.True(hasDone);
            Assert.False(string.IsNullOrWhiteSpace(text));
        }

        [Fact]
        public async Task SendMessageWithConversation_TracksIds()
        {
            var client = TestConfig.CreateClient();

            var first = await client.SendMessageWithConversation("Say hello.");
            Assert.False(string.IsNullOrWhiteSpace(first.ConversationId));
            Assert.False(string.IsNullOrWhiteSpace(first.MessageId));

            var second = await client.SendMessageWithConversation(
                "Say hello again.",
                conversationId: first.ConversationId!,
                parentMessageId: first.MessageId!);

            Assert.Equal(first.ConversationId, second.ConversationId);
            Assert.False(string.IsNullOrWhiteSpace(second.MessageId));
            Assert.NotEqual(first.MessageId, second.MessageId);
        }

        [Fact]
        public async Task SendMessageStreamWithConversation_ReturnsIds()
        {
            var client = TestConfig.CreateClient();

            string? conversationId = null;
            string? messageId = null;
            var chunks = new List<string>();

            await foreach (var chunk in client.SendMessageStreamWithConversation("Stream hello."))
            {
                conversationId ??= chunk.ConversationId;
                messageId ??= chunk.MessageId;
                if (!string.IsNullOrEmpty(chunk.DeltaText))
                {
                    chunks.Add(chunk.DeltaText!);
                }
            }

            Assert.False(string.IsNullOrWhiteSpace(conversationId));
            Assert.False(string.IsNullOrWhiteSpace(messageId));
            Assert.False(string.IsNullOrWhiteSpace(string.Concat(chunks)));
        }
    }
}
