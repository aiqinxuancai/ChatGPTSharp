using System.Text.Json.Nodes;

namespace ChatGPTSharp.Model
{
    public sealed class ChatStreamEvent
    {
        public string ConversationId { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string? DeltaText { get; set; }
        public JsonObject Raw { get; set; } = new JsonObject();
        public bool IsDone { get; set; }
    }
}
