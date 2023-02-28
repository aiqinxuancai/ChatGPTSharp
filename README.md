# ChatGPTSharp

**Not calling the real ChatGPT model**, but using openai model "text-davinci-003", a simple implementation of c# that adds contextual capabilities, with capabilities close to ChatGPT.

## Start

```csharp
ChatGPTClient chatGPT = new ChatGPTClient("OPENAI-KEY");
var r = await chatGPT.SendMessage("Hello");
var r2 = await chatGPT.SendMessage("Who are you?", r.ConversationId, r.MessageId);
```


This code base references [node-chatgpt-api](https://github.com/waylaidwanderer/node-chatgpt-api)