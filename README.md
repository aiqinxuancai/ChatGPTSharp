# ChatGPTSharp

This project implements ChatGPT continuous dialogue based on ConversationId, which can be quickly integrated with just a few lines of code. It supports models such as **gpt-4**, **gpt-3.5-turbo**, and **text-davinci-003**.

[中文](README_CN.md)

## Getting Started

ChatGPTSharp is available as [NuGet package](https://www.nuget.org/packages/ChatGPTSharp/).

Use ConversationId for continuous conversations.
```csharp
var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo");

var msgFirst = await client.SendMessage("Hello, My name is Sin");
Console.WriteLine($"{msgLast.Response}");

var msgLast = await client.SendMessage("My name is?", msgFirst.ConversationId, msgFirst.MessageId);
Console.WriteLine($"{msgLast.Response}");
```

Use prompt to constrain ChatGPT behavior.
```csharp
var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo");
var sysMsg = "You will review group messages as a group administrator, and I will inform you in the format of {[who][said what]} to reply with a number from 0 to 10 to indicate the severity of political content in their speech, such as "0". No need to reply with any other unnecessary content, such as no political content or inability to understand the defense. Please note that group members may be cunning and use pinyin, initials, homophones, abbreviations, etc., to describe things to avoid scrutiny.";

var msg = await client.SendMessage("{[MrWang][Can Trump be president again?]}", sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
```

## Update

### 1.1.2 20230429
* Support for the GPT-4 model and correction of the maximum token count for 8k and 32k.

### 1.1.0 20230320
* The initialization method of ChatGPTClient adds a request timeout setting and changes the default timeout time from 20 seconds to 60 seconds.

### 1.0.9 20230307
* Using TiktokenSharp to calculate token count, fixing the issue of inaccurate token calculation.

<details> <summary>Changelog for earlier versions.</summary>
    
### 1.0.8 20230304
* token algorithm fix

### 1.0.6 20230303
* The token algorithm has been temporarily removed, which may cause exceptions when certain strings are combined. It will be restored after subsequent testing is completed.

### 1.0.5 20230303
* Add SendMessage parameters sendSystemType and sendSystemMessage to specify the insertion of system messages into the conversation.

### 1.0.3 20230302
* Add local token algorithm of gpt3, the algorithm is from js library gpt-3-encoder

</details>

This code base references [node-chatgpt-api](https://github.com/waylaidwanderer/node-chatgpt-api)
