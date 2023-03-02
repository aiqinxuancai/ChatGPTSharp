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
        private Dictionary<string, Conversation> ConversationsCache = new Dictionary<string, Conversation>();

        public int MaxContextTokens { set; get; } = 4097;
        public int MaxResponseTokens { set; get; } = 1024;
        public int MaxPromptTokens { set; get; } = 3073;
        public string UserLabel { set; get; } = "User";
        public string ChatGptLabel { set; get; } = "ChatGPT";
        public string Model { set; get; } = "text-davinci-003";

        public string CompletionsUrl { set; get; } = "";


        private string EndToken { set; get; } = "<|endoftext|>";
        private string StartToken { set; get; } = "";

        

        private string[] Stop { set; get; } = { };

        private string OpenAIToken { set; get; } = "";


        private bool IsChatGptModel { set; get; }
        private bool IsUnofficialChatGptModel { set; get; }
        private int MessageTokenOffset { set; get; } = 7;
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="openaiToken"></param>
        /// <param name="modelName">text-davinci-003、gpt-3.5-turbo</param>
        public ChatGPTClient(string openaiToken, string modelName = "text-davinci-003")
        {
            OpenAIToken = openaiToken;
            Model = modelName;
            this.IsUnofficialChatGptModel = Model.StartsWith("text-chat") || Model.StartsWith("text-davinci-002-render");


            var isChatGptModel = Model.StartsWith("gpt-3.5-turbo");
            IsChatGptModel = isChatGptModel;
            if (isChatGptModel)
            {
                if (this.UserLabel.ToLower() == "user")
                {
                    this.UserLabel = null;
                }
                if (this.ChatGptLabel.ToLower() == "assistant")
                {
                    this.ChatGptLabel = null;
                }
            }

            if (isChatGptModel)
            {
                this.StartToken = "";
                this.EndToken = "";
            }
            else if (IsUnofficialChatGptModel)
            {
                this.StartToken = "<|im_start|>";
                this.EndToken = "<|im_end|>";
            }
            else
            {
                this.StartToken = "<|endoftext|>";
                this.EndToken = this.StartToken;
            }

            if (!isChatGptModel)
            {
                if (IsUnofficialChatGptModel)
                {
                    Stop = new string[] { this.EndToken, this.StartToken, $"\n${this.UserLabel}:" };
                }
                else
                {
                    Stop = new string[] { this.EndToken, $"\n{this.UserLabel}:" };
                }
            }


            if (isChatGptModel)
            {
                this.CompletionsUrl = "https://api.openai.com/v1/chat/completions";
            }
            else
            {
                this.CompletionsUrl = "https://api.openai.com/v1/completions";
            }

        }

        public async Task<(JObject result, string source)> PostData(object obj)
        {
            // 创建代理
            var proxy = new WebProxy("http://127.0.0.1:1081");

            // 创建 HttpClientHandler 对象，并设置代理
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true
            };
            HttpClient client = new HttpClient(httpClientHandler);
            client.Timeout = TimeSpan.FromSeconds(20);

            string uri = "https://api.openai.com/v1/completions";
            if (IsChatGptModel)
            {
                uri = "https://api.openai.com/v1/chat/completions";
            }

            // Request headers.
            client.DefaultRequestHeaders.Add(
                "Authorization", $"Bearer {OpenAIToken}");

            JObject req = new JObject();
            req["model"] = Model;

            //req["max_tokens"] = 1024;
            req["temperature"] = 0.8;
            req["top_p"] = 1;
            //req["frequency_penalty"] = 0;
            req["presence_penalty"] = 1;


            if (IsChatGptModel)
            {
                req["messages"] = new JArray(((List<JObject >)obj).ToArray());
            }
            else
            {
                req["prompt"] = (string)obj;
                req["stop"] = new JArray(Stop);
            }


            var jsonString = req.ToString();

            Console.WriteLine(jsonString);

            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(uri, content);


            var resultJsonString = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            Console.WriteLine(resultJsonString);
            //var reply = string.Empty;
            JObject result = JObject.Parse(resultJsonString);
            return (result, resultJsonString);
        }



        public async Task<ConversationResult> SendMessage(string message, string conversationId = "", string parentMessageId = "")
        {
            try
            {
                conversationId = !string.IsNullOrEmpty(conversationId) ? conversationId : Guid.NewGuid().ToString();
                parentMessageId = !string.IsNullOrEmpty(parentMessageId) ? parentMessageId : Guid.NewGuid().ToString();

                ConversationsCache.TryGetValue(conversationId, out Conversation conversation);
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
                
                if (IsChatGptModel)
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
                if (!string.IsNullOrEmpty(this.EndToken))
                {
                    reply = reply.Replace(this.EndToken, "");
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

                ConversationsCache[conversationId] = conversation;

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
                Debug.WriteLine(ex);
                return null;
            }

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
            //this.modelOptions.max_tokens = Math.min(this.maxContextTokens - numTokens, this.maxResponseTokens);

            return prompt;
        }

        public List<JObject> BuildChatPayload(List<Message> messages, string parentMessageId)
        {
            var orderedMessages = GetMessagesForConversation(messages, parentMessageId);

           
            var currentDateString = DateTime.Now.ToString("MMMM d, yyyy");
            string systemMessage = $"You are ChatGPT, a large language model trained by OpenAI.\nCurrent date: {currentDateString}";

            var payload = new List<JObject>();

            bool isFirstMessage = true;
            int currentTokenCount = systemMessage != null ? (this.GetTokenCount(systemMessage) + this.MessageTokenOffset) : 0;
            int maxTokenCount = this.MaxPromptTokens;
            // Iterate backwards through the messages, adding them to the prompt until we reach the max token count.
            while (currentTokenCount < maxTokenCount && orderedMessages.Count > 0)
            {
                var message = orderedMessages.Last();
                orderedMessages.Remove(message);

                string messageString = message.Content;
                if (message.Role == "User")
                {
                    if (this.UserLabel != null)
                    {
                        messageString = $"{this.UserLabel}:\n{messageString}";
                    }
                    if (this.ChatGptLabel != null)
                    {
                        messageString = $"{messageString}\n{this.ChatGptLabel}:\n";
                    }
                }

                int newTokenCount = this.GetTokenCount(messageString) + currentTokenCount + this.MessageTokenOffset;
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
