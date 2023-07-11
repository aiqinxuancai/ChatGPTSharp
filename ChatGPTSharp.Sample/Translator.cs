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

            var sysMsg = "你将作为翻译官，请将以下内容翻译为中文，不要添加解释：\n"+
                "【メンテナンスのお知らせ】3月14日(火)13時～16時にメンテナンスを実施いたします。" +
                "メンテナンス中はゲームをプレイすることができません。" +
                "ご迷惑をおかけいたしますが、ご理解ご了承の程よろしくお願いいたします。#刀剣乱舞 #とうらぶ";
            var msg = await client.SendMessage(sysMsg, sendSystemType: Model.SendSystemType.None);
        }
    }
}
