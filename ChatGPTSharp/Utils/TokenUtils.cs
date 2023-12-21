using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TiktokenSharp;

namespace ChatGPTSharp.Utils
{
    public class TokenUtils
    {

        public static Dictionary<string, int> GetTokenLimitWithOpenAI()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "ChatGPTSharp.Assets.ModelTokens.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
            }
        }


        /// <summary>
        /// https://platform.openai.com/docs/guides/text-generation/managing-tokens Counting tokens for chat API calls
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        public static int GetTokensForMessages(TikToken tikToken, List<JObject> messages)
        {
            int num_tokens = 2;
            foreach (var message in messages)
            {
                num_tokens += GetTokensForSingleMessage(tikToken, message);
            }
            return num_tokens;
        }


        /// <summary>
        /// https://platform.openai.com/docs/guides/text-generation/managing-tokens Counting tokens for chat API calls
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static int GetTokensForSingleMessage(TikToken tikToken, JObject message)
        {
            int tokens = 4;   // every message follows <im_start>{role/name}\n{content}<im_end>\n
            foreach (var property in message)
            {
                string key = property.Key;

                if (property.Value?.Type == JTokenType.Array && key == "content")
                {
                    foreach (JObject msg in property.Value)
                    {
                        if (msg.ContainsKey("type"))
                        {
                            string msgType = (string)msg["type"]!;
                            switch (msgType)
                            {
                                case "text":
                                    var token = tikToken.Encode((string)msg["text"]!).Count;
                                    tokens += token;
                                    break;
                                case "image_url":
                                    //??
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    string value = property.Value.ToString();
                    var token = tikToken.Encode(value).Count;
                    tokens += token;
                    //if (Settings.IsDebug)
                    //{
                    //    Console.WriteLine($"[GetTokensForSingleMessage]:{value} +{token} = {tokens}");
                    //}
                }

                if (key == "name")
                {
                    tokens -= 1;
                }
            }
            return tokens;
        }
    }
}
