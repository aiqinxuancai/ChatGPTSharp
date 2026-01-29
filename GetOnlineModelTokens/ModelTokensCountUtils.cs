using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

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

                var jsonObject = JsonNode.Parse(jsonString) as JsonObject;
                var models = jsonObject?["data"]?["models"] as JsonArray;

                if (models != null)
                {
                    foreach (var model in models.OfType<JsonObject>())
                    {
                        var slug = model["slug"]?.GetValue<string>();
                        if (string.IsNullOrEmpty(slug))
                        {
                            continue;
                        }

                        var modelName = slug.Replace("openai/", "");
                        var contextLength = model["context_length"]?.GetValue<int>() ?? 0;
                        if (contextLength > 0)
                        {
                            _modelContextLengths[modelName] = contextLength;
                        }
                    }
                }

                Console.WriteLine(JsonSerializer.Serialize(_modelContextLengths));

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
