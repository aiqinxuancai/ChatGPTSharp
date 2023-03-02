
namespace ChatGPTSharp.Model.ChatCompletions
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class ChatCompletionsResult
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("usage")]
        public Usage Usage { get; set; }

        [JsonProperty("choices")]
        public Choice[] Choices { get; set; }
    }

    public partial class Choice
    {
        [JsonProperty("message")]
        public ChatMessage Message { get; set; }

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }

        [JsonProperty("index")]
        public long Index { get; set; }
    }

    public partial class ChatMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public partial class Usage
    {
        [JsonProperty("prompt_tokens")]
        public long PromptTokens { get; set; }

        [JsonProperty("completion_tokens")]
        public long CompletionTokens { get; set; }

        [JsonProperty("total_tokens")]
        public long TotalTokens { get; set; }
    }

    public partial class ChatCompletionsResult
    {
        public static ChatCompletionsResult FromJson(string json) => JsonConvert.DeserializeObject<ChatCompletionsResult>(json, ChatGPTSharp.Model.ChatCompletions.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this ChatCompletionsResult self) => JsonConvert.SerializeObject(self, ChatGPTSharp.Model.ChatCompletions.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
