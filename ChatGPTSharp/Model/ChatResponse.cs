using System.Text.Json.Nodes;

namespace ChatGPTSharp.Model
{
    public sealed class ChatResponse
    {
        public string? Id { get; set; }
        public string? Model { get; set; }
        public ChatMessage? Message { get; set; }
        public JsonObject Raw { get; set; } = new JsonObject();
        public JsonObject? Usage { get; set; }
    }
}
