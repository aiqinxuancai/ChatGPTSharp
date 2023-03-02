using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatGPTSharp.Model
{
    public partial class CompletionsResult
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("choices")]
        public Choice[] Choices { get; set; }

        [JsonProperty("usage")]
        public Usage Usage { get; set; }
    }

    public partial class Choice
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("index")]
        public long Index { get; set; }

        [JsonProperty("logprobs")]
        public object Logprobs { get; set; }

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }
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


}
