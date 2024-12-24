using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GetOnlineModelTokens
{
    public class ModelTokensCountUtils
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, int> _modelContextLengths;

        public ModelTokensCountUtils()
        {
            _httpClient = new HttpClient();
            _modelContextLengths = new Dictionary<string, int>();
        }

        public async Task FetchModelsAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();

                var jsonObject = JObject.Parse(jsonString);
                var models = jsonObject["data"]["models"];

                foreach (var model in models)
                {
                    string slug = model["slug"].ToString();
                    string modelName = slug.Replace("openai/", "");

                    int contextLength = model["context_length"].Value<int>();
                    _modelContextLengths[modelName] = contextLength;
                }

                Console.WriteLine(JsonConvert.SerializeObject(_modelContextLengths));

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching or processing model data: {ex.Message}");
            }
        }

        public Dictionary<string, int> GetModelContextLengths()
        {
            return _modelContextLengths;
        }
    }
}
