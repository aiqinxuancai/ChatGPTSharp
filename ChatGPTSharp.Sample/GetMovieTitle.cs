using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatGPTSharp.Sample
{
    internal class GetMovieTitle
    {
        const string kSystemMessage = """
            From the content I provide, identify the title of the work and return JSON in this format:
            { "title": <string> }
            Notes:
            1. Do not add explanations.
            2. The content may include titles in multiple languages, often separated by symbols like "/"; return the first language title.
            3. Do not treat fansub group names or subtitle tags as the title.
            4. Do not shorten, translate, or transform characters in the title.
            5. The title will not include brackets like [] or full-width brackets.
            Content:

            """;

        internal static async Task Test()
        {


            var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-5.2", "http://127.0.0.1:10809");
            //client.IsDebug = true;



            var titleFull = "[TeaHouse Fansubs] Spring 2024 New Anime [The Hero Is Dead!/Yuusha ga Shinda!/Hero Is Dead!][03-06][1080p][EN][Recruiting translators]";



            for (int i = 0; i < 100; i++)
            {
                var msg = await client.SendMessage($"{kSystemMessage}{titleFull}");
                Console.WriteLine(msg.Response);
            }
        }
    }
}
