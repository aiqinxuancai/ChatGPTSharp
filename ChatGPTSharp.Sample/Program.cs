// See https://aka.ms/new-console-template for more information
using ChatGPTSharp;
using ChatGPTSharp.Sample;
using TiktokenSharp;

Console.WriteLine("Hello, World!");


var t = TikToken.EncodingForModel("gpt-4-vision-preview");

var t1 = t.Encode("Hello");
var t2 = t.Encode("Hello! How can I assist you today?");
var t3 = t.Encode("Have you eaten today?");
var t4 = t.Encode("As an artificial intelligence, I don't eat or drink. I'm here to help you. How can I assist you today?");


ChatGPTClientSettings settings = new ChatGPTClientSettings();
settings.OpenAIToken = File.ReadAllText("KEY.txt");
settings.ModelName = "gpt-4-vision-preview";
settings.ProxyUri = "http://127.0.0.1:1081";

//settings.APIURL = "";
var client = new ChatGPTClient(settings);
client.IsDebug = true;


var prompt = "";

var msg = await client.SendMessage("Hello", systemPrompt: prompt);
Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");
//msg = await client.SendMessage("Have you eaten today?", msg.ConversationId, msg.MessageId, systemPrompt: prompt);
//Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");
//msg = await client.SendMessage("Really?", msg.ConversationId, msg.MessageId, systemPrompt: prompt);
//Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");
//msg = await client.SendMessage("Really?", msg.ConversationId, msg.MessageId, systemPrompt: prompt);
//Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");


