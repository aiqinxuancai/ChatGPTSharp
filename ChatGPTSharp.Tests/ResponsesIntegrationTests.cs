using System.Collections.Generic;
using System.Threading.Tasks;
using ChatGPTSharp.Model;
using Xunit;

namespace ChatGPTSharp.Tests
{
    public class ResponsesIntegrationTests
    {
        [Fact]
        public async Task CreateResponse_ReturnsOutput()
        {
            var client = TestConfig.CreateClient();

            var contents = new List<MessageContent> { MessageContent.FromText("Reply with OK only.") };
            var result = await client.CreateResponseAsync(
                contents,
                instructions: "Reply with OK only.",
                store: false);

            Assert.False(string.IsNullOrWhiteSpace(result.Id));
            Assert.False(string.IsNullOrWhiteSpace(result.Model));
            Assert.False(string.IsNullOrWhiteSpace(result.GetOutputText()));
        }

        [Fact]
        public async Task StreamResponse_ReturnsDelta()
        {
            var client = TestConfig.CreateClient();

            var request = new ResponseRequest
            {
                Input = MessageContent.BuildResponseInput(
                    RoleType.User,
                    new List<MessageContent> { MessageContent.FromText("Stream OK only.") }),
                Instructions = "Stream OK only.",
                Store = false
            };

            var text = string.Empty;
            var done = false;

            await foreach (var chunk in client.StreamResponseAsync(request))
            {
                if (!string.IsNullOrEmpty(chunk.DeltaText))
                {
                    text += chunk.DeltaText;
                }
                done = done || chunk.IsDone;
            }

            Assert.True(done);
            Assert.False(string.IsNullOrWhiteSpace(text));
        }

        [Fact]
        public async Task Conversations_CreateAndAddItems()
        {
            var client = TestConfig.CreateClient();

            var conversation = await client.CreateConversationAsync();
            Assert.False(string.IsNullOrWhiteSpace(conversation.Id));

            var items = MessageContent.BuildResponseInput(
                RoleType.User,
                new List<MessageContent> { MessageContent.FromText("Hello from conversation.") });

            var result = await client.AddConversationItemsAsync(conversation.Id!, items);
            Assert.NotNull(result);
        }
    }
}
