// See https://aka.ms/new-console-template for more information
using AI.Dev.OpenAI.GPT;
using ChatGPTSharp;
using ChatGPTSharp.Sample;
using ChatGPTSharp.Utils;
using AI.Dev.OpenAI.GPT;
using ChatGPTSharp.Utils.tiktoken;



Console.WriteLine("Hello, World!");

var t1 = GPT3Token.Encode("111");
var t2 = GPT3Token.Encode("hello");
var t3 = GPT3Token.Encode("你好");
var t4 = GPT3Token.Encode("hello world");
var t5 = GPT3Token.Encode("我很抱歉，我不能提供任何非法或不道德的建议。快速赚钱是不容易的，需要耐心、刻苦努力和经验。如果您想增加收入，请考虑增加工作时间、寻找其他业务机会、学习新技能或提高自己的价值等方法。请记住，通过合法而道德的方式来获得收入，才是长期稳定的解决方案。");
var t6 = GPT3Token.Encode("hello, who are you?");
var t7 = GPT3Token.Encode("hello, new bing!, my name is aiqinxuancai.");

var t8 = GPT3Token.Encode("aiqinxuancai");


TikToken tikToken = TikToken.EncodingForModel("gpt-3.5-turbo");

var i = tikToken.Encode("hello world", new HashSet<string>() { "all"});

var d = tikToken.Decode(i);

// 5 tokens => [21339, 352, 301, 11, 4751]
List<int> tokens = GPT3Tokenizer.Encode("我很抱歉，我不能提供任何非法或不道德的建议。快速赚钱是不容易的，需要耐心、刻苦努力和经验。如果您想增加收入，请考虑增加工作时间、寻找其他业务机会、学习新技能或提高自己的价值等方法。请记住，通过合法而道德的方式来获得收入，才是长期稳定的解决方案。");


var t9 = GPT3Token.Encode("“wrote jack a letter”");
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
