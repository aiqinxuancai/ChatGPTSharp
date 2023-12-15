using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatGPTSharp.Model
{
    public enum ContentType
    {
        None,
        Text,
        ImageUrl,
    }

    public enum RoleType
    {
        User,
        Assistant,
        System,
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
        public JToken Content { get; set; }

        /// <summary>
        /// only reply message
        /// </summary>
        public int UsageTokens { get; set; }

        /// <summary>
        /// The token count of the last requested message, a replacement for GetTokenCount implementation needed for future development.
        /// </summary>
        public int TotalTokens { get; set; }
    }

    public class ConversationResult
    {
        public JToken? Response { get; set; }
        public string? ConversationId { get; set; }
        public string? MessageId { get; set; }
        public string? Details { get; set; }
    }
}
