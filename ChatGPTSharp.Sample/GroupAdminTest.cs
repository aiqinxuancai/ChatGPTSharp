﻿using System;
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
        /// Practical Example
        /// </summary>
        /// <returns></returns>
        internal static async Task Test()
        {
            var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo");
            client.IsDebug = true;

            //var sysMsg = "你将作为一个群管理员，我将会按照{[谁][说了什么]}，这样的格式告诉你，你只需要回复我一个从0到10的数值，来表示他的发言涉及政治内容的严重程度。";
            //var sysMsg = "You will serve as a group administrator, and I will inform you in the format {[Who][What]}, where you only need to reply with a number from 0 to 10 to indicate the severity of political content in their speech.";

            var sysMsg = "你将作为一个狼人杀的法官，我将会按照{[谁][说了什么]}，这样的格式告诉你，你只需要回复我一个格式为{[从0到10的数值，来表示他的发言贴脸的程度],[从0到10的数值，来表示他的发言情绪是否激烈]}这样包含两个数值的json格式文本，其中贴脸的意思是企图用非游戏内的逻辑来证明自己的身份，比如说“我要不是XXX，就死全家”这样的言论。";
            //var sysMsg = "You will serve as a judge in a game of Werewolf, and I will inform you in the format {[Who][What]}. You only need to reply with a JSON text containing two values in the format {[A number from 0 to 10 indicating the degree of their speech being suspiciously related to the game],[A number from 0 to 10 indicating the intensity of their emotional expression in the speech]}. 'Suspiciously related to the game' means attempting to prove one's identity using logic outside the game, such as saying 'If I'm not XXX, then my whole family should die.'"
            //var msg = await client.SendMessage("{[小明][妈的你们这群人怎么都不信我，我才是预言家，你们是傻吧]}", sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
            //Console.WriteLine($"Data:{msg.Response}  {msg.ConversationId}, {msg.MessageId}");

            //var msg2 = await client.SendMessage("{[小红][我觉得这局全票出6]}",conversationId:msg.ConversationId, parentMessageId:msg.MessageId,  sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
            //Console.WriteLine($"Data:{msg2.Response}  {msg2.ConversationId}, {msg2.MessageId}");

            //var msg4 = await client.SendMessage("{[小白][我没啥可说的]}", conversationId: msg2.ConversationId, parentMessageId: msg2.MessageId, sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
            //Console.WriteLine($"Data:{msg4.Response}  {msg4.ConversationId}, {msg4.MessageId}");

            //var msg5 = await client.SendMessage("{[小绿][我要是不是预言家，我就把手机吃了]}", conversationId: msg4.ConversationId, parentMessageId: msg4.MessageId, sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
            //Console.WriteLine($"Data:{msg5.Response}  {msg5.ConversationId}, {msg5.MessageId}");


            sysMsg = "你将作为一个群管理员审查群消息，我将会按照{[谁][说了什么]}，这样的格式告诉你，" +
                "你只需要回复我一个从0到10的数字来表示他的发言涉及政治内容的严重程度，比如\"0\"，" +
                "无需回复其他多余的内容，如无政治内容或无法理解辩解，应回复数字0，不要有其他附加内容。" +
                "请注意，群员可能很狡猾，会使用一些拼音、首字母、同音字、简写等来描述一些事物来避免审查。";

            //sysMsg = "You will act as a group administrator to review group messages, and I will inform you in the format {[Who][What]}. " +
            //    "You only need to reply with a number from 0 to 10 to indicate the severity of political content in their speech, for example, \"0\". " +
            //    "There is no need to reply with any additional content. If there is no political content or you cannot understand the defense, reply with the number 0. " +
            //    "Do not include any additional content. Please note that group members may be cunning and may use pinyin, initials, homophones, abbreviations, etc., " +
            //    "to describe things in order to avoid scrutiny."
            var msg6 = await client.SendMessage("{[小绿][有高铁的时候我已经在穿拖鞋上班了]}", sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
            //msg6 = await client.SendMessage("{[Greenie]" +
            //    "[When there is a high-speed train, " +
            //    "I'm already going to work in slippers]}", sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);

            Console.WriteLine($"Data:{msg6.Response}  {msg6.ConversationId}, {msg6.MessageId}");

        }

    }
}
