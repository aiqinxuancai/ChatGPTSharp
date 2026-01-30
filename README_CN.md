# ChatGPTSharp

面向现代 C# 的 Chat 客户端，支持工具调用、流式输出、图像/音频输入，并保留 ConversationId 连续对话能力。支持扩展请求体（ExtraBody），不再进行本地 token 计算或限制。

## 功能特性

- ConversationId 连续对话
- 图像与音频统一用内容数组传递
- Tool 定义 + 工具调用返回
- `IAsyncEnumerable` 流式输出
- `ExtraBody` 自由扩展请求字段
- 不做本地 token 计算与限制

## 安装

```bash
 dotnet add package ChatGPTSharp
```

## 快速开始（无状态对话）

```csharp
using ChatGPTSharp;
using ChatGPTSharp.Model;

var settings = new ChatGPTClientSettings
{
    OpenAIKey = File.ReadAllText("KEY.txt"),
    ModelName = "gpt-4o-mini"
};

var client = new ChatGPTClient(settings);

var result = await client.SendMessage(new List<MessageContent>
{
    MessageContent.FromText("你好！")
});

Console.WriteLine(result.Response);
```

## 连续对话

使用 `SendMessageWithConversation` 记录历史并返回 `ConversationId` + `MessageId`。

```csharp
var first = await client.SendMessageWithConversation(new List<MessageContent>
{
    MessageContent.FromText("你好！")
});

var second = await client.SendMessageWithConversation(
    new List<MessageContent> { MessageContent.FromText("继续聊下去") },
    conversationId: first.ConversationId ?? "",
    parentMessageId: first.MessageId ?? "");

Console.WriteLine(second.Response);
```

## 简化写法（Session）

使用 `CreateSession` 自动维护会话状态，无需手动传递 `ConversationId` 和 `MessageId`。
同时支持更简洁的内容调用，不必手动构造 `List<MessageContent>`。

```csharp
var session = client.CreateSession(systemPrompt: "你是一个非常简洁的助手。");

var first = await session.SendAsync("你好！");
var second = await session.SendAsync("继续聊下去。");

var multimodal = await session.SendAsync(
    "描述这张图片。",
    MessageContent.FromImageUrl("https://example.com/demo.png"));

Console.WriteLine(second.Response);
```

## System Prompt

```csharp
var result = await client.SendMessage(
    new List<MessageContent> { MessageContent.FromText("用一句话总结这段文字。") },
    systemPrompt: "你是一个非常简洁的助手。"
);
```

## 图像输入

```csharp
var contents = new List<MessageContent>
{
    MessageContent.FromText("描述这些图片"),
    MessageContent.FromImageFile(@"C:\Images\demo.jpg", ImageDetailMode.Low),
    MessageContent.FromImageUrl("https://example.com/demo.png", ImageDetailMode.Auto)
};

var result = await client.SendMessage(contents);
```

## 音频输入

```csharp
var contents = new List<MessageContent>
{
    MessageContent.FromText("请转写这段音频"),
    MessageContent.FromAudioUrl("https://example.com/sample.mp3"),
    MessageContent.FromAudioFile(@"C:\Audio\sample.mp3", "audio/mpeg")
};

var result = await client.SendMessage(contents);
```

## 流式输出

```csharp
await foreach (var evt in client.SendMessageStream(new List<MessageContent>
{
    MessageContent.FromText("讲一个故事")
}))
{
    if (evt.IsDone)
    {
        Console.WriteLine("\n[done]");
        break;
    }

    if (!string.IsNullOrEmpty(evt.DeltaText))
    {
        Console.Write(evt.DeltaText);
    }
}
```

## 工具调用（Tool / Function Calling）

定义工具 schema，收到 tool_call 后执行，再把结果发回模型。

```csharp
using System.Text.Json.Nodes;

var weatherSchema = JsonNode.Parse(@"{
  \"type\": \"object\",
  \"properties\": {
    \"city\": { \"type\": \"string\" }
  },
  \"required\": [\"city\"]
}") as JsonObject;

var tools = new List<ToolDefinition>
{
    ToolDefinition.CreateFunction(new ToolFunctionDefinition(
        "get_weather",
        "获取当前天气",
        weatherSchema))
};

var toolResult = await client.SendMessage(
    new List<MessageContent> { MessageContent.FromText("巴黎天气怎么样？") },
    tools: tools);

if (toolResult.ToolCalls?.Count > 0)
{
    var call = toolResult.ToolCalls[0];
    var args = JsonNode.Parse(call.Function.Arguments) as JsonObject;
    var city = args?["city"]?.GetValue<string>();

    // 实际工具执行逻辑
    var weather = new JsonObject
    {
        ["city"] = city,
        ["tempC"] = 18
    };

    // 手动构造消息继续对话
    var messages = new List<ChatMessage>
    {
        new ChatMessage
        {
            Role = RoleType.User,
            Contents = new List<MessageContent> { MessageContent.FromText("巴黎天气怎么样？") }
        },
        new ChatMessage { Role = RoleType.Assistant, ToolCalls = toolResult.ToolCalls },
        new ChatMessage
        {
            Role = RoleType.Tool,
            ToolCallId = call.Id,
            Contents = new List<MessageContent> { MessageContent.FromText(weather.ToString()) }
        }
    };

    var followup = await client.SendAsync(new ChatRequest
    {
        Messages = messages,
        Tools = tools
    });

    Console.WriteLine(followup.Message?.GetTextContent());
}
```

## Responses API（服务端会话状态 + 内置工具）

Responses API 支持通过 `conversation` 或 `previous_response_id` 使用服务端会话状态，并支持内置工具。

```csharp
using System.Text.Json.Nodes;

var response = await client.CreateResponseAsync(new ResponseRequest
{
    Instructions = "你很简洁。",
    Input = MessageContent.BuildResponseInput(RoleType.User, new List<MessageContent>
    {
        MessageContent.FromText("用一句话总结这段内容。")
    }),
    Store = true,
    Tools = new List<ResponseTool>
    {
        ResponseTool.BuiltIn("web_search")
    }
});

Console.WriteLine(response.GetOutputText());
```

创建服务端会话并持续追加消息：

```csharp
var convo = await client.CreateConversationAsync();

var items = MessageContent.BuildResponseInput(RoleType.User, new List<MessageContent>
{
    MessageContent.FromText("记住偏好：我喜欢简短回答。")
});

await client.AddConversationItemsAsync(convo.Id ?? "", items);

var followup = await client.CreateResponseAsync(new ResponseRequest
{
    ConversationId = convo.Id,
    Input = MessageContent.BuildResponseInput(RoleType.User, new List<MessageContent>
    {
        MessageContent.FromText("你应该记住什么？")
    })
});

Console.WriteLine(followup.GetOutputText());
```

## ExtraBody（扩展请求体）

```csharp
var extra = new Dictionary<string, object?>
{
    ["response_format"] = new { type = "json_object" }
};

var result = await client.SendMessage(
    new List<MessageContent> { MessageContent.FromText("返回 JSON，包含字段 a 和 b") },
    extraBody: extra);
```

也可以设置全局默认：

```csharp
settings.ExtraBody = new Dictionary<string, object?>
{
    ["response_format"] = new { type = "json_object" }
};
```

## 高级用法：手动请求

通过 `SendAsync` / `StreamAsync` 自己维护消息列表。

```csharp
var request = new ChatRequest
{
    Model = "gpt-4o-mini",
    Messages = new List<ChatMessage>
    {
        new ChatMessage
        {
            Role = RoleType.System,
            Contents = new List<MessageContent> { MessageContent.FromText("你很简洁。") }
        },
        new ChatMessage
        {
            Role = RoleType.User,
            Contents = new List<MessageContent> { MessageContent.FromText("解释一下异步流。") }
        }
    }
};

var response = await client.SendAsync(request);
Console.WriteLine(response.Message?.GetTextContent());
```

## 配置

```csharp
var settings = new ChatGPTClientSettings
{
    OpenAIKey = File.ReadAllText("KEY.txt"),
    ModelName = "gpt-4o-mini",
    BaseUrl = "https://api.openai.com/",
    ProxyUri = "http://127.0.0.1:1080",
    TimeoutSeconds = 60
};
```

说明：
- `BaseUrl` 与 `ProxyUri` 可用于路由或代理。`BaseUrl` 即使包含 `/v1` 也能正确拼接。

This code base references node-chatgpt-api.
