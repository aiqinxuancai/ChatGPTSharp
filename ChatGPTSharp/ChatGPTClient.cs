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

        /// <summary>
        /// gpt-4-vision-preview
        /// </summary>
        private bool _isVisionModel;

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

            _isVisionModel = modelName.Contains("-vision");
            _tiktoken = TikToken.EncodingForModel(settings.ModelName);

            Settings = settings;
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
        /// <param name="systemPrompt">https://platform.openai.com/docs/guides/chat </param>
        /// <param name="images">set a local image file or an image URL; if it is a local image, it will be converted to base64 for transmission.</param>
        /// <returns></returns>
        public async Task<ConversationResult> SendMessage(string message, 
            string conversationId = "", 
            string parentMessageId = "", 
            string systemPrompt = "", 
            List<string>? images = null)
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
                        Messages = new List<ChatMessage>(),
                        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    };
                }

                JToken content;

                if (_isVisionModel)
                {
                    //content.Type = JTokenType.String;
                    var j = new JArray();
                    if (!string.IsNullOrEmpty(message))
                    {
                        //{
                        //    "type": "text",
                        //    "text": "What are in these images? Is there any difference between them?",
                        //},
                        j.Add(new JObject { { "type", "text" }, { "text", message } });
                    }
                   
                    //image token
                    /*

                            {
                              "type": "image_url",
                              "image_url": {
                                "url": "https://upload.wikimedia.org/wikipedia/commons/thumb/d/dd/Gfp-wisconsin-madison-the-nature-boardwalk.jpg/2560px-Gfp-wisconsin-madison-the-nature-boardwalk.jpg",
                              },
                            },
 
                     */
                }
                else
                {
                    content = new JValue(message);
                }
                

                //把当前消息加入会话
                var userMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    ParentMessageId = parentMessageId,
                    Role = "User",
                    Content = content,
                };

                conversation.Messages.Add(userMessage);

                JObject result = new JObject();
                string? reply = string.Empty;
                var resultJsonString = string.Empty;

                List<JObject> messages = BuildChatPayload(conversation.Messages, userMessage.Id, systemPrompt);
                var data = await PostData(messages);
                result = data.result;
                reply = (string?)result.SelectToken("choices[0].message.content");
                resultJsonString = data.source;

                reply = reply?.Trim();

                //userMessage.UsageTokens = result.SelectToken("usage.prompt_tokens").ToObject<int>();

                var replyMessage = new ChatMessage
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

            req["messages"] = new JArray(((List<JObject>)obj).ToArray());



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

        public List<JObject> BuildChatPayload(List<ChatMessage> messages, string parentMessageId, string systemPrompt = "")
        {
            var orderedMessages = GetMessagesForConversation(messages, parentMessageId);

            var payload = new List<JObject>();

            bool isFirstMessage = true;
            int currentTokenCount = systemPrompt != null ? (GetTokenCount(systemPrompt) + Settings.MessageTokenOffset) : 0;
            int maxTokenCount = Settings.MaxPromptTokens;

            //If the current token count has not exceeded and there are still messages in orderedMessages, continue. 
            //TODO: Prepare to add restrictions to support setting only the last * messages.
            while (currentTokenCount < maxTokenCount && orderedMessages.Count > 0)
            {
                var message = orderedMessages.Last();
                orderedMessages.Remove(message);

                string? messageString = message.Content;

                //TODO 

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

            if (!string.IsNullOrEmpty(prompt))
            {
                //Insert a system message before the last message.
                payload.Insert(payload.Count - 1, new JObject()
                {
                    { "role", "system" },
                    { "content", prompt }
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
            return _tiktoken.Encode(text).Count;
        }

        private static List<ChatMessage> GetMessagesForConversation(List<ChatMessage> messages, string parentMessageId)
        {
            List<ChatMessage> orderedMessages = new List<ChatMessage>();
            string? currentMessageId = parentMessageId;
            while (currentMessageId != null)
            {
                ChatMessage message = messages.Find(m => m.Id == currentMessageId);
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
