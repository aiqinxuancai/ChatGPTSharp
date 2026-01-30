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
        /// Practical example
        /// </summary>
        /// <returns></returns>
        internal static async Task Test() 
        {
            var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-5.2");
            client.IsDebug = true;

            //var sysMsg = "You are a group moderator. I will send messages in the format {[who][what was said]}. Reply with a single number from 0 to 10 indicating how severe the political content is.";

            var sysMsg = "You are a Werewolf game judge. I will send messages in the format {[who][what was said]}. Reply with JSON in this format: {[a number from 0 to 10 indicating how much the statement is 'out-of-game identity claiming'],[a number from 0 to 10 indicating how emotionally intense the statement is]}. 'Out-of-game identity claiming' means trying to prove identity using logic outside the game, e.g., 'If I'm not XXX, my whole family should die.'";

            //var msg = await client.SendMessage("{[Alex][Why won't you believe me? I'm the seer. Are you all idiots?]}", sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
            //Console.WriteLine($"Data:{msg.Response}  {msg.ConversationId}, {msg.MessageId}");

            //var msg2 = await client.SendMessage("{[Riley][I think we should vote out player 6 unanimously.]}",conversationId:msg.ConversationId, parentMessageId:msg.MessageId,  sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
            //Console.WriteLine($"Data:{msg2.Response}  {msg2.ConversationId}, {msg2.MessageId}");

            //var msg4 = await client.SendMessage("{[Morgan][I don't have much to say.]}", conversationId: msg2.ConversationId, parentMessageId: msg2.MessageId, sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
            //Console.WriteLine($"Data:{msg4.Response}  {msg4.ConversationId}, {msg4.MessageId}");

            //var msg5 = await client.SendMessage("{[Casey][If I'm not the seer, I'll eat my phone.]}", conversationId: msg4.ConversationId, parentMessageId: msg4.MessageId, sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
            //Console.WriteLine($"Data:{msg5.Response}  {msg5.ConversationId}, {msg5.MessageId}");


            sysMsg = "You are a group moderator reviewing messages. I will send messages in the format {[who][what was said]}. " +
                "Reply with a single number from 0 to 10 indicating how severe the political content is, e.g., \"0\". " +
                "Do not reply with any other content. If there is no political content or it is unclear, reply with 0 only. " +
                "Note: members may be evasive and use acronyms, homophones, or abbreviations to avoid detection.";

            var msg6 = await client.SendMessage("{[Casey][Back when high-speed rail was new, I was already commuting in slippers.]}", systemPrompt: sysMsg);


            Console.WriteLine($"Data:{msg6.Response}  {msg6.ConversationId}, {msg6.MessageId}");

        }

    }
}
