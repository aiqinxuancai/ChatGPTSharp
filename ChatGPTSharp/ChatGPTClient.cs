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
using Newtonsoft.Json.Serialization;

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
            List<ChatImageContent>? images = null)
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

                JToken content = new JValue("");  //JTokenType.String;

                if (_isVisionModel)
                {
                    var j = new JArray();
                    if (!string.IsNullOrEmpty(message))
                    {
                        j.Add(new JObject { { "type", "text" }, { "text", message } });
                    }

                    if (images != null && images.Count > 0)
                    {
                        foreach (ChatImageContent item in images)
                        {
                            JObject url = new JObject();
                            url["url"] = item.Url;

                            JObject imageContent = new JObject
                                {
                                    { "type", "image_url" },
                                    { "image_url", url }
                                };
                            j.Add(imageContent);
                        }

                    }
                }
                else
                {
                    content = new JValue(message);  //JTokenType.String;
                }
                

                var userMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    ParentMessageId = parentMessageId,
                    Role = RoleType.User,
                    Content = content,
                    //TODO  UsageTokens = List<ChatMessage> messages 
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
                    Role = RoleType.Assistant,
                    Content = new JValue(reply),
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
                Console.WriteLine("req:" + jsonString);
            }

            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(uri, content);

            var resultJsonString = await response.Content.ReadAsStringAsync();
            if (Settings.IsDebug)
            {
                Console.WriteLine("rsp:" + resultJsonString);
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

            var systemPromptJson = new JObject() { { "role", "system" }, { "content", systemPrompt } };


            int currentTokenCount = string.IsNullOrEmpty(systemPrompt) ?  0 : GetTokensForSingleMessage(systemPromptJson);
            int maxTokenCount = Settings.MaxPromptTokens;


            while (currentTokenCount < maxTokenCount && orderedMessages.Count > 0)
            {
                var message = orderedMessages.Last();
                orderedMessages.Remove(message);

                var messageString = message.Content;

                int newTokenCount = 0;

                JObject msgJson = new JObject();

                if (_isVisionModel)
                {
                    //TODO newTokenCount = GetTokenCount(messageString!) + currentTokenCount + Settings.MessageTokenOffset;
                }
                else
                {
                    msgJson = new JObject() { { "role", message.Role == RoleType.User ? "user" : "assistant" }, { "content", messageString } };
                    var currentMessageToken = GetTokensForSingleMessage(msgJson);
                    newTokenCount = currentMessageToken + currentTokenCount;

                    if (Settings.IsDebug)
                    {
                        Console.WriteLine($"[Tokens]:{msgJson.ToString(Formatting.None)} {currentMessageToken} {newTokenCount}");
                    }

                }

                if (newTokenCount > maxTokenCount)
                {
                    if (!isFirstMessage)
                    {
                        break;
                    }
                    throw new Exception($"Prompt is too long. Max token count is {maxTokenCount}, but prompt is {newTokenCount} tokens long.");
                }

                payload.Insert(0, msgJson);
                isFirstMessage = false;
                currentTokenCount = newTokenCount;
            }

            if (!string.IsNullOrEmpty(systemPrompt))
            {
                payload.Insert(payload.Count - 1, systemPromptJson);
            }

            //from documentation https://platform.openai.com/docs/guides/text-generation/managing-tokens 
            currentTokenCount += 2;

            //I tested using the method in the documentation and found that the token count calculated using the algorithm in the documentation
            //is inconsistent with the prompt_tokens returned. I'm not sure if the documentation was not updated in time or if there is another issue.
            //After adding the following code, it seems to be consistent with prompt_tokens.
            currentTokenCount = currentTokenCount  - (payload.Count - 1);

            if (Settings.IsDebug)
            {
                Console.WriteLine($"[Prompt Tokens]:{currentTokenCount} MsgCount:{payload.Count}");
            }
            return payload;
        }


        private int GetTokenCount(string text)
        {
            return _tiktoken.Encode(text).Count;
        }

        /// <summary>
        /// https://platform.openai.com/docs/guides/text-generation/managing-tokens Counting tokens for chat API calls
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        public int GetTokensForMessages(List<JObject> messages)
        {
            int num_tokens = 2; 
            foreach (var message in messages)
            {
                num_tokens += GetTokensForSingleMessage(message);
            }
            return num_tokens;
        }


        /// <summary>
        /// https://platform.openai.com/docs/guides/text-generation/managing-tokens Counting tokens for chat API calls
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private int GetTokensForSingleMessage(JObject message)
        {
            int tokens = 4;   // every message follows <im_start>{role/name}\n{content}<im_end>\n
            foreach (var property in message)
            {
                string key = property.Key;
                string value = property.Value.ToString();
                var token = GetTokenCount(value);
                tokens += token;
                if (Settings.IsDebug)
                {
                    Console.WriteLine($"[GetTokensForSingleMessage]:{value} +{token} = {tokens}");
                }


                if (key == "name")
                {
                    tokens -= 1;
                }
            }
            return tokens;
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
