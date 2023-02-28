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

        private string EndToken { set; get; } = "<|endoftext|>";
        private string SeparatorToken { set; get; } = "";

        private string[] Stop { set; get; } = { };

        private string OpenAIToken { set; get; } = "";

        public ChatGPTClient(string openaiToken)
        {
            OpenAIToken = openaiToken;
            var isChatGptModel = Model.StartsWith("text-chat") || Model.StartsWith("text-davinci-002-render");

            if (isChatGptModel)
            {
                this.EndToken = "<|im_end|>";
                this.SeparatorToken = "<|im_sep|>";
            }
            else
            {
                this.EndToken = "<|endoftext|>";
                this.SeparatorToken = this.EndToken;
            }

            if (isChatGptModel)
            {
                Stop = new string[] { this.EndToken, this.SeparatorToken, $"\n${this.UserLabel}:" };
            }
            else
            {
                Stop = new string[] { this.EndToken, $"\n{this.UserLabel}:" };
            }

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


                var prompt = BuildPrompt(conversation.Messages, userMessage.Id);

                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(20);
                string uri = "https://api.openai.com/v1/completions";

                // Request headers.
                client.DefaultRequestHeaders.Add(
                    "Authorization", $"Bearer {OpenAIToken}");

                JObject req = new JObject();
                req["model"] = Model;
                req["prompt"] = prompt;
                req["max_tokens"] = 1024;
                req["temperature"] = 0.8;
                req["top_p"] = 1;
                //req["frequency_penalty"] = 0;
                req["presence_penalty"] = 1;
                req["stop"] = new JArray(Stop);

                var jsonString = req.ToString();

                Console.WriteLine(jsonString);

                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();

                var resultJsonString = await response.Content.ReadAsStringAsync();

                Console.WriteLine(resultJsonString);


                Result result = JsonConvert.DeserializeObject<Result>(resultJsonString);

                var reply = result.Choices[0].Text;
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


                //this.ConversationsCache[conversationId] = conversation;

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
            string promptPrefix = $"\n{SeparatorToken}Instructions:\nYou are ChatGPT, a large language model trained by OpenAI.\nCurrent date: {currentDateString}{SeparatorToken}\n\n";

            var promptSuffix = $"{ChatGptLabel}:\n";
            var currentTokenCount = GetTokenCount($"{promptPrefix}{promptSuffix}");
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

        public int GetTokenCount(string text)
        {
            text = Regex.Replace(text, "<|im_end|>", "");
            text = Regex.Replace(text, "<|im_sep|>", "");
            //TODO gptEncode(text).length;
            //中文的话词等于字？
            return text.Length;
        }

        public static List<Message> GetMessagesForConversation(List<Message> messages, string parentMessageId)
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
