using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatGPTSharp.Sample
{
    internal class GroupAdminTest
    {
        /// <summary>
        /// 实用案例
        /// </summary>
        /// <returns></returns>
        internal static async Task Test() 
        {
            var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo", "http://127.0.0.1:1081");
            //client.IsDebug = true;

            //var sysMsg = "你将作为一个群管理员，我将会按照{[谁][说了什么]}，这样的格式告诉你，你只需要回复我一个从0到10的数值，来表示他的发言涉及政治内容的严重程度。";

            var sysMsg = "你将作为一个狼人杀的法官，我将会按照{[谁][说了什么]}，这样的格式告诉你，你只需要回复我一个格式为{[从0到10的数值，来表示他的发言贴脸的程度],[从0到10的数值，来表示他的发言情绪是否激烈]}这样包含两个数值的json格式文本，其中贴脸的意思是企图用非游戏内的逻辑来证明自己的身份，比如说“我要不是XXX，就死全家”这样的言论。";

            var msg = await client.SendMessage("{[小明][妈的你们这群人怎么都不信我，我才是预言家，你们是傻吧]}", sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
            Console.WriteLine($"Data:{msg.Response}  {msg.ConversationId}, {msg.MessageId}");

            var msg2 = await client.SendMessage("{[小红][我觉得这局全票出6]}",conversationId:msg.ConversationId, parentMessageId:msg.MessageId,  sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
            Console.WriteLine($"Data:{msg2.Response}  {msg2.ConversationId}, {msg2.MessageId}");

            var msg4 = await client.SendMessage("{[小白][我没啥可说的]}", conversationId: msg2.ConversationId, parentMessageId: msg2.MessageId, sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
            Console.WriteLine($"Data:{msg4.Response}  {msg4.ConversationId}, {msg4.MessageId}");

            var msg5 = await client.SendMessage("{[小绿][我要是不是预言家，我就把手机吃了]}", conversationId: msg4.ConversationId, parentMessageId: msg4.MessageId, sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
            Console.WriteLine($"Data:{msg5.Response}  {msg5.ConversationId}, {msg5.MessageId}");
        }

    }
}
