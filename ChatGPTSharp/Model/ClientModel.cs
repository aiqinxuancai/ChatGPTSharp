using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace ChatGPTSharp.Model
{
    public enum RoleType
    {
        User,
        Assistant,
        System,
        Tool,
    }

    public class Conversation
    {
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public long CreatedAt { get; set; }
    }

    public class ChatMessage
    {
        public string? Id { get; set; }

        public string? ParentMessageId { get; set; }

        public RoleType Role { get; set; }

        public List<MessageContent> Contents { get; set; } = new List<MessageContent>();

        public List<ToolCall>? ToolCalls { get; set; }

        public string? ToolCallId { get; set; }

        public string? Name { get; set; }

        public AudioOutputModel? AudioOutput { get; set; }

        public JsonObject ToRequestBody()
        {
            var body = new JsonObject
            {
                ["role"] = RoleToString(Role)
            };

            if (!string.IsNullOrWhiteSpace(Name))
            {
                body["name"] = Name;
            }

            if (Role == RoleType.Tool)
            {
                body["content"] = GetTextContent() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(ToolCallId))
                {
                    body["tool_call_id"] = ToolCallId;
                }
                return body;
            }

            if (ToolCalls != null && ToolCalls.Count > 0)
            {
                var toolCalls = new JsonArray();
                foreach (var toolCall in ToolCalls.Select(t => t.ToJsonObject()))
                {
                    toolCalls.Add(toolCall);
                }
                body["tool_calls"] = toolCalls;
            }

            if (Contents == null || Contents.Count == 0)
            {
                body["content"] = string.Empty;
                return body;
            }

            var contentArray = new JsonArray();
            foreach (var content in Contents.Select(c => c.ToJsonObject()))
            {
                contentArray.Add(content);
            }
            body["content"] = contentArray;
            return body;
        }

        public static ChatMessage FromResponse(JsonObject message)
        {
            var role = ParseRole(message["role"]?.GetValue<string>());
            var contents = MessageContent.FromJsonNode(message["content"]);

            var toolCalls = message["tool_calls"] is JsonArray toolCallsArray
                ? toolCallsArray
                    .OfType<JsonObject>()
                    .Select(ToolCall.FromJsonObject)
                    .ToList()
                : null;

            var audioOutput = message["audio"] is JsonObject audioObj
                ? AudioOutputModel.FromJsonObject(audioObj)
                : null;

            return new ChatMessage
            {
                Role = role,
                Contents = contents,
                ToolCalls = toolCalls,
                AudioOutput = audioOutput
            };
        }

        public string? GetTextContent()
        {
            return MessageContent.ExtractText(Contents);
        }

        private static string RoleToString(RoleType role)
        {
            return role switch
            {
                RoleType.User => "user",
                RoleType.Assistant => "assistant",
                RoleType.System => "system",
                RoleType.Tool => "tool",
                _ => "user",
            };
        }

        private static RoleType ParseRole(string? role)
        {
            return role switch
            {
                "assistant" => RoleType.Assistant,
                "system" => RoleType.System,
                "tool" => RoleType.Tool,
                _ => RoleType.User,
            };
        }

    }

    public class ConversationResult
    {
        public string? Response { get; set; }
        public string? ConversationId { get; set; }
        public string? MessageId { get; set; }
        public string? Details { get; set; }
        public List<ToolCall>? ToolCalls { get; set; }
        public AudioOutputModel? AudioOutput { get; set; }
        public List<MessageContent>? Contents { get; set; }
    }
}
