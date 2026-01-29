using ChatGPTSharp;
using Xunit;

namespace ChatGPTSharp.Tests
{
    public class ClientSettingsTests
    {
        [Fact]
        public void BaseUrl_NormalizesV1Suffix()
        {
            var settings = new ChatGPTClientSettings
            {
                BaseUrl = "https://api.openai.com/v1"
            };

            Assert.Equal("https://api.openai.com/v1/chat/completions", settings.CompletionsUrl);
            Assert.Equal("https://api.openai.com/v1/responses", settings.ResponsesUrl);
            Assert.Equal("https://api.openai.com/v1/conversations", settings.ConversationsUrl);
        }

        [Fact]
        public void BaseUrl_NormalizesFullEndpoint()
        {
            var settings = new ChatGPTClientSettings
            {
                BaseUrl = "https://api.openai.com/v1/chat/completions"
            };

            Assert.Equal("https://api.openai.com/v1/chat/completions", settings.CompletionsUrl);
            Assert.Equal("https://api.openai.com/v1/responses", settings.ResponsesUrl);
            Assert.Equal("https://api.openai.com/v1/conversations", settings.ConversationsUrl);
        }
    }
}
