// See https://aka.ms/new-console-template for more information
using ChatGPTSharp;
using ChatGPTSharp.Sample;
using TiktokenSharp;

Console.WriteLine("Hello, World!");



//await GetMovieTitle.Test();

var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-4", timeoutSeconds: 1000);
client.IsDebug = true;

var msgString = "我接下来说一个数值，你把这个数值加上9527后再返回给我";
//var msgString = "I will now say a number, and you should add 9527 to this number and then return it to me";
var msg = await client.SendMessage(msgString);
Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");
var msg2 = await client.SendMessage("5", msg.ConversationId, msg.MessageId);
Console.WriteLine($"{msg2.Response}  {msg2.ConversationId}, {msg2.MessageId}");



//var clientN = new ChatGPTClient(File.ReadAllText("KEY.txt"));
//var msg3 = await clientN.SendMessage("Hello");
//Console.WriteLine($"{msg3.Response}  {msg3.ConversationId}, {msg3.MessageId}");
//var msg4 = await clientN.SendMessage("Who are you", msg3.ConversationId, msg3.MessageId);
//Console.WriteLine($"{msg4.Response}  {msg4.ConversationId}, {msg4.MessageId}");


//GPT3Token.getToken();

//int tokenCount = 0;
//double usdor = 1 / 0.0018 * 1000;
//int count = (int)usdor / 12873;
//while (true)
//{
//    await Translator.Test();
//    tokenCount += 12873;

//    if (tokenCount > usdor)
//    {
//        break;
//    }
//    Console.WriteLine($"使用token：{tokenCount} 次数：{tokenCount / 12873}");
//}


