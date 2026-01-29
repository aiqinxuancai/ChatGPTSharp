using System;
using System.Collections.Generic;

namespace ChatGPTSharp
{
    /// <summary>
    /// init settings
    /// </summary>
    public class ChatGPTClientSettings
    {
        private string _modelName = "gpt-4o-mini";

        public ChatGPTClientSettings()
        {
            UpdateApiUrls();
        }

        public string ModelName
        {
            set
            {
                _modelName = value;
                UpdateApiUrls();
            }
            get
            {
                return _modelName;
            }
        }

        private void UpdateCompletionsUrl()
        {
            UpdateApiUrls();
        }

        /// <summary>
        /// OpenAI key
        /// </summary>
        public string OpenAIKey { set; get; } = string.Empty;

        /// <summary>
        /// Whether to output debug information in the console
        /// </summary>
        public bool IsDebug { set; get; }

        /// <summary>
        /// Extra request fields merged into the request body.
        /// </summary>
        public IDictionary<string, object?>? ExtraBody { get; set; }

        private string _baseUrl = "https://api.openai.com/";

        /// <summary>
        /// Sets a custom base URI for accessing the API, useful for scenarios requiring a reverse proxy for network configurations or specific deployment requirements. 
        /// The default value is "https://api.openai.com/". 
        /// This property can be used alongside ProxyUri, but it is generally recommended to use only one to avoid potential routing conflicts or redundancy. 
        /// Adjusting this property also updates the internal API completion URLs.
        /// </summary>
        public string BaseUrl
        {
            set
            {
                _baseUrl = value;
                UpdateApiUrls();
            }
            get
            {
                return _baseUrl;
            }
        }

        public string CompletionsUrl { get; private set; } = string.Empty;
        public string ResponsesUrl { get; private set; } = string.Empty;
        public string ConversationsUrl { get; private set; } = string.Empty;

        /// <summary>
        /// Specifies a proxy address for accessing OpenAI's API. 
        /// This can be useful for routing requests through a specific network configuration or for accessing the API from regions with restricted internet access. 
        /// For example, use "http://127.0.0.1:1080/" to direct requests through a local proxy server.
        /// </summary>
        public string ProxyUri { set; get; } = string.Empty;

        /// <summary>
        /// Timeout seconds.
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

        private void UpdateApiUrls()
        {
            var root = NormalizeBaseUrl(BaseUrl);
            CompletionsUrl = $"{root}/v1/chat/completions";
            ResponsesUrl = $"{root}/v1/responses";
            ConversationsUrl = $"{root}/v1/conversations";
        }

        private static string NormalizeBaseUrl(string baseUrl)
        {
            UriBuilder uriBuilder = new UriBuilder(baseUrl);
            var path = (uriBuilder.Path ?? string.Empty).TrimEnd('/');

            if (path.EndsWith("/v1/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - "/v1/chat/completions".Length);
            }
            else if (path.EndsWith("/v1/responses", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - "/v1/responses".Length);
            }
            else if (path.EndsWith("/v1/conversations", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - "/v1/conversations".Length);
            }
            else if (path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - "/v1".Length);
            }

            uriBuilder.Path = path;
            return uriBuilder.Uri.AbsoluteUri.TrimEnd('/');
        }
    }
}
