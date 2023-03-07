// See https://aka.ms/new-console-template for more information
using ChatGPTSharp;
using ChatGPTSharp.Sample;
using ChatGPTSharp.Utils;
using TiktokenSharp;

Console.WriteLine("Hello, World!");

var t1 = GPT3Token.Encode("111");
var t2 = GPT3Token.Encode("hello");
var t3 = GPT3Token.Encode("你好");
var t4 = GPT3Token.Encode("hello world");
var t5 = GPT3Token.Encode("1");
var t6 = GPT3Token.Encode("hello, who are you?");
var t7 = GPT3Token.Encode("hello, new bing!, my name is aiqinxuancai.");

var t8 = GPT3Token.Encode("aiqinxuancai");

var t9 = GPT3Token.Encode("“wrote jack a letter”");

var tiktoken = TikToken.EncodingForModel("gpt-3.5-turbo");

var t10 = tiktoken.Encode("hello world").Count;

//Test "gpt-3.5-turbo"
//var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo");
//client.IsDebug = true;
//var msg = await client.SendMessage("你好！");
//Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");
//var msg2 = await client.SendMessage("你是谁？", msg.ConversationId, msg.MessageId);
//Console.WriteLine($"{msg2.Response}  {msg2.ConversationId}, {msg2.MessageId}");



//var clientN = new ChatGPTClient(File.ReadAllText("KEY.txt"));
//var msg3 = await clientN.SendMessage("Hello");
//Console.WriteLine($"{msg3.Response}  {msg3.ConversationId}, {msg3.MessageId}");
//var msg4 = await clientN.SendMessage("Who are you", msg3.ConversationId, msg3.MessageId);
//Console.WriteLine($"{msg4.Response}  {msg4.ConversationId}, {msg4.MessageId}");


//GPT3Token.getToken();


await GroupAdminTest.Test();
