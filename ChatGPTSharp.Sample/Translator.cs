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
            var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-5.2", "http://127.0.0.1:10809");
            client.IsDebug = true;


            var file = File.ReadAllText("asao_monogatari.txt");

            var sysMsg = "Please translate the following content into English. Do not add explanations:\n" +
                file.Substring(0, file.Length / 4);
            var msg = await client.SendMessage(sysMsg);
        }
    }
}
