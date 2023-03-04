# ChatGPTSharp

This project supports the real ChatGPT model "gpt-3.5-turbo", and the previous generation model "text-davinci-003", both with continuous dialog capability.


[中文](README_CN.md)

## Update

### 1.0.7 20230304
token algorithm after testing does not seem to find any problems, the code recovery

### 1.0.6 20230303
* The token calculation has been temporarily removed, which may cause exceptions when certain strings are combined. It will be restored after subsequent testing is completed.

### 1.0.5 20230303
* Add SendMessage parameters sendSystemType and sendSystemMessage to specify the insertion of system messages into the conversation.

### 1.0.3 20230302
* Add local token algorithm of gpt3, the algorithm is from js library gpt-3-encoder

## Start

```csharp
var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo");
var msg = await client.SendMessage("Hello");
Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");
var msg2 = await client.SendMessage("Who are you", msg.ConversationId, msg.MessageId);
Console.WriteLine($"{msg2.Response}  {msg2.ConversationId}, {msg2.MessageId}");
```


This code base references [node-chatgpt-api](https://github.com/waylaidwanderer/node-chatgpt-api)