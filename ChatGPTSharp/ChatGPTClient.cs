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
using ChatGPTSharp.Utils;

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


        #region mark custom conversationId

        /// <summary>
        /// Clear conversation
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
            List<ChatImageModel>? images = null)
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


                var userMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    IsVisionModel = Settings.IsVisionModel,
                    ParentMessageId = parentMessageId,
                    Role = RoleType.User,
                    TextContent = message,
                    ImageContent = images
                };

                conversation.Messages.Add(userMessage);

                JObject result = new JObject();
                string? reply = string.Empty;
                var resultJsonString = string.Empty;

                var messages = BuildChatPayload(conversation.Messages, userMessage.Id, systemPrompt);
                var data = await PostData(messages.message); //SendMessage
                result = data.result;
                reply = (string?)result.SelectToken("choices[0].message.content");
                resultJsonString = data.source;

                reply = reply?.Trim();

                //TODO 重新计算userMessage中的token量？
                //userMessage.


                var replyMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    IsVisionModel = Settings.IsVisionModel,
                    ParentMessageId = userMessage.Id,
                    Role = RoleType.Assistant,
                    TextContent = reply,//Content = new JValue(reply),
                    //UsageTokens = result.SelectToken("usage.completion_tokens")!.ToObject<int>(),
                };

                conversation.Messages.Add(replyMessage);

                _conversationsCache[conversationId] = conversation;

                return new ConversationResult()
                {
                    Response = replyMessage.TextContent,
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


        /// <summary>
        /// Post List<JObject> data.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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

            //https://platform.openai.com/docs/guides/vision
            //Currently, GPT-4 Turbo with vision does not support the message.name parameter,
            //functions/tools, response_format parameter,
            //and we currently set a low max_tokens default which you can override.
            if (Settings.MaxResponseTokens > 0)
            {
                req["max_tokens"] = Settings.MaxResponseTokens;
            }

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

        public (List<JObject> message, int tokensCount) BuildChatPayload(List<ChatMessage> messages, string parentMessageId, string systemPrompt = "")
        {
            var orderedMessages = GetMessagesForConversation(messages, parentMessageId);

            var payload = new List<JObject>();

            bool isFirstMessage = true;

            var systemPromptJson = new JObject() { { "role", "system" }, { "content", systemPrompt } };

            int currentTokenCount = string.IsNullOrEmpty(systemPrompt) ? 0 : TokenUtils.GetTokensForSingleMessage(_tiktoken, systemPromptJson);

            int maxTokenCount = Settings.MaxPromptTokens;
            if (maxTokenCount <= 0)
            {
                maxTokenCount = Settings.MaxContextTokens;
            }

            while (currentTokenCount < maxTokenCount && orderedMessages.Count > 0)
            {
                var message = orderedMessages.Last();
                orderedMessages.Remove(message);

                var tokensResult = message.GetTokens(_tiktoken);

                int newTokenCount = tokensResult.tokens + currentTokenCount;

                if (newTokenCount > maxTokenCount)
                {
                    if (!isFirstMessage)
                    {
                        break;
                    }
                    throw new Exception($"Prompt is too long. Max token count is {maxTokenCount}, but prompt is {newTokenCount} tokens long.");
                }

                payload.Insert(0, tokensResult.body);

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
            currentTokenCount = currentTokenCount - (payload.Count - 1);

            if (Settings.IsDebug)
            {
                Console.WriteLine($"[Prompt Tokens]:{currentTokenCount} MsgCount:{payload.Count}");
            }
            return (payload, currentTokenCount);
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
        #endregion






    }


}
