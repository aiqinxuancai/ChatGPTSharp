using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ChatGPTSharp.Model;

namespace ChatGPTSharp
{
    public sealed class ChatSession
    {
        private readonly ChatGPTClient _client;

        internal ChatSession(
            ChatGPTClient client,
            string systemPrompt = "",
            IReadOnlyList<ToolDefinition>? tools = null,
            ToolChoice? toolChoice = null,
            IDictionary<string, object?>? extraBody = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            SystemPrompt = systemPrompt ?? string.Empty;
            Tools = tools;
            ToolChoice = toolChoice;
            ExtraBody = extraBody;
        }

        public string? ConversationId { get; private set; }
        public string? LastMessageId { get; private set; }

        public string SystemPrompt { get; set; } = string.Empty;
        public IReadOnlyList<ToolDefinition>? Tools { get; set; }
        public ToolChoice? ToolChoice { get; set; }
        public IDictionary<string, object?>? ExtraBody { get; set; }

        public void Reset()
        {
            ConversationId = null;
            LastMessageId = null;
        }

        public Task<ConversationResult> SendAsync(
            string message,
            string? systemPrompt = null,
            IReadOnlyList<ToolDefinition>? tools = null,
            ToolChoice? toolChoice = null,
            IDictionary<string, object?>? extraBody = null,
            CancellationToken cancellationToken = default)
        {
            return SendAsync(
                new List<MessageContent> { message },
                systemPrompt,
                tools,
                toolChoice,
                extraBody,
                cancellationToken);
        }

        public Task<ConversationResult> SendAsync(
            params MessageContent[] contents)
        {
            return SendAsync(contents?.ToList() ?? new List<MessageContent>());
        }

        public async Task<ConversationResult> SendAsync(
            List<MessageContent> contents,
            string? systemPrompt = null,
            IReadOnlyList<ToolDefinition>? tools = null,
            ToolChoice? toolChoice = null,
            IDictionary<string, object?>? extraBody = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _client.SendMessageWithConversation(
                contents,
                conversationId: ConversationId ?? string.Empty,
                parentMessageId: LastMessageId ?? string.Empty,
                systemPrompt: systemPrompt ?? SystemPrompt,
                tools: tools ?? Tools,
                toolChoice: toolChoice ?? ToolChoice,
                extraBody: extraBody ?? ExtraBody,
                cancellationToken: cancellationToken);

            if (!string.IsNullOrWhiteSpace(result.ConversationId))
            {
                ConversationId = result.ConversationId;
            }
            if (!string.IsNullOrWhiteSpace(result.MessageId))
            {
                LastMessageId = result.MessageId;
            }

            return result;
        }

        public IAsyncEnumerable<ChatStreamEvent> StreamAsync(
            string message,
            string? systemPrompt = null,
            IReadOnlyList<ToolDefinition>? tools = null,
            ToolChoice? toolChoice = null,
            IDictionary<string, object?>? extraBody = null,
            CancellationToken cancellationToken = default)
        {
            return StreamAsync(
                new List<MessageContent> { message },
                systemPrompt,
                tools,
                toolChoice,
                extraBody,
                cancellationToken);
        }

        public IAsyncEnumerable<ChatStreamEvent> StreamAsync(
            params MessageContent[] contents)
        {
            return StreamAsync(contents?.ToList() ?? new List<MessageContent>());
        }

        public async IAsyncEnumerable<ChatStreamEvent> StreamAsync(
            List<MessageContent> contents,
            string? systemPrompt = null,
            IReadOnlyList<ToolDefinition>? tools = null,
            ToolChoice? toolChoice = null,
            IDictionary<string, object?>? extraBody = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string? conversationId = null;
            string? messageId = null;

            await foreach (var chunk in _client.SendMessageStreamWithConversation(
                contents,
                conversationId: ConversationId ?? string.Empty,
                parentMessageId: LastMessageId ?? string.Empty,
                systemPrompt: systemPrompt ?? SystemPrompt,
                tools: tools ?? Tools,
                toolChoice: toolChoice ?? ToolChoice,
                extraBody: extraBody ?? ExtraBody,
                cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrWhiteSpace(chunk.ConversationId))
                {
                    conversationId = chunk.ConversationId;
                }
                if (!string.IsNullOrWhiteSpace(chunk.MessageId))
                {
                    messageId = chunk.MessageId;
                }

                yield return chunk;
            }

            if (!string.IsNullOrWhiteSpace(conversationId))
            {
                ConversationId = conversationId;
            }
            if (!string.IsNullOrWhiteSpace(messageId))
            {
                LastMessageId = messageId;
            }
        }
    }
}
