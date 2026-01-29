using System;
using System.Collections.Generic;

namespace ChatGPTSharp.Model
{
    public sealed class ChatRequest
    {
        public IReadOnlyList<ChatMessage> Messages { get; set; } = Array.Empty<ChatMessage>();
        public string? Model { get; set; }
        public double? Temperature { get; set; }
        public double? TopP { get; set; }
        public double? PresencePenalty { get; set; }
        public double? FrequencyPenalty { get; set; }
        public IReadOnlyList<ToolDefinition>? Tools { get; set; }
        public ToolChoice? ToolChoice { get; set; }
        public IDictionary<string, object?>? ExtraBody { get; set; }
    }
}
