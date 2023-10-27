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
using TiktokenSharp;
using System.Runtime;

namespace ChatGPTSharp
{
    public class ChatGPTClient
    {
        private Dictionary<string, Conversation> _conversationsCache = new Dictionary<string, Conversation>();

        public ChatGPTClientSettings Settings { get; private set; }

        private TikToken _tiktoken;

        public bool IsDebug {
            get { return Settings.IsDebug; }
            set { Settings.IsDebug = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="openaiToken"></param>
        /// <param name="modelName">text-davinci-003、gpt-3.5-turbo</param>
        public ChatGPTClient(string openaiToken, string modelName = "gpt-3.5-turbo", string proxyUri = "", uint timeoutSeconds = 60)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                throw new ChatGPTException("modelName is null.");
            }
            if (string.IsNullOrEmpty(openaiToken))
            {
                throw new ChatGPTException("openaiToken is null.");
            }

            ChatGPTClientSettings settings = new ChatGPTClientSettings()
            { 
                 ModelName = modelName,
                 OpenAIToken = openaiToken,
                 ProxyUri = proxyUri,
                 TimeoutSeconds = timeoutSeconds
            };
            Settings = settings;
            _tiktoken = TikToken.EncodingForModel(settings.ModelName);
        }


        public ChatGPTClient(ChatGPTClientSettings settings)
        {
            if (string.IsNullOrEmpty(settings.ModelName))
            {
                throw new ChatGPTException("ModelName is null.");
            }
            if (string.IsNullOrEmpty(settings.OpenAIToken))
            {
                throw new ChatGPTException("OpenAIToken is null.");
            }


            Settings = settings;
            _tiktoken = TikToken.EncodingForModel(settings.ModelName);
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
                //Add current message to conversation
                
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
                
                if (Settings.IsChatGptModel)
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
                if (!string.IsNullOrEmpty(Settings.EndToken))
                {
                    reply = reply?.Replace(Settings.EndToken, "");
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
                if (Settings.IsDebug)
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

            if (!string.IsNullOrEmpty(Settings.ProxyUri))
            {
                WebProxy proxy = new WebProxy(Settings.ProxyUri);

                httpClientHandler.Proxy = proxy;
                httpClientHandler.UseProxy = true;
            }

            HttpClient client = new HttpClient(httpClientHandler);
            client.Timeout = TimeSpan.FromSeconds(Settings.TimeoutSeconds);

            string uri = Settings.CompletionsUrl;
            if (Settings.IsDebug)
            {
                Console.WriteLine(uri);
            }
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Settings.OpenAIToken}");

            JObject req = new JObject();
            req["model"] = Settings.ModelName;
            req["temperature"] = Settings.Temperature;
            req["top_p"] = Settings.TopP;
            req["presence_penalty"] = Settings.PresencePenalty;
            req["frequency_penalty"] = Settings.FrequencyPenalty;

            //req["max_tokens"] = 1024;

            if (Settings.IsChatGptModel)
            {
                req["messages"] = new JArray(((List<JObject>)obj).ToArray());
            }
            else
            {
                req["prompt"] = (string)obj;
                req["stop"] = new JArray(Settings.Stop);
            }

            var jsonString = req.ToString();

            if (Settings.IsDebug)
            {
                Console.WriteLine(jsonString);
            }

            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(uri, content);


            var resultJsonString = await response.Content.ReadAsStringAsync();
            if (Settings.IsDebug)
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
            string promptPrefix = $"\n{Settings.StartToken}Instructions:\nYou are ChatGPT, a large language model trained by OpenAI.\nCurrent date: {currentDateString}{Settings.StartToken}\n\n";

            var promptSuffix = $"{Settings.ChatGptLabel}:\n";
            var currentTokenCount = GetTokenCount($"{promptPrefix}{promptSuffix}"); //TODO
            var promptBody = string.Empty;
            var maxTokenCount = Settings.MaxPromptTokens;

            while (currentTokenCount < maxTokenCount && orderedMessages.Count > 0)
            {
                var message = orderedMessages.Last();
                orderedMessages.Remove(message);
                var roleLabel = message.Role == "User" ? Settings.UserLabel : Settings.ChatGptLabel;
                var messageString = $"{roleLabel}:\n{message.Content}{Settings.EndToken}\n";
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
            int currentTokenCount = systemMessage != null ? (GetTokenCount(systemMessage) + Settings.MessageTokenOffset) : 0;
            int maxTokenCount = Settings.MaxPromptTokens;

            //If the current token count has not exceeded and there are still messages in orderedMessages, continue. 
            //TODO: Prepare to add restrictions to support setting only the last * messages.
            while (currentTokenCount < maxTokenCount && orderedMessages.Count > 0)
            {
                var message = orderedMessages.Last();
                orderedMessages.Remove(message);

                string? messageString = message.Content;
                if (message.Role == "User")
                {
                    if (Settings.UserLabel != null)
                    {
                        messageString = $"{Settings.UserLabel}:\n{messageString}";
                    }
                    if (Settings.ChatGptLabel != null)
                    {
                        messageString = $"{messageString}\n{Settings.ChatGptLabel}:\n";
                    }
                }

                int newTokenCount = GetTokenCount(messageString!) + currentTokenCount + Settings.MessageTokenOffset;
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
                    { "content" , messageString }
                });
                isFirstMessage = false;
                currentTokenCount = newTokenCount;
            }

            if (!string.IsNullOrEmpty(systemMessage))
            {
                //Insert a system message before the last message.
                payload.Insert(payload.Count - 1, new JObject()
                {
                    { "role", "system" },
                    { "content", systemMessage }
                });
            }
            if (Settings.IsDebug)
            {
                Console.WriteLine($"Request expected consume {currentTokenCount} tokens.");
            }
            return payload;
        }


        private int GetTokenCount(string text)
        {
            text = Regex.Replace(text, "<|im_end|>", "");
            text = Regex.Replace(text, "<|im_sep|>", "");
            return _tiktoken.Encode(text).Count;
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
