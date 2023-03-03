# ChatGPTSharp

这个项目支持真正的ChatGPT模型 "gpt-3.5-turbo"，以及上一代模型 "text-davinci-003"，两者都具有连续对话能力。

后续可能会移除旧model的支持

## Update
### 1.0.6 20230303
* 暂时移除了token计算，在某些字符串组合时，可能会出现异常，后续测试完毕后再恢复。
### 1.0.5 20230303
* 增加SendMessage的参数sendSystemType和sendSystemMessage，用于指定在对话中插入system消息。

### 1.0.4 20230302
* 增加gpt3的本地token算法，算法来自js库gpt-3-encoder

## Start

```csharp
var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo");
var msg = await client.SendMessage("Hello");
Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");
var msg2 = await client.SendMessage("Who are you", msg.ConversationId, msg.MessageId);
Console.WriteLine($"{msg2.Response}  {msg2.ConversationId}, {msg2.MessageId}");
```


This code base references [node-chatgpt-api](https://github.com/waylaidwanderer/node-chatgpt-api)