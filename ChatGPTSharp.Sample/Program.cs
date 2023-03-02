// See https://aka.ms/new-console-template for more information
using ChatGPTSharp;

Console.WriteLine("Hello, World!");

//Test "gpt-3.5-turbo"
var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo");
client.IsDebug = true;
var msg = await client.SendMessage("Hello");
Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");
var msg2 = await client.SendMessage("Who are you", msg.ConversationId, msg.MessageId);
Console.WriteLine($"{msg2.Response}  {msg2.ConversationId}, {msg2.MessageId}");



var clientN = new ChatGPTClient(File.ReadAllText("KEY.txt"));
var msg3 = await clientN.SendMessage("Hello");
Console.WriteLine($"{msg3.Response}  {msg3.ConversationId}, {msg3.MessageId}");
var msg4 = await clientN.SendMessage("Who are you", msg3.ConversationId, msg3.MessageId);
Console.WriteLine($"{msg4.Response}  {msg4.ConversationId}, {msg4.MessageId}");