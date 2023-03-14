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
            var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo", "http://127.0.0.1:1081");
            client.IsDebug = true;

            var sysMsg = "你将作为翻译官，我接下来会发送给你格式为[文本]的内容，你只需要将其中的文本翻译为中文发送给我就可以了";

            var jp = "【メンテナンスのお知らせ】3月14日(火)13時～16時にメンテナンスを実施いたします。" +
                "メンテナンス中はゲームをプレイすることができません。" +
                "ご迷惑をおかけいたしますが、ご理解ご了承の程よろしくお願いいたします。#刀剣乱舞 #とうらぶ";

            var msg6 = await client.SendMessage($"[{jp}]", sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
        }
    }
}
