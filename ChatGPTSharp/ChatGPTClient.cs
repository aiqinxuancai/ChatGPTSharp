using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Xml.Linq;
using ChatGPTSharp.Model;
using System.Globalization;
using System.Data;
using System.Net;
using ChatGPTSharp.Utils;
using TiktokenSharp;

namespace ChatGPTSharp
{

    

    public class ChatGPTClient
    {
        public int MaxContextTokens { set; get; } = 4097;
        public int MaxResponseTokens { set; get; } = 1024;
        public int MaxPromptTokens { set; get; } = 3073;
        public string? UserLabel { set; get; } = "user";
        public string? ChatGptLabel { set; get; } = "assistant";
        
        public string CompletionsUrl { set; get; } = "";
        public bool IsDebug { set; get; }
        public string EndToken { set; get; } = "<|endoftext|>";
        public string StartToken { set; get; } = "";

       


        private Dictionary<string, Conversation> _conversationsCache = new Dictionary<string, Conversation>();
        private string _model = "text-davinci-003";
        private string[] _stop = { };
        private string _openAIToken = string.Empty;
        private bool _isChatGptModel;
        private bool _isUnofficialChatGptModel;
        private int _messageTokenOffset  = 7;
        private string _proxyUri = string.Empty;
        private int _timeoutSeconds = 60;



        private TikToken _tiktoken;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="openaiToken"></param>
        /// <param name="modelName">text-davinci-003、gpt-3.5-turbo</param>
        public ChatGPTClient(string openaiToken, string modelName = "gpt-3.5-turbo", string proxyUri = "", int timeoutSeconds = 60)
        {
            _isUnofficialChatGptModel = _model.StartsWith("text-chat") || _model.StartsWith("text-davinci-002-render");
            _model = modelName;
            _isChatGptModel = _model.StartsWith("gpt-3.5-turbo");
            _openAIToken = openaiToken;
            _proxyUri = proxyUri;
            _tiktoken = TikToken.EncodingForModel(modelName);
            _timeoutSeconds = timeoutSeconds;

            if (_isChatGptModel)
            {
                //if (UserLabel.ToLower() == "user")
                //{
                //    UserLabel = null;
                //}
                //if (ChatGptLabel.ToLower() == "assistant")
                //{
                //    ChatGptLabel = null;
                //}

                //gpt-3.5-turbo 
                UserLabel = null;
                ChatGptLabel = null;
            }

            if (_isChatGptModel)
            {
                StartToken = "";
                EndToken = "";
            }
            else if (_isUnofficialChatGptModel)
            {
                StartToken = "<|im_start|>";
                EndToken = "<|im_end|>";
            }
            else
            {
                StartToken = "<|endoftext|>";
                EndToken = StartToken;
            }

            if (!_isChatGptModel)
            {
                if (_isUnofficialChatGptModel)
                {
                    _stop = new string[] { EndToken, StartToken, $"\n${UserLabel}:" };
                }
                else
                {
                    _stop = new string[] { EndToken, $"\n{UserLabel}:" };
                }
            }

            //You can set up custom URLs and use your own reverse proxy address for support.
            string apiBaseUri = Environment.GetEnvironmentVariable("OPENAI_API_BASE_URI_CUSTOM");

            if (string.IsNullOrWhiteSpace(apiBaseUri))
            {
                apiBaseUri = "https://api.openai.com/";
            }

            UriBuilder uriBuilder = new UriBuilder(apiBaseUri);
            if (_isChatGptModel)
            {
                uriBuilder.Path = "/v1/chat/completions";
            }
            else
            {
                uriBuilder.Path = $"/v1/completions";
            }

            CompletionsUrl = uriBuilder.Uri.AbsoluteUri;

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


        /// <summary>
        /// SendMessage
        /// </summary>
        /// <param name="message"></param>
        /// <param name="conversationId"></param>
        /// <param name="parentMessageId"></param>
        /// <param name="sendSystemType">only gpt-3.5-turbo</param>
        /// <param name="sendSystemMessage">only gpt-3.5-turbo, need SendSystemType.Custom, see https://platform.openai.com/docs/guides/chat </param>
        /// <returns></returns>
        public async Task<ConversationResult> SendMessage(string message, string conversationId = "", string parentMessageId = "", SendSystemType sendSystemType = SendSystemType.None, string sendSystemMessage = "")
        {
            try
            {
                conversationId = !string.IsNullOrEmpty(conversationId) ? conversationId : Guid.NewGuid().ToString();
                parentMessageId = !string.IsNullOrEmpty(parentMessageId) ? parentMessageId : Guid.NewGuid().ToString();

                _conversationsCache.TryGetValue(conversationId, out Conversation conversation);
                if (conversation == null)
                {
                    conversation = new Conversation
                    {
                        Messages = new List<Message>(),
                        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    };
                }

                //把当前消息加入会话
                var userMessage = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    ParentMessageId = parentMessageId,
                    Role = "User",
                    Content = message,
                };

                conversation.Messages.Add(userMessage);

                JObject result = new JObject();
                string? reply = string.Empty;
                var resultJsonString = string.Empty;
                
                if (_isChatGptModel)
                {
                    List<JObject> messages = BuildChatPayload(conversation.Messages, userMessage.Id, sendSystemType, sendSystemMessage);
                    var data = await PostData(messages);
                    result = data.result;
                    reply = (string?)result.SelectToken("choices[0].message.content");
                    resultJsonString = data.source;
                }
                else
                {
                    string prompt = BuildPrompt(conversation.Messages, userMessage.Id);
                    var data = await PostData(prompt);
                    result = data.result;
                    reply = (string?)result.SelectToken("choices[0].text");
                    resultJsonString = data.source;
                }

                //存储
                if (!string.IsNullOrEmpty(EndToken))
                {
                    reply = reply?.Replace(EndToken, "");
                }

                reply = reply?.Trim();

                //userMessage.UsageTokens = result.SelectToken("usage.prompt_tokens").ToObject<int>();

                var replyMessage = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    ParentMessageId = userMessage.Id,
                    Role = "ChatGPT",
                    Content = reply,
                    UsageTokens = result.SelectToken("usage.completion_tokens").ToObject<int>(),
                    TotalTokens = result.SelectToken("usage.total_tokens").ToObject<int>(),
                };

                conversation.Messages.Add(replyMessage);

                _conversationsCache[conversationId] = conversation;

                return new ConversationResult()
                {
                    Response = replyMessage.Content,
                    ConversationId = conversationId,
                    MessageId = replyMessage.Id,
                    Details = resultJsonString
                };

            }
            catch (Exception ex)
            {
                if (IsDebug)
                {
                    Console.WriteLine(ex);
                    Debug.WriteLine(ex.ToString());
                }

                throw;
            }

        }
        private async Task<(JObject result, string source)> PostData(object obj)
        {
            var httpClientHandler = new HttpClientHandler();

            if (!string.IsNullOrEmpty(_proxyUri))
            {
                WebProxy proxy = new WebProxy(_proxyUri);

                httpClientHandler.Proxy = proxy;
                httpClientHandler.UseProxy = true;
            }

            HttpClient client = new HttpClient(httpClientHandler);
            client.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

            string uri = CompletionsUrl;

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAIToken}");

            JObject req = new JObject();
            req["model"] = _model;
            req["temperature"] = 0.8;
            req["top_p"] = 1;
            req["presence_penalty"] = 1;

            //req["max_tokens"] = 1024;
            //req["frequency_penalty"] = 0;

            if (_isChatGptModel)
            {
                req["messages"] = new JArray(((List<JObject>)obj).ToArray());
            }
            else
            {
                req["prompt"] = (string)obj;
                req["stop"] = new JArray(_stop);
            }

            var jsonString = req.ToString();

            if (IsDebug)
            {
                Console.WriteLine(jsonString);
            }

            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(uri, content);


            var resultJsonString = await response.Content.ReadAsStringAsync();
            if (IsDebug)
            {
                Console.WriteLine(resultJsonString);
            }
            response.EnsureSuccessStatusCode();


            JObject result = JObject.Parse(resultJsonString);
            return (result, resultJsonString);
        }

        private string BuildPrompt(List<Message> messages, string parentMessageId)
        {
            var orderedMessages = GetMessagesForConversation(messages, parentMessageId);
            var currentDateString = DateTime.Now.ToString("MMMM d, yyyy");
            string promptPrefix = $"\n{StartToken}Instructions:\nYou are ChatGPT, a large language model trained by OpenAI.\nCurrent date: {currentDateString}{StartToken}\n\n";

            var promptSuffix = $"{ChatGptLabel}:\n";
            var currentTokenCount = GetTokenCount($"{promptPrefix}{promptSuffix}"); //TODO
            var promptBody = string.Empty;
            var maxTokenCount = MaxPromptTokens;

            while (currentTokenCount < maxTokenCount && orderedMessages.Count > 0)
            {
                var message = orderedMessages.Last();
                orderedMessages.Remove(message);
                var roleLabel = message.Role == "User" ? UserLabel : ChatGptLabel;
                var messageString = $"{roleLabel}:\n{message.Content}{EndToken}\n";
                var newPromptBody = string.Empty;

                if (!string.IsNullOrEmpty(promptBody))
                {
                    newPromptBody = $"{messageString}{promptBody}";
                }
                else
                {
                    newPromptBody = $"{promptPrefix}{messageString}{promptBody}";
                }

                var newTokenCount = GetTokenCount($"{promptPrefix}{newPromptBody}{promptSuffix}");

                if (newTokenCount > maxTokenCount)
                {
                    if (!string.IsNullOrEmpty(promptBody))
                    {
                        break;
                    }

                    throw new ArgumentException($"Prompt is too long. Max token count is {maxTokenCount}, but prompt is {newTokenCount} tokens long.");
                }

                promptBody = newPromptBody;
                currentTokenCount = newTokenCount;
            }

            var prompt = $"{promptBody}{promptSuffix}";
            var numTokens = GetTokenCount(prompt);

            //TODO ？？
            //modelOptions.max_tokens = Math.min(maxContextTokens - numTokens, maxResponseTokens);

            return prompt;
        }

        public List<JObject> BuildChatPayload(List<Message> messages, string parentMessageId, SendSystemType sendSystemType = SendSystemType.None, string sendSystemMessage = "")
        {
            var orderedMessages = GetMessagesForConversation(messages, parentMessageId);

            string systemMessage = string.Empty;

            if (sendSystemType == SendSystemType.BaseMessage)
            {
                var currentDateString = DateTime.Now.ToString("MMMM d, yyyy");
                systemMessage = $"You are ChatGPT, a large language model trained by OpenAI.\nCurrent date: {currentDateString}";
            } 
            else if (sendSystemType == SendSystemType.Custom && !string.IsNullOrWhiteSpace(sendSystemMessage))
            {
                systemMessage = sendSystemMessage;
            }

            var payload = new List<JObject>();

            bool isFirstMessage = true;
            int currentTokenCount = systemMessage != null ? (GetTokenCount(systemMessage) + _messageTokenOffset) : 0;
            int maxTokenCount = MaxPromptTokens;

            //If the current token count has not exceeded and there are still messages in orderedMessages, continue. 
            //TODO: Prepare to add restrictions to support setting only the last * messages.
            while (currentTokenCount < maxTokenCount && orderedMessages.Count > 0)
            {
                var message = orderedMessages.Last();
                orderedMessages.Remove(message);

                string? messageString = message.Content;
                if (message.Role == "User")
                {
                    if (UserLabel != null)
                    {
                        messageString = $"{UserLabel}:\n{messageString}";
                    }
                    if (ChatGptLabel != null)
                    {
                        messageString = $"{messageString}\n{ChatGptLabel}:\n";
                    }
                }

                int newTokenCount = GetTokenCount(messageString!) + currentTokenCount + _messageTokenOffset;
                if (newTokenCount > maxTokenCount)
                {
                    if (!isFirstMessage)
                    {
                        break;
                    }
                    throw new Exception($"Prompt is too long. Max token count is {maxTokenCount}, but prompt is {newTokenCount} tokens long.");
                }

                payload.Insert(0, new JObject()
                {
                    { "role" , message.Role == "User" ? "user" : "assistant" },
                    {"content" , messageString }
                });
                isFirstMessage = false;
                currentTokenCount = newTokenCount;
            }

            if (!string.IsNullOrEmpty(systemMessage))
            {
                //Insert a system message before the last message.
                payload.Insert(payload.Count - 1, new JObject()
                {
                    {"role", "system"},
                    {"content", systemMessage}
                });
            }

            return payload;
        }


        private int GetTokenCount(string text)
        {
            text = Regex.Replace(text, "<|im_end|>", "");
            text = Regex.Replace(text, "<|im_sep|>", "");
            //TODO gptEncode(text).length;
            //

            return _tiktoken.Encode(text).Count;

//#if DEBUG
            return GPT3Token.Encode(text).Count;
//#else
//            return text.Length;
//#endif


        }

        private static List<Message> GetMessagesForConversation(List<Message> messages, string parentMessageId)
        {
            List<Message> orderedMessages = new List<Message>();
            string? currentMessageId = parentMessageId;
            while (currentMessageId != null)
            {
                Message message = messages.Find(m => m.Id == currentMessageId);
                if (message == null)
                {
                    break;
                }
                orderedMessages.Insert(0, message);
                currentMessageId = message.ParentMessageId;
            }
            return orderedMessages;
        }


    }


}
