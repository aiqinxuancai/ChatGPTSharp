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
using ChatGPTSharp.Model.ChatCompletions;
using System.Net;

namespace ChatGPTSharp
{
    public class ChatGPTClient
    {
       

        public int MaxContextTokens { set; get; } = 4097;
        public int MaxResponseTokens { set; get; } = 1024;
        public int MaxPromptTokens { set; get; } = 3073;
        public string UserLabel { set; get; } = "User";
        public string ChatGptLabel { set; get; } = "ChatGPT";
        
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="openaiToken"></param>
        /// <param name="modelName">text-davinci-003、gpt-3.5-turbo</param>
        public ChatGPTClient(string openaiToken, string modelName = "gpt-3.5-turbo", string proxyUri = "")
        {
            _isUnofficialChatGptModel = _model.StartsWith("text-chat") || _model.StartsWith("text-davinci-002-render");
            _isChatGptModel = _model.StartsWith("gpt-3.5-turbo");
            _openAIToken = openaiToken;
            _proxyUri = proxyUri;
            _model = modelName;

            if (_isChatGptModel)
            {
                if (UserLabel.ToLower() == "user")
                {
                    UserLabel = null;
                }
                if (ChatGptLabel.ToLower() == "assistant")
                {
                    ChatGptLabel = null;
                }
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


            if (_isChatGptModel)
            {
                CompletionsUrl = "https://api.openai.com/v1/chat/completions";
            }
            else
            {
                CompletionsUrl = "https://api.openai.com/v1/completions";
            }

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
        /// send message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="conversationId"></param>
        /// <param name="parentMessageId"></param>
        /// <returns></returns>
        public async Task<ConversationResult> SendMessage(string message, string conversationId = "", string parentMessageId = "")
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

                var userMessage = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    ParentMessageId = parentMessageId,
                    Role = "User",
                    Content = message,
                };

                conversation.Messages.Add(userMessage);

                JObject result = new JObject();
                var reply = string.Empty;
                var resultJsonString = string.Empty;
                
                if (_isChatGptModel)
                {
                    List<JObject> messages = BuildChatPayload(conversation.Messages, userMessage.Id);
                    var data = await PostData(messages);
                    result = data.result;
                    reply = (string)result.SelectToken("choices[0].message.content");
                    resultJsonString = data.source;
                }
                else
                {
                    string prompt = BuildPrompt(conversation.Messages, userMessage.Id);
                    var data = await PostData(prompt);
                    result = data.result;
                    reply = (string)result.SelectToken("choices[0].text");
                    resultJsonString = data.source;
                }

                //存储
                if (!string.IsNullOrEmpty(EndToken))
                {
                    reply = reply.Replace(EndToken, "");
                }

                reply = reply.Trim();


                var replyMessage = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    ParentMessageId = userMessage.Id,
                    Role = "ChatGPT",
                    Content = reply,
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
                    Debug.WriteLine(ex);
                }
                
                return null;
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
            client.Timeout = TimeSpan.FromSeconds(20);

            string uri = "https://api.openai.com/v1/completions";
            if (_isChatGptModel)
            {
                uri = "https://api.openai.com/v1/chat/completions";
            }

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
            response.EnsureSuccessStatusCode();
            if (IsDebug)
            {
                Console.WriteLine(resultJsonString);
            }

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

        public List<JObject> BuildChatPayload(List<Message> messages, string parentMessageId)
        {
            var orderedMessages = GetMessagesForConversation(messages, parentMessageId);

           
            var currentDateString = DateTime.Now.ToString("MMMM d, yyyy");
            string systemMessage = $"You are ChatGPT, a large language model trained by OpenAI.\nCurrent date: {currentDateString}";

            var payload = new List<JObject>();

            bool isFirstMessage = true;
            int currentTokenCount = systemMessage != null ? (GetTokenCount(systemMessage) + _messageTokenOffset) : 0;
            int maxTokenCount = MaxPromptTokens;
 
            while (currentTokenCount < maxTokenCount && orderedMessages.Count > 0)
            {
                var message = orderedMessages.Last();
                orderedMessages.Remove(message);

                string messageString = message.Content;
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

                int newTokenCount = GetTokenCount(messageString) + currentTokenCount + _messageTokenOffset;
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

            if (systemMessage != null)
            {
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
            //中文的话词等于字？
            return text.Length;
        }

        private static List<Message> GetMessagesForConversation(List<Message> messages, string parentMessageId)
        {
            List<Message> orderedMessages = new List<Message>();
            string currentMessageId = parentMessageId;
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
