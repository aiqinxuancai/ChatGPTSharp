# ChatGPTSharp

This project implements ChatGPT continuous dialogue based on ConversationId, which can be quickly integrated with just a few lines of code. It supports models such as **gpt-4**, **gpt-4-vision-preview** and **gpt-3.5-turbo**.

[中文](README_CN.md)

## Getting Started

ChatGPTSharp is available as [NuGet package](https://www.nuget.org/packages/ChatGPTSharp/).

Use ConversationId for continuous conversations.
```csharp
ChatGPTClientSettings settings = new ChatGPTClientSettings();
settings.OpenAIToken = File.ReadAllText("KEY.txt");
settings.ModelName = "gpt-4-vision-preview";
settings.ProxyUri = "http://127.0.0.1:1081";

var client = new ChatGPTClient(settings);
client.IsDebug = true;

var ChatImageModels = new List<ChatImageModel>()
{
    ChatImageModel.CreateWithFile(@"C:\Users\aiqin\Pictures\20231221155547.png", ImageDetailMode.Low)
};

var systemPrompt = "";
var msg = await client.SendMessage("Please describe this image", systemPrompt: systemPrompt, images: ChatImageModels);
Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");
msg = await client.SendMessage("Have you eaten today?", msg.ConversationId, msg.MessageId);
Console.WriteLine($"{msg.Response}  {msg.ConversationId}, {msg.MessageId}");
```


## Update

### 2.0.0 20231221
* Added support for sending images using the Vision model and pre-computing image tokens (local files only).
* Improved the token algorithm for messages to align with the official API.
* Added more default token count data for official website models and automatic conversion of '16k' in model names to maximum tokens.
* Considering the increasing number of tokens in models, introduced a method to support unlimited MaxResponseTokens and MaxPromptTokens. Setting them to 0 will remove the limit.

### 1.1.4 20230711
* Add support gpt-3.5-turbo-16k
 
### 1.1.3 20230508
* Removed the old token algorithm code and now supports netstandard2.0, now, the library can also be used with .NET Framework.

### 1.1.2 20230429
* Support for the GPT-4 model and correction of the maximum token count for 8k and 32k.

<details> <summary>Changelog for earlier versions.</summary>

### 1.1.0 20230320
* The initialization method of ChatGPTClient adds a request timeout setting and changes the default timeout time from 20 seconds to 60 seconds.

### 1.0.9 20230307
* Using TiktokenSharp to calculate token count, fixing the issue of inaccurate token calculation.

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
