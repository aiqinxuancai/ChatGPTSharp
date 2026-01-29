// See https://aka.ms/new-console-template for more information
using ChatGPTSharp;
using ChatGPTSharp.Model;
using ChatGPTSharp.Sample;

Console.WriteLine("Hello, World!");

ChatGPTClientSettings settings = new ChatGPTClientSettings();
settings.OpenAIKey = File.ReadAllText("am.txt");
settings.ModelName = "aihub-Phi-4-multimodal-instruct";
settings.BaseUrl = "https://aihubmix.com/";




var client = new ChatGPTClient(settings);


client.IsDebug = true;

var contents = new List<MessageContent>
{
    MessageContent.FromText("请将音频转换为文本。"),
    MessageContent.FromAudioUrl("https://wolfapps.oss-cn-shanghai.aliyuncs.com/17667_0_1747799830348.mp3")
};

var systemPrompt = "";

var msg = await client.SendMessageWithConversation(contents, systemPrompt: systemPrompt);
Console.WriteLine($"[AI Response]: {msg.Response} (ConversationId: {msg.ConversationId}, MessageId: {msg.MessageId})");

var followUp = await client.SendMessageWithConversation(
    new List<MessageContent> { MessageContent.FromText("继续总结上面的内容。") },
    conversationId: msg.ConversationId ?? "",
    parentMessageId: msg.MessageId ?? "",
    systemPrompt: systemPrompt);

Console.WriteLine($"[AI Response (Conversation)]: {followUp.Response} (ConversationId: {followUp.ConversationId}, MessageId: {followUp.MessageId})");





