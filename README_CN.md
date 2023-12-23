# ChatGPTSharp

本项目实现了基于ConversationId的ChatGPT连续对话，只需几行代码就可快速集成，支持模型gpt-4、gpt-3.5-turbo、text-davinci-003。

## 开始使用

[NuGet package](https://www.nuget.org/packages/ChatGPTSharp/)

使用会话ID进行连续对话
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
* 支持Vision model的图片发送，并预先计算图片token（仅本地文件）
* 改进消息的token算法，使其与官方接口一致
* 增加更多官网模型的默认token数量数据，以及自动转换模型名称中的16k字样为最大token
* 鉴于模型的token数量越来越大，支持不限制的MaxResponseTokens、MaxPromptTokens的方法，将其设置为0既可

### 1.1.4 20230711
* 支持gpt-3.5-turbo-16k

### 1.1.3 20230508
* 移除旧的token算法代码，支持netstandard2.0，现在.NET Framework也可以使用此库

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
