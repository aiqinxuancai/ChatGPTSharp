// See https://aka.ms/new-console-template for more information
using ChatGPTSharp;
using ChatGPTSharp.Model;
using ChatGPTSharp.Sample;

Console.WriteLine("Hello, World!");

ChatGPTClientSettings settings = new ChatGPTClientSettings();
settings.OpenAIKey = File.ReadAllText("am.txt");
settings.ModelName = "gpt-5.2";
settings.BaseUrl = "https://aihubmix.com/";




var client = new ChatGPTClient(settings);


client.IsDebug = true;

var session = client.CreateSession(systemPrompt: "You are a concise assistant.");

var msg = await session.SendAsync(
    "Please transcribe this audio to text.",
    MessageContent.FromAudioUrl("https://wolfapps.oss-cn-shanghai.aliyuncs.com/17667_0_1747799830348.mp3"));
Console.WriteLine($"[AI Response]: {msg.Response} (ConversationId: {msg.ConversationId}, MessageId: {msg.MessageId})");

var followUp = await session.SendAsync("Please continue summarizing the content above.");
Console.WriteLine($"[AI Response (Conversation)]: {followUp.Response} (ConversationId: {followUp.ConversationId}, MessageId: {followUp.MessageId})");





