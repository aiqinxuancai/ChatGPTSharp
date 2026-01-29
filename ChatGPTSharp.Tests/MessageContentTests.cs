using System.Text.Json.Nodes;
using ChatGPTSharp.Model;
using Xunit;

namespace ChatGPTSharp.Tests
{
    public class MessageContentTests
    {
        [Fact]
        public void TextContent_ToJsonObject()
        {
            var content = MessageContent.FromText("hello");
            var obj = content.ToJsonObject();

            Assert.Equal("text", obj["type"]?.GetValue<string>());
            Assert.Equal("hello", obj["text"]?.GetValue<string>());
        }

        [Fact]
        public void ImageContent_ToJsonObject_WithDetail()
        {
            var content = MessageContent.FromImageUrl("https://example.com/a.png", ImageDetailMode.High);
            var obj = content.ToJsonObject();

            Assert.Equal("image_url", obj["type"]?.GetValue<string>());
            var imageUrl = obj["image_url"] as JsonObject;
            Assert.NotNull(imageUrl);
            Assert.Equal("https://example.com/a.png", imageUrl?["url"]?.GetValue<string>());
            Assert.Equal("high", imageUrl?["detail"]?.GetValue<string>());
        }

        [Fact]
        public void AudioContent_ToResponseContentItem_DataUrl()
        {
            var dataUrl = "data:audio/mpeg;base64,AAAA";
            var content = MessageContent.FromAudioUrl(dataUrl);
            var obj = content.ToResponseContentItem();

            Assert.Equal("input_audio", obj["type"]?.GetValue<string>());
            var audio = obj["input_audio"] as JsonObject;
            Assert.NotNull(audio);
            Assert.Equal("AAAA", audio?["data"]?.GetValue<string>());
            Assert.Equal("mp3", audio?["format"]?.GetValue<string>());
        }
    }
}
