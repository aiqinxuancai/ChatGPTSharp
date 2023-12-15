using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;

namespace ChatGPTSharp
{
    /// <summary>
    /// init settings
    /// </summary>
    public class ChatGPTClientSettings
    {
        private string _modelName = "gpt-3.5-turbo";

        /// <summary>
        /// Model name
        /// </summary>
        public string ModelName 
        { 
            set 
            {
                _modelName = value;

                //TODO auto tokens number, use regex "-(\d+)k\W" * 1024

                Regex regex = new Regex("-(\\d+)k\\W");
                var match = regex.Match(_modelName);
                bool findTokenNumber = false;

                if (match.Success)
                {
                    var tokens = match.Groups[1].Value;
                    if (int.TryParse(tokens, out int toukensNumber)) {
                        MaxContextTokens = toukensNumber * 1024;
                        MaxResponseTokens = 1024;
                        MaxPromptTokens = MaxContextTokens - MaxResponseTokens;
                        findTokenNumber = true;
                    }
                }

                if (!findTokenNumber)
                {
                    if (_modelName.StartsWith("gpt-4-32k")) //32768
                    {
                        MaxContextTokens = 32768;
                        MaxResponseTokens = 1024;
                        MaxPromptTokens = MaxContextTokens - MaxResponseTokens;
                    }
                    else if (_modelName.StartsWith("gpt-4")) //8192
                    {
                        MaxContextTokens = 8192;
                        MaxResponseTokens = 1024;
                        MaxPromptTokens = MaxContextTokens - MaxResponseTokens;
                    }
                    else if (_modelName.StartsWith("gpt-3.5-turbo-16k")) //8192
                    {
                        MaxContextTokens = 16384;
                        MaxResponseTokens = 1024;
                        MaxPromptTokens = MaxContextTokens - MaxResponseTokens;
                    }
                    else
                    {
                        MaxContextTokens = 4096;
                        MaxResponseTokens = 1024;
                        MaxPromptTokens = MaxContextTokens - MaxResponseTokens;
                    }
                }

                UpdateCompletionsUrl();

            }
            get { 
                return _modelName; 
            } 
        }


        private void UpdateCompletionsUrl()
        {
            UriBuilder uriBuilder = new UriBuilder(APIURL);

            uriBuilder.Path = "/v1/chat/completions";

            CompletionsUrl = uriBuilder.Uri.AbsoluteUri;
        }


        /// <summary>
        /// OpenAI key
        /// </summary>
        public string OpenAIToken { set; get; } = string.Empty;

        /// <summary>
        /// The maximum total number of tokens. After setting ModelName, this configuration will be automatically updated. If you need to customize it, please modify it after setting ModelName.
        /// </summary>
        public int MaxContextTokens { set; get; } = 4096;

        /// <summary>
        /// The maximum number of tokens that the API returns. Usually kept at 1024. If you want to include more prompts as much as possible, you can consider modifying this value. After setting ModelName, this configuration will be automatically updated. If you need to customize it, please modify it after setting ModelName.
        /// </summary>
        public int MaxResponseTokens { set; get; } = 1024;

        /// <summary>
        /// The maximum number of Prompt tokens. Usually kept as MaxContextTokens - MaxResponseTokens. If you want to include more prompts as much as possible, you can consider increasing this value and reducing MaxResponseTokens. After setting ModelName, this configuration will be automatically updated. If you need to customize it, please modify it after setting ModelName.
        /// </summary>
        public int MaxPromptTokens { set; get; } = 3072;

        /// <summary>
        /// Whether to output debug information in the console
        /// </summary>
        public bool IsDebug { set; get; }


        private string _APIURL = "https://api.openai.com/";


        /// <summary>
        /// Custom base URI for API access, suitable for building reverse proxies. This is not mutually exclusive with ProxyUri, but it is recommended to use only one of them.
        /// </summary>
        public string APIURL 
        { 
            set 
            {
                _APIURL = value;
                UpdateCompletionsUrl(); 
            } 
            get 
            { 
                return _APIURL; 
            } 
        }

        public string CompletionsUrl { get; private set; } = string.Empty;


        /// <summary>
        /// Reserved token offset
        /// </summary>
        public int MessageTokenOffset { set; get; } = 7;

        /// <summary>
        /// Use a proxy address to access OpenAI's API, for example: http://127.0.0.1:1080/
        /// </summary>
        public string ProxyUri { set; get; } = string.Empty;

        /// <summary>
        /// Timeout period. In GPT-4, it is recommended to set a higher value due to congestion.
        /// </summary>
        public uint TimeoutSeconds { set; get; } = 60;

        /// <summary>
        /// Defaults to 1
        /// What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.
        /// We generally recommend altering this or top_p but not both.
        /// </summary>
        public double Temperature { set; get; } = 1;

        /// <summary>
        /// Defaults to 1
        /// An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass.So 0.1 means only the tokens comprising the top 10% probability mass are considered.
        /// We generally recommend altering this or temperature but not both.
        /// </summary>
        public double TopP { set; get; } = 1;

        /// <summary>
        /// Defaults to 0
        /// Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.
        /// </summary>
        public double PresencePenalty { set; get; } = 0;

        /// <summary>
        /// Defaults to 0
        /// Number between -2.0 and 2.0. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.
        /// </summary>
        public double FrequencyPenalty { set; get; } = 0;


    }
}
