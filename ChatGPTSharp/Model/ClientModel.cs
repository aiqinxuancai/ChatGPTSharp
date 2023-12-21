using ChatGPTSharp.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;
using TiktokenSharp;

namespace ChatGPTSharp.Model
{
    public enum ContentType
    {
        None,
        Text,
        ImageUrl,
    }

    public enum RoleType
    {
        User,
        Assistant,
        System,
    }

    public class Conversation
    {
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public long CreatedAt { get; set; }
    }

    public class ChatMessage
    {
        public string? Id { get; set; }
        public string? ParentMessageId { get; set; }

        public bool IsVisionModel { get; set; }

        public RoleType Role { get; set; }

        public string? TextContent { get; set; }

        public List<ChatImageModel> ImageContent { get; set; } = new List<ChatImageModel> { };


        public (JObject body, int tokens) GetTokens (TikToken tikToken)
        {
            var body = MessageBody;
            var textTokens = TokenUtils.GetTokensForSingleMessage(tikToken, body);

            if (ImageContent != null)
            {
                foreach (var item in ImageContent)
                {
                    textTokens += item.TokensCount;
                }
                
            }


            return (body, textTokens);
        }

        public JObject MessageBody
        {
            get
            {
                JToken messageBody = new JObject();
                if (IsVisionModel)
                {
                    var j = new JArray();
                    if (!string.IsNullOrEmpty(TextContent))
                    {
                        j.Add(new JObject { { "type", "text" }, { "text", TextContent } });
                    }

                    if (ImageContent != null && ImageContent.Count > 0)
                    {
                        foreach (ChatImageModel item in ImageContent)
                        {
                            JObject url = new JObject();
                            url["url"] = item.Url;

                            switch (item.Mode)
                            {
                                case ImageDetailMode.Auto:
                                    {
                                        url["detail"] = "auto";
                                        break;
                                    }
                                case ImageDetailMode.Low:
                                    {
                                        url["detail"] = "low";
                                        break;
                                    }
                                case ImageDetailMode.High:
                                    {
                                        url["detail"] = "high";
                                        break;
                                    }

                            }

                            //url["tokensCount"] = item.TokensCount;
                            JObject imageContent = new JObject
                                {
                                    { "type", "image_url" },
                                    { "image_url", url }
                                };
                            j.Add(imageContent);
                        }

                    }
                    messageBody = j;
                }
                else
                {
                    messageBody = new JValue(TextContent);  //JTokenType.String;
                }

                return new JObject() { { "role", Role == RoleType.User ? "user" : "assistant" }, { "content", messageBody } };
            }
        }



        

    }

    public class ConversationResult
    {
        public JToken? Response { get; set; }
        public string? ConversationId { get; set; }
        public string? MessageId { get; set; }
        public string? Details { get; set; }
    }
}
