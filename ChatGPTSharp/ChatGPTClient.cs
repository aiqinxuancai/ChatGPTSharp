using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ChatGPTSharp.Model;
using ChatGPTSharp.Utils;

namespace ChatGPTSharp
{
    public class ChatGPTClient
    {
        private readonly Dictionary<string, Conversation> _conversationsCache = new Dictionary<string, Conversation>();
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public const string DefaultModel = "gpt-4o-mini";

        public const uint DefaultTimeout = 60;

        public ChatGPTClientSettings Settings { get; private set; }

        public bool IsDebug
        {
            get { return Settings.IsDebug; }
            set { Settings.IsDebug = value; }
        }

        public ChatGPTClient(string openAIKey, string modelName = DefaultModel, string proxyUri = "", uint timeoutSeconds = DefaultTimeout)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                throw new ChatGPTException("ModelName is null.");
            }
            if (string.IsNullOrEmpty(openAIKey))
            {
                throw new ChatGPTException("OpenAIKey is null.");
            }

            ChatGPTClientSettings settings = new ChatGPTClientSettings()
            {
                ModelName = modelName,
                OpenAIKey = openAIKey,
                ProxyUri = proxyUri,
                TimeoutSeconds = timeoutSeconds
            };

            Settings = settings;
        }

        public ChatGPTClient(ChatGPTClientSettings settings)
        {
            if (string.IsNullOrEmpty(settings.ModelName))
            {
                throw new ChatGPTException("ModelName is null.");
            }
            if (string.IsNullOrEmpty(settings.OpenAIKey))
            {
                throw new ChatGPTException("OpenAIKey is null.");
            }

            Settings = settings;
        }

        public ChatSession CreateSession(
            string systemPrompt = "",
            IReadOnlyList<ToolDefinition>? tools = null,
            ToolChoice? toolChoice = null,
            IDictionary<string, object?>? extraBody = null)
        {
            return new ChatSession(this, systemPrompt, tools, toolChoice, extraBody);
        }

        /// <summary>
        /// clear conversation
        /// </summary>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        public bool RemoveConversationId(string conversationId)
        {
            if (_conversationsCache.ContainsKey(conversationId))
            {
                _conversationsCache.Remove(conversationId);
                return true;
            }
            return false;
        }

        public Task<ConversationResult> SendMessage(
            string message,
            string systemPrompt = "",
            IReadOnlyList<ToolDefinition>? tools = null,
            ToolChoice? toolChoice = null,
            IDictionary<string, object?>? extraBody = null,
            CancellationToken cancellationToken = default)
        {
            var contents = new List<MessageContent> { MessageContent.FromText(message) };
            return SendMessage(contents, systemPrompt, tools, toolChoice, extraBody, cancellationToken);
        }

        public Task<ConversationResult> SendMessage(params MessageContent[] contents)
        {
            return SendMessage(contents?.ToList() ?? new List<MessageContent>());
        }

        /// <summary>
        /// Stateless chat call. Does not record conversation history.
        /// </summary>
        public async Task<ConversationResult> SendMessage(
            List<MessageContent> contents,
            string systemPrompt = "",
            IReadOnlyList<ToolDefinition>? tools = null,
            ToolChoice? toolChoice = null,
            IDictionary<string, object?>? extraBody = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userMessage = new ChatMessage
                {
                    Role = RoleType.User,
                    Contents = contents ?? new List<MessageContent>()
                };

                var messages = new List<ChatMessage>();
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    messages.Add(new ChatMessage
                    {
                        Role = RoleType.System,
                        Contents = new List<MessageContent> { MessageContent.FromText(systemPrompt) }
                    });
                }
                messages.Add(userMessage);

                var request = new ChatRequest
                {
                    Messages = messages,
                    Tools = tools,
                    ToolChoice = toolChoice,
                    ExtraBody = extraBody
                };

                ChatResponse response = await SendAsync(request, cancellationToken);

                var replyMessage = response.Message ?? new ChatMessage
                {
                    Role = RoleType.Assistant
                };

                return new ConversationResult()
                {
                    Response = replyMessage.GetTextContent(),
                    Details = response.Raw.ToJsonString(JsonOptions),
                    ToolCalls = replyMessage.ToolCalls,
                    AudioOutput = replyMessage.AudioOutput,
                    Contents = replyMessage.Contents
                };
            }
            catch (Exception ex)
            {
                if (Settings.IsDebug)
                {
                    Console.WriteLine(ex);
                    Debug.WriteLine(ex.ToString());
                }

                throw;
            }
        }

        /// <summary>
        /// Stateful chat call with ConversationId and MessageId tracking.
        /// </summary>
        public Task<ConversationResult> SendMessageWithConversation(
            string message,
            string conversationId = "",
            string parentMessageId = "",
            string systemPrompt = "",
            IReadOnlyList<ToolDefinition>? tools = null,
            ToolChoice? toolChoice = null,
            IDictionary<string, object?>? extraBody = null,
            CancellationToken cancellationToken = default)
        {
            var contents = new List<MessageContent> { MessageContent.FromText(message) };
            return SendMessageWithConversation(contents, conversationId, parentMessageId, systemPrompt, tools, toolChoice, extraBody, cancellationToken);
        }

        public Task<ConversationResult> SendMessageWithConversation(params MessageContent[] contents)
        {
            return SendMessageWithConversation(contents?.ToList() ?? new List<MessageContent>());
        }

        public async Task<ConversationResult> SendMessageWithConversation(
            List<MessageContent> contents,
            string conversationId = "",
            string parentMessageId = "",
            string systemPrompt = "",
            IReadOnlyList<ToolDefinition>? tools = null,
            ToolChoice? toolChoice = null,
            IDictionary<string, object?>? extraBody = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                conversationId = !string.IsNullOrEmpty(conversationId) ? conversationId : Guid.NewGuid().ToString();
                parentMessageId = !string.IsNullOrEmpty(parentMessageId) ? parentMessageId : Guid.NewGuid().ToString();

                _conversationsCache.TryGetValue(conversationId, out Conversation? conversation);
                if (conversation == null)
                {
                    conversation = new Conversation
                    {
                        Messages = new List<ChatMessage>(),
                        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    };
                }

                var userMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    ParentMessageId = parentMessageId,
                    Role = RoleType.User,
                    Contents = contents ?? new List<MessageContent>()
                };

                conversation.Messages.Add(userMessage);

                var request = new ChatRequest
                {
                    Messages = BuildChatMessages(conversation.Messages, userMessage.Id!, systemPrompt),
                    Tools = tools,
                    ToolChoice = toolChoice,
                    ExtraBody = extraBody
                };

                ChatResponse response = await SendAsync(request, cancellationToken);

                var replyMessage = response.Message ?? new ChatMessage
                {
                    Role = RoleType.Assistant
                };

                replyMessage.Id = Guid.NewGuid().ToString();
                replyMessage.ParentMessageId = userMessage.Id;

                conversation.Messages.Add(replyMessage);
                _conversationsCache[conversationId] = conversation;

                return new ConversationResult()
                {
                    Response = replyMessage.GetTextContent(),
                    ConversationId = conversationId,
                    MessageId = replyMessage.Id,
                    Details = response.Raw.ToJsonString(JsonOptions),
                    ToolCalls = replyMessage.ToolCalls,
                    AudioOutput = replyMessage.AudioOutput,
                    Contents = replyMessage.Contents
                };
            }
            catch (Exception ex)
            {
                if (Settings.IsDebug)
                {
                    Console.WriteLine(ex);
                    Debug.WriteLine(ex.ToString());
                }

                throw;
            }
        }

        public Task<ResponseResult> CreateResponseAsync(
            List<MessageContent> contents,
            string? instructions = null,
            IReadOnlyList<ResponseTool>? tools = null,
            ResponseToolChoice? toolChoice = null,
            bool? store = null,
            string? conversationId = null,
            string? previousResponseId = null,
            IDictionary<string, object?>? extraBody = null,
            CancellationToken cancellationToken = default)
        {
            var input = MessageContent.BuildResponseInput(RoleType.User, contents ?? new List<MessageContent>());
            var request = new ResponseRequest
            {
                Input = input,
                Instructions = instructions,
                Tools = tools,
                ToolChoice = toolChoice,
                Store = store,
                ConversationId = conversationId,
                PreviousResponseId = previousResponseId,
                ExtraBody = extraBody
            };

            return CreateResponseAsync(request, cancellationToken);
        }

        public async Task<ResponseResult> CreateResponseAsync(ResponseRequest request, CancellationToken cancellationToken = default)
        {
            var body = BuildResponsesRequestBody(request, stream: false);
            var data = await PostData(body, Settings.ResponsesUrl, cancellationToken);
            var result = data.result;

            return new ResponseResult
            {
                Id = GetString(result, "id"),
                Model = GetString(result, "model"),
                ConversationId = GetNodeByPath(result, "conversation.id")?.GetValue<string>()
                    ?? GetString(result, "conversation_id"),
                Output = result["output"] as JsonArray ?? new JsonArray(),
                Raw = result
            };
        }

        public async IAsyncEnumerable<ResponseStreamEvent> StreamResponseAsync(
            ResponseRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var body = BuildResponsesRequestBody(request, stream: true);
            await foreach (var chunk in StreamRawAsync(body, Settings.ResponsesUrl, cancellationToken))
            {
                var type = chunk["type"]?.GetValue<string>();
                string? delta = null;

                if (string.Equals(type, "response.output_text.delta", StringComparison.OrdinalIgnoreCase))
                {
                    delta = chunk["delta"]?.GetValue<string>();
                }
                else if (string.Equals(type, "response.output_text", StringComparison.OrdinalIgnoreCase))
                {
                    delta = chunk["text"]?.GetValue<string>();
                }

                yield return new ResponseStreamEvent
                {
                    Type = type,
                    DeltaText = delta,
                    Raw = chunk,
                    IsDone = string.Equals(type, "response.completed", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(type, "response.failed", StringComparison.OrdinalIgnoreCase)
                };
            }
        }

        public async Task<ConversationInfo> CreateConversationAsync(
            JsonArray? items = null,
            IDictionary<string, object?>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            var body = new JsonObject();
            if (items != null && items.Count > 0)
            {
                body["items"] = items;
            }
            if (metadata != null && metadata.Count > 0)
            {
                body["metadata"] = JsonSerializer.SerializeToNode(metadata, JsonOptions) as JsonObject;
            }

            var data = await PostData(body, Settings.ConversationsUrl, cancellationToken);
            var result = data.result;
            return new ConversationInfo
            {
                Id = GetString(result, "id"),
                Raw = result
            };
        }

        public async Task<JsonObject> AddConversationItemsAsync(
            string conversationId,
            JsonArray items,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentException("conversationId is null or empty.", nameof(conversationId));
            }
            if (items == null || items.Count == 0)
            {
                throw new ArgumentException("items is null or empty.", nameof(items));
            }

            var url = $"{Settings.ConversationsUrl}/{conversationId}/items";
            var body = new JsonObject
            {
                ["items"] = items
            };

            var data = await PostData(body, url, cancellationToken);
            return data.result;
        }

        public IAsyncEnumerable<ChatStreamEvent> SendMessageStream(
            string message,
            string systemPrompt = "",
            IReadOnlyList<ToolDefinition>? tools = null,
            ToolChoice? toolChoice = null,
            IDictionary<string, object?>? extraBody = null,
            CancellationToken cancellationToken = default)
        {
            var contents = new List<MessageContent> { MessageContent.FromText(message) };
            return SendMessageStream(contents, systemPrompt, tools, toolChoice, extraBody, cancellationToken);
        }

        public IAsyncEnumerable<ChatStreamEvent> SendMessageStream(params MessageContent[] contents)
        {
            return SendMessageStream(contents?.ToList() ?? new List<MessageContent>());
        }

        /// <summary>
        /// Stateless streaming call. Does not record conversation history.
        /// </summary>
        public async IAsyncEnumerable<ChatStreamEvent> SendMessageStream(
            List<MessageContent> contents,
            string systemPrompt = "",
            IReadOnlyList<ToolDefinition>? tools = null,
            ToolChoice? toolChoice = null,
            IDictionary<string, object?>? extraBody = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var messages = new List<ChatMessage>();
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(new ChatMessage
                {
                    Role = RoleType.System,
                    Contents = new List<MessageContent> { MessageContent.FromText(systemPrompt) }
                });
            }

            messages.Add(new ChatMessage
            {
                Role = RoleType.User,
                Contents = contents ?? new List<MessageContent>()
            });

            var request = new ChatRequest
            {
                Messages = messages,
                Tools = tools,
                ToolChoice = toolChoice,
                ExtraBody = extraBody
            };

            var replyMessageId = Guid.NewGuid().ToString();
            var contentBuilder = new StringBuilder();
            var toolCallAccumulator = new ToolCallAccumulator();

            await foreach (var chunk in StreamRawAsync(request, cancellationToken))
            {
                var deltaText = GetNodeByPath(chunk, "choices[0].delta.content")?.GetValue<string>();
                if (!string.IsNullOrEmpty(deltaText))
                {
                    contentBuilder.Append(deltaText);
                }

                if (GetNodeByPath(chunk, "choices[0].delta.tool_calls") is JsonArray toolCallsDelta)
                {
                    toolCallAccumulator.AddDelta(toolCallsDelta);
                }

                yield return new ChatStreamEvent
                {
                    MessageId = replyMessageId,
                    DeltaText = deltaText,
                    Raw = chunk,
                    IsDone = false
                };
            }

            yield return new ChatStreamEvent
            {
                MessageId = replyMessageId,
                DeltaText = null,
                    Raw = new JsonObject(),
                    IsDone = true
                };
        }

        /// <summary>
        /// Stateful streaming call with ConversationId and MessageId tracking.
        /// </summary>
        public IAsyncEnumerable<ChatStreamEvent> SendMessageStreamWithConversation(
            string message,
            string conversationId = "",
            string parentMessageId = "",
            string systemPrompt = "",
            IReadOnlyList<ToolDefinition>? tools = null,
            ToolChoice? toolChoice = null,
            IDictionary<string, object?>? extraBody = null,
            CancellationToken cancellationToken = default)
        {
            var contents = new List<MessageContent> { MessageContent.FromText(message) };
            return SendMessageStreamWithConversation(contents, conversationId, parentMessageId, systemPrompt, tools, toolChoice, extraBody, cancellationToken);
        }

        public IAsyncEnumerable<ChatStreamEvent> SendMessageStreamWithConversation(params MessageContent[] contents)
        {
            return SendMessageStreamWithConversation(contents?.ToList() ?? new List<MessageContent>());
        }

        public async IAsyncEnumerable<ChatStreamEvent> SendMessageStreamWithConversation(
            List<MessageContent> contents,
            string conversationId = "",
            string parentMessageId = "",
            string systemPrompt = "",
            IReadOnlyList<ToolDefinition>? tools = null,
            ToolChoice? toolChoice = null,
            IDictionary<string, object?>? extraBody = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            conversationId = !string.IsNullOrEmpty(conversationId) ? conversationId : Guid.NewGuid().ToString();
            parentMessageId = !string.IsNullOrEmpty(parentMessageId) ? parentMessageId : Guid.NewGuid().ToString();

            _conversationsCache.TryGetValue(conversationId, out Conversation? conversation);
            if (conversation == null)
            {
                conversation = new Conversation
                {
                    Messages = new List<ChatMessage>(),
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                };
            }

            var userMessage = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                ParentMessageId = parentMessageId,
                Role = RoleType.User,
                Contents = contents ?? new List<MessageContent>()
            };

            conversation.Messages.Add(userMessage);

            var request = new ChatRequest
            {
                Messages = BuildChatMessages(conversation.Messages, userMessage.Id!, systemPrompt),
                Tools = tools,
                ToolChoice = toolChoice,
                ExtraBody = extraBody
            };

            var replyMessageId = Guid.NewGuid().ToString();
            var contentBuilder = new StringBuilder();
            var toolCallAccumulator = new ToolCallAccumulator();

            await foreach (var chunk in StreamRawAsync(request, cancellationToken))
            {
                var deltaText = GetNodeByPath(chunk, "choices[0].delta.content")?.GetValue<string>();
                if (!string.IsNullOrEmpty(deltaText))
                {
                    contentBuilder.Append(deltaText);
                }

                if (GetNodeByPath(chunk, "choices[0].delta.tool_calls") is JsonArray toolCallsDelta)
                {
                    toolCallAccumulator.AddDelta(toolCallsDelta);
                }

                yield return new ChatStreamEvent
                {
                    ConversationId = conversationId,
                    MessageId = replyMessageId,
                    DeltaText = deltaText,
                    Raw = chunk,
                    IsDone = false
                };
            }

            var replyMessage = new ChatMessage
            {
                Id = replyMessageId,
                ParentMessageId = userMessage.Id,
                Role = RoleType.Assistant,
                Contents = contentBuilder.Length == 0
                    ? new List<MessageContent>()
                    : new List<MessageContent> { MessageContent.FromText(contentBuilder.ToString()) },
                ToolCalls = toolCallAccumulator.Build()
            };

            conversation.Messages.Add(replyMessage);
            _conversationsCache[conversationId] = conversation;

            yield return new ChatStreamEvent
            {
                ConversationId = conversationId,
                MessageId = replyMessageId,
                DeltaText = null,
                Raw = new JsonObject(),
                IsDone = true
            };
        }

        public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            var body = BuildRequestBody(request, stream: false);
            var data = await PostData(body, cancellationToken);
            var result = data.result;

            var messageToken = GetNodeByPath(result, "choices[0].message") as JsonObject;
            var message = messageToken != null ? ChatMessage.FromResponse(messageToken) : null;

            return new ChatResponse
            {
                Id = GetString(result, "id"),
                Model = GetString(result, "model"),
                Message = message,
                Usage = result["usage"] as JsonObject,
                Raw = result
            };
        }

        public async IAsyncEnumerable<ChatStreamEvent> StreamAsync(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var chunk in StreamRawAsync(request, cancellationToken))
            {
                var deltaText = GetNodeByPath(chunk, "choices[0].delta.content")?.GetValue<string>();
                yield return new ChatStreamEvent
                {
                    DeltaText = deltaText,
                    Raw = chunk,
                    IsDone = false
                };
            }

            yield return new ChatStreamEvent
            {
                IsDone = true
            };
        }

        private async Task<(JsonObject result, string source)> PostData(JsonObject body, CancellationToken cancellationToken)
        {
            return await PostData(body, Settings.CompletionsUrl, cancellationToken);
        }

        private async IAsyncEnumerable<JsonObject> StreamRawAsync(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var body = BuildRequestBody(request, stream: true);
            await foreach (var chunk in StreamRawAsync(body, Settings.CompletionsUrl, cancellationToken))
            {
                yield return chunk;
            }
        }

        private async Task<(JsonObject result, string source)> PostData(
            JsonObject body,
            string url,
            CancellationToken cancellationToken)
        {
            using var client = CreateHttpClient();

            if (Settings.IsDebug)
            {
                Console.WriteLine(url);
            }

            var jsonString = body.ToJsonString(JsonOptions);
            if (Settings.IsDebug)
            {
                Console.WriteLine("req:" + jsonString);
            }

            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content, cancellationToken);
            var resultJsonString = await response.Content.ReadAsStringAsync();

            if (Settings.IsDebug)
            {
                Console.WriteLine("rsp:" + resultJsonString);
            }

            response.EnsureSuccessStatusCodeWithContent(resultJsonString);

            var node = JsonNode.Parse(resultJsonString) as JsonObject;
            if (node == null)
            {
                throw new ChatGPTException("Invalid JSON response.");
            }
            JsonObject result = node;
            return (result, resultJsonString);
        }

        private async IAsyncEnumerable<JsonObject> StreamRawAsync(
            JsonObject body,
            string url,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var client = CreateHttpClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body.ToJsonString(JsonOptions), Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Settings.OpenAIKey);

            var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCodeWithContent(errorContent);
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync();
                if (line == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var data = line.Substring(5).Trim();
                if (data == "[DONE]")
                {
                    yield break;
                }

                JsonObject? chunk = null;
                try
                {
                    chunk = JsonNode.Parse(data) as JsonObject;
                }
                catch (JsonException)
                {
                    if (Settings.IsDebug)
                    {
                        Console.WriteLine("stream parse failed: " + data);
                    }
                }

                if (chunk != null)
                {
                    yield return chunk;
                }
            }
        }

        private HttpClient CreateHttpClient()
        {
            var httpClientHandler = new HttpClientHandler();

            if (!string.IsNullOrEmpty(Settings.ProxyUri))
            {
                WebProxy proxy = new WebProxy(Settings.ProxyUri);
                httpClientHandler.Proxy = proxy;
                httpClientHandler.UseProxy = true;
            }

            var client = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(Settings.TimeoutSeconds)
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Settings.OpenAIKey);
            return client;
        }

        private JsonObject BuildRequestBody(ChatRequest request, bool stream)
        {
            var req = new JsonObject
            {
                ["model"] = request.Model ?? Settings.ModelName,
                ["temperature"] = request.Temperature ?? Settings.Temperature,
                ["top_p"] = request.TopP ?? Settings.TopP,
                ["presence_penalty"] = request.PresencePenalty ?? Settings.PresencePenalty,
                ["frequency_penalty"] = request.FrequencyPenalty ?? Settings.FrequencyPenalty
            };

            var messagesArray = new JsonArray();
            foreach (var msg in request.Messages.Select(m => m.ToRequestBody()))
            {
                messagesArray.Add(msg);
            }
            req["messages"] = messagesArray;

            if (request.Tools != null && request.Tools.Count > 0)
            {
                var toolsArray = new JsonArray();
                foreach (var tool in request.Tools.Select(t => t.ToJsonObject()))
                {
                    toolsArray.Add(tool);
                }
                req["tools"] = toolsArray;
            }

            if (request.ToolChoice != null)
            {
                var toolChoiceToken = request.ToolChoice.ToJsonNode();
                if (toolChoiceToken != null)
                {
                    req["tool_choice"] = toolChoiceToken;
                }
            }

            if (stream)
            {
                req["stream"] = true;
            }

            var mergedExtra = MergeExtraBody(Settings.ExtraBody, request.ExtraBody);
            if (mergedExtra != null)
            {
                JsonNodeUtils.Merge(req, mergedExtra);
            }

            return req;
        }

        private JsonObject BuildResponsesRequestBody(ResponseRequest request, bool stream)
        {
            var req = new JsonObject
            {
                ["model"] = request.Model ?? Settings.ModelName
            };

            if (request.Input != null)
            {
                req["input"] = request.Input;
            }

            if (!string.IsNullOrWhiteSpace(request.Instructions))
            {
                req["instructions"] = request.Instructions;
            }

            if (!string.IsNullOrWhiteSpace(request.ConversationId))
            {
                req["conversation"] = request.ConversationId;
            }

            if (!string.IsNullOrWhiteSpace(request.PreviousResponseId))
            {
                req["previous_response_id"] = request.PreviousResponseId;
            }

            if (request.Store.HasValue)
            {
                req["store"] = request.Store.Value;
            }

            if (request.Tools != null && request.Tools.Count > 0)
            {
                var toolsArray = new JsonArray();
                foreach (var tool in request.Tools.Select(t => t.ToJsonObject()))
                {
                    toolsArray.Add(tool);
                }
                req["tools"] = toolsArray;
            }

            if (request.ToolChoice != null)
            {
                var toolChoiceToken = request.ToolChoice.ToJsonNode();
                if (toolChoiceToken != null)
                {
                    req["tool_choice"] = toolChoiceToken;
                }
            }

            if (request.ParallelToolCalls.HasValue)
            {
                req["parallel_tool_calls"] = request.ParallelToolCalls.Value;
            }

            if (stream)
            {
                req["stream"] = true;
            }

            var mergedExtra = MergeExtraBody(Settings.ExtraBody, request.ExtraBody);
            if (mergedExtra != null)
            {
                JsonNodeUtils.Merge(req, mergedExtra);
            }

            return req;
        }

        private static JsonObject? MergeExtraBody(
            IDictionary<string, object?>? settingsExtra,
            IDictionary<string, object?>? requestExtra)
        {
            var merged = ToJObject(settingsExtra);
            var request = ToJObject(requestExtra);

            if (merged == null)
            {
                return request;
            }

            if (request != null)
            {
                JsonNodeUtils.Merge(merged, request);
            }

            return merged;
        }

        private static JsonObject? ToJObject(IDictionary<string, object?>? data)
        {
            if (data == null || data.Count == 0)
            {
                return null;
            }
            return JsonSerializer.SerializeToNode(data, JsonOptions) as JsonObject;
        }

        private static string? GetString(JsonObject obj, string propertyName)
        {
            return obj[propertyName]?.GetValue<string>();
        }

        private static JsonNode? GetNodeByPath(JsonNode? node, string path)
        {
            if (node == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            var segments = path.Split('.');
            JsonNode? current = node;

            foreach (var segment in segments)
            {
                if (current == null)
                {
                    return null;
                }

                var part = segment;
                while (true)
                {
                    var bracket = part.IndexOf('[');
                    if (bracket < 0)
                    {
                        if (current is JsonObject obj)
                        {
                            current = obj[part];
                        }
                        else
                        {
                            return null;
                        }
                        break;
                    }

                    var prop = part.Substring(0, bracket);
                    if (!string.IsNullOrEmpty(prop))
                    {
                        if (current is JsonObject obj)
                        {
                            current = obj[prop];
                        }
                        else
                        {
                            return null;
                        }
                    }

                    if (current == null)
                    {
                        return null;
                    }

                    var end = part.IndexOf(']', bracket);
                    if (end < 0)
                    {
                        return null;
                    }

                    var indexText = part.Substring(bracket + 1, end - bracket - 1);
                    if (!int.TryParse(indexText, out var index))
                    {
                        return null;
                    }

                    if (current is JsonArray array)
                    {
                        current = index >= 0 && index < array.Count ? array[index] : null;
                    }
                    else
                    {
                        return null;
                    }

                    if (end == part.Length - 1)
                    {
                        break;
                    }

                    part = part.Substring(end + 1);
                }
            }

            return current;
        }

        private static List<ChatMessage> BuildChatMessages(List<ChatMessage> messages, string parentMessageId, string systemPrompt = "")
        {
            var orderedMessages = GetMessagesForConversation(messages, parentMessageId);
            var payload = new List<ChatMessage>();

            if (!string.IsNullOrEmpty(systemPrompt))
            {
                payload.Add(new ChatMessage
                {
                    Role = RoleType.System,
                    Contents = new List<MessageContent> { MessageContent.FromText(systemPrompt) }
                });
            }

            payload.AddRange(orderedMessages);
            return payload;
        }

        private static List<ChatMessage> GetMessagesForConversation(List<ChatMessage> messages, string parentMessageId)
        {
            List<ChatMessage> orderedMessages = new List<ChatMessage>();
            string? currentMessageId = parentMessageId;
            while (currentMessageId != null)
            {
                ChatMessage? message = messages.Find(m => m.Id == currentMessageId);
                if (message == null)
                {
                    break;
                }
                orderedMessages.Insert(0, message);
                currentMessageId = message.ParentMessageId;
            }
            return orderedMessages;
        }

        private sealed class ToolCallAccumulator
        {
            private readonly Dictionary<int, ToolCallBuilder> _builders = new Dictionary<int, ToolCallBuilder>();

            public void AddDelta(JsonArray toolCallsDelta)
            {
                foreach (var token in toolCallsDelta.OfType<JsonObject>())
                {
                    int index = token["index"]?.GetValue<int>() ?? 0;
                    if (!_builders.TryGetValue(index, out var builder))
                    {
                        builder = new ToolCallBuilder();
                        _builders[index] = builder;
                    }

                    var id = token["id"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(id))
                    {
                        builder.Id = id;
                    }

                    var type = token["type"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(type))
                    {
                        builder.Type = type;
                    }

                    var function = token["function"] as JsonObject;
                    if (function != null)
                    {
                        var name = function["name"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(name))
                        {
                            builder.Name = name;
                        }

                        var arguments = function["arguments"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(arguments))
                        {
                            builder.Arguments.Append(arguments);
                        }
                    }
                }
            }

            public List<ToolCall>? Build()
            {
                if (_builders.Count == 0)
                {
                    return null;
                }

                var calls = new List<ToolCall>();
                foreach (var entry in _builders.OrderBy(e => e.Key))
                {
                    var builder = entry.Value;
                    var id = builder.Id ?? string.Empty;
                    var type = builder.Type ?? "function";
                    var name = builder.Name ?? string.Empty;
                    var arguments = builder.Arguments.ToString();
                    calls.Add(new ToolCall(id, type, new ToolFunctionCall(name, arguments)));
                }
                return calls;
            }
        }

        private sealed class ToolCallBuilder
        {
            public string? Id { get; set; }
            public string? Type { get; set; }
            public string? Name { get; set; }
            public StringBuilder Arguments { get; } = new StringBuilder();
        }
    }
}
