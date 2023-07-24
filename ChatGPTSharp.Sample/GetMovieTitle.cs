using Newtonsoft.Json.Linq;
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
            请从我提供的内容中告诉我这个作品的名称，请返回如下JSON格式：
            { "title": <string> }
            请注意：
            1.不要添加解释。
            2.内容中可能包含多种语言的作品名称，通常可能会使用符号/进行分割，请返回第一种语言名称。
            3.请避免将字幕组名称及字幕名称等识别为标题。
            4.不要对作品名称进行删减或翻译以及字符的转换。
            5.作品名称不会包含包含[]【】等符号。
            以下为内容：

            """;

        internal static async Task Test()
        {


            var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo", "http://127.0.0.1:10809");
            //client.IsDebug = true;



            var titleFull = "【喵萌奶茶屋】★04月新番★[勇者死了！/勇者が死んだ！/Yuusha ga Shinda!][03-06][1080p][繁体][招募翻译校对]";



            for (int i = 0; i < 100; i++)
            {
                var msg = await client.SendMessage($"{kSystemMessage}{titleFull}");
                Console.WriteLine(JObject.Parse(msg.Response)["title"]);
            }
        }
    }
}
