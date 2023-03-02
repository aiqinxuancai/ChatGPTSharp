# ChatGPTSharp

This project supports the real ChatGPT model "gpt-3.5-turbo", and the previous generation model "text-davinci-003", both with continuous dialog capability.

## Start

```csharp
var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo");
var msg = await client.SendMessage("Hello");
Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");
var msg2 = await client.SendMessage("Who are you", msg.ConversationId, msg.MessageId);
Console.WriteLine($"{msg2.Response}  {msg2.ConversationId}, {msg2.MessageId}");
```


This code base references [node-chatgpt-api](https://github.com/waylaidwanderer/node-chatgpt-api)