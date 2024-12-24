using HtmlAgilityPack;
using Flurl.Http;
using System.Text.Json.Nodes;

namespace GetOnlineModelTokens
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();


        }
        static async Task MainAsync(string[] args)
        {
            var model = new ModelTokensCountUtils();
            await model.FetchModelsAsync("https://openrouter.ai/api/frontend/models/find?fmt=table&q=openai");



            //model.ToString();
        }

        static void GetModelTokens()
        {

            string htmlContent = File.ReadAllText("model.html"); // 替换为您的HTML字符串
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            var rows = htmlDoc.DocumentNode.SelectNodes("//table/tbody/tr");

            JsonObject keyValuePairs = new JsonObject();

            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var cells = row.SelectNodes("td");
                    if (cells != null && cells.Count > 2)
                    {

                        string modelName = cells[0].InnerText.Trim();
                        string tokenCount = cells[2].InnerText.Trim();

                        if (modelName.EndsWith("Legacy"))
                        {
                            modelName = modelName.Replace("Legacy", "");
                        }

                        if (tokenCount.EndsWith("tokens"))
                        {
                            var toukens = tokenCount.Replace(",", "");
                            toukens = toukens.Replace(" tokens", "");
                            Console.WriteLine($"Model: {modelName}, Token Count: {toukens}");


                            keyValuePairs[modelName] = int.Parse(toukens);

                        }

                    }
                }
            }

            Console.WriteLine(keyValuePairs);
        }
    }
}
