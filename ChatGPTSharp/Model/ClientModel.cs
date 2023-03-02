using System;
using System.Collections.Generic;
using System.Text;

namespace ChatGPTSharp.Model
{
    public class Conversation
    {
        public List<Message> Messages { get; set; } = new List<Message>();
        public long CreatedAt { get; set; }
    }

    public class Message
    {
        public string? Id { get; set; }
        public string? ParentMessageId { get; set; }
        public string? Role { get; set; }
        public string? Content { get; set; }

        public int UsageToken { get; set; }
    }

    public class ConversationResult
    {
        public string? Response { get; set; }
        public string? ConversationId { get; set; }
        public string? MessageId { get; set; }
        public string? Details { get; set; }
    }
}
