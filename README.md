# ChatGPTSharp

This project supports the real ChatGPT model "gpt-3.5-turbo", and the previous generation model "text-davinci-003", both with continuous dialog capability.


[中文](README_CN.md)

## Update

### 1.1.0 20230320
* The initialization method of ChatGPTClient adds a request timeout setting and changes the default timeout time from 20 seconds to 60 seconds.

### 1.0.9 20230307
* Using TikTokSharp to calculate token count, fixing the issue of inaccurate token calculation.

### 1.0.8 20230304
* token algorithm fix

### 1.0.6 20230303
* The token algorithm has been temporarily removed, which may cause exceptions when certain strings are combined. It will be restored after subsequent testing is completed.

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

### Advanced usage
Use prompt to constrain ChatGPT's behavior.
```csharp
var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo");
var sysMsg = "You will review group messages as a group administrator, and I will inform you in the format of {[who][said what]} to reply with a number from 0 to 10 to indicate the severity of political content in their speech, such as "0". No need to reply with any other unnecessary content, such as no political content or inability to understand the defense. Please note that group members may be cunning and use pinyin, initials, homophones, abbreviations, etc., to describe things to avoid scrutiny.";

var msg = await client.SendMessage("{[MrWang][Can Trump be president again?]}", sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
```

This code base references [node-chatgpt-api](https://github.com/waylaidwanderer/node-chatgpt-api)