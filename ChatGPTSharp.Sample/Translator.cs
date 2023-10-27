using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatGPTSharp.Sample
{
    internal class Translator
    {
        internal static async Task Test()
        {
            // 242 tokens
            var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo-16k", "http://127.0.0.1:10809");
            client.IsDebug = true;


            var file = File.ReadAllText("asao_monogatari.txt");

            var sysMsg = "你将作为翻译官，请将以下内容翻译为中文，不要添加解释：\n"+
                //"You will be acting as a translator, " +
                //"please translate the following content into " +
                //"Chinese without adding explanations\n" +
                file.Substring(0, file.Length / 4);
            var msg = await client.SendMessage(sysMsg, sendSystemType: Model.SendSystemType.None);
        }
    }
}
