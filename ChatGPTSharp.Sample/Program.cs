// See https://aka.ms/new-console-template for more information
using ChatGPTSharp;
using ChatGPTSharp.Model;
using ChatGPTSharp.Sample;
using TiktokenSharp;

Console.WriteLine("Hello, World!");

ChatGPTClientSettings settings = new ChatGPTClientSettings();
settings.OpenAIToken = File.ReadAllText("KEY.txt");
settings.ModelName = "gpt-4-vision-preview";
settings.ProxyUri = "http://127.0.0.1:1081";
var client = new ChatGPTClient(settings);
client.IsDebug = true;


var prompt = "";


var ChatImageModels = new List<ChatImageModel>() { ChatImageModel.CreateWithFile(@"C:\Users\aiqin\Pictures\20231221155547.png") } ;

var msg = await client.SendMessage(null, systemPrompt: prompt, images: ChatImageModels);
Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");


//msg = await client.SendMessage("Have you eaten today?", msg.ConversationId, msg.MessageId, systemPrompt: prompt);
//Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");
//msg = await client.SendMessage("Really?", msg.ConversationId, msg.MessageId, systemPrompt: prompt);
//Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");
//msg = await client.SendMessage("Really?", msg.ConversationId, msg.MessageId, systemPrompt: prompt);
//Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");


