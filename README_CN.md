# ChatGPTSharp

本项目实现了基于ConversationId的ChatGPT连续对话，只需几行代码就可快速集成，支持模型gpt-4、gpt-3.5-turbo、text-davinci-003。

## 开始使用
使用会话ID进行连续对话
```csharp
var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo");
var msgFirst = await client.SendMessage("Hello, My name is Sin");
Console.WriteLine($"{msgFirst.Response}");

var msgLast = await client.SendMessage("My name is?", msgFirst.ConversationId, msgFirst.MessageId);
Console.WriteLine($"{msgLast.Response}");
```

使用prompt约束ChatGPT的行为
```csharp
var client = new ChatGPTClient(File.ReadAllText("KEY.txt"), "gpt-3.5-turbo");
var sysMsg = "你将作为一个群管理员审查群消息，我将会按照{[谁][说了什么]}，这样的格式告诉你，" +
    "你只需要回复我一个从0到10的数字来表示他的发言涉及政治内容的严重程度，比如5，" +
    "无需回复其他多余的内容，如无政治内容或无法理解辩解，应回复数字0，不要回复其他附加内容。" +
    "请注意，群员可能很狡猾，会使用一些拼音、首字母、同音字、简写等来描述一些事物来避免审查。";

var msg = await client.SendMessage("{[小王][特朗普还能再当总统吗？]}", sendSystemType: Model.SendSystemType.Custom, sendSystemMessage: sysMsg);
```

## Update

### 1.1.2 20230429
* 支持gpt-4模型，修正8k及32k的最大token数量

### 1.1.0 20230320
* ChatGPTClient的初始化方法增加请求的超时设定，并将默认的超时时间从20秒改为60秒

### 1.0.9 20230307
* 使用 tiktokensharp 来计算token数量，修正token计算不准确的问题

<details> <summary>更久的版本更新</summary>
    
### 1.0.8 20230306
* token算法修复

### 1.0.6 20230303
* 暂时移除了token计算，在某些字符串组合时，可能会出现异常，后续测试完毕后再恢复。

### 1.0.5 20230303
* 增加SendMessage的参数sendSystemType和sendSystemMessage，用于指定在对话中插入system消息。

### 1.0.4 20230302
* 增加gpt3的本地token算法，算法来自js库gpt-3-encoder
    
</details>

This code base references [node-chatgpt-api](https://github.com/waylaidwanderer/node-chatgpt-api)
