# ChatGPTSharp

Modern C# client for chat, tools, streaming, and multimodal messages (image/audio). It supports ConversationId-based continuity, flexible request extensions, and manual control when you need it.

## Features

- ConversationId-based continuity
- Image and audio inputs via a unified content array
- Tool definitions + tool-call responses
- Streaming responses via `IAsyncEnumerable`
- Extra request fields via `ExtraBody`
- No local token counting or token limits

## Installation

```bash
 dotnet add package ChatGPTSharp
```

## Quick Start (Stateless Chat)

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
    MessageContent.FromText("Hello!")
});

Console.WriteLine(result.Response);
```

## Continuous Conversation

Use `SendMessageWithConversation` to record history and return `ConversationId` + `MessageId`.

```csharp
var first = await client.SendMessageWithConversation(new List<MessageContent>
{
    MessageContent.FromText("Hello!")
});

var second = await client.SendMessageWithConversation(
    new List<MessageContent> { MessageContent.FromText("Continue the conversation") },
    conversationId: first.ConversationId ?? "",
    parentMessageId: first.MessageId ?? "");

Console.WriteLine(second.Response);
```

## System Prompt

```csharp
var result = await client.SendMessage(
    new List<MessageContent> { MessageContent.FromText("Summarize this in one sentence.") },
    systemPrompt: "You are a concise assistant.");
```

## Image Input

```csharp
var contents = new List<MessageContent>
{
    MessageContent.FromText("Describe these images"),
    MessageContent.FromImageFile(@"C:\Images\demo.jpg", ImageDetailMode.Low),
    MessageContent.FromImageUrl("https://example.com/demo.png", ImageDetailMode.Auto)
};

var result = await client.SendMessage(contents);
```

## Audio Input

```csharp
var contents = new List<MessageContent>
{
    MessageContent.FromText("Transcribe this audio"),
    MessageContent.FromAudioUrl("https://example.com/sample.mp3"),
    MessageContent.FromAudioFile(@"C:\Audio\sample.mp3", "audio/mpeg")
};

var result = await client.SendMessage(contents);
```

## Streaming

```csharp
await foreach (var evt in client.SendMessageStream(new List<MessageContent>
{
    MessageContent.FromText("Tell me a story")
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

## Tools (Function Calling)

Define a tool schema, send a request, then execute the tool and return its result.

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
        "Get current weather",
        weatherSchema))
};

var toolResult = await client.SendMessage(
    new List<MessageContent> { MessageContent.FromText("What's the weather in Paris?") },
    tools: tools);

if (toolResult.ToolCalls?.Count > 0)
{
    var call = toolResult.ToolCalls[0];
    var args = JsonNode.Parse(call.Function.Arguments) as JsonObject;
    var city = args?["city"]?.GetValue<string>();

    // Your tool execution
    var weather = new JsonObject
    {
        ["city"] = city,
        ["tempC"] = 18
    };

    // Continue with manual message list
    var messages = new List<ChatMessage>
    {
        new ChatMessage
        {
            Role = RoleType.User,
            Contents = new List<MessageContent> { MessageContent.FromText("What's the weather in Paris?") }
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

## Responses API (Server-Side State + Built-in Tools)

The Responses API supports server-side state via `conversation` or `previous_response_id`, and built-in tools.

```csharp
using System.Text.Json.Nodes;

var response = await client.CreateResponseAsync(new ResponseRequest
{
    Instructions = "You are concise.",
    Input = MessageContent.BuildResponseInput(RoleType.User, new List<MessageContent>
    {
        MessageContent.FromText("Summarize this in one sentence.")
    }),
    Store = true,
    Tools = new List<ResponseTool>
    {
        ResponseTool.BuiltIn("web_search")
    }
});

Console.WriteLine(response.GetOutputText());
```

Create a conversation on the server and keep adding items:

```csharp
var convo = await client.CreateConversationAsync();

var items = MessageContent.BuildResponseInput(RoleType.User, new List<MessageContent>
{
    MessageContent.FromText("Remember this preference: I like short answers.")
});

await client.AddConversationItemsAsync(convo.Id ?? "", items);

var followup = await client.CreateResponseAsync(new ResponseRequest
{
    ConversationId = convo.Id,
    Input = MessageContent.BuildResponseInput(RoleType.User, new List<MessageContent>
    {
        MessageContent.FromText("What should you remember?")
    })
});

Console.WriteLine(followup.GetOutputText());
```

## ExtraBody (Request Extensions)

```csharp
var extra = new Dictionary<string, object?>
{
    ["response_format"] = new { type = "json_object" }
};

var result = await client.SendMessage(
    new List<MessageContent> { MessageContent.FromText("Return JSON with fields a and b") },
    extraBody: extra);
```

You can also set defaults:

```csharp
settings.ExtraBody = new Dictionary<string, object?>
{
    ["response_format"] = new { type = "json_object" }
};
```

## Advanced: Manual Requests

Use `SendAsync` and `StreamAsync` with a `ChatRequest` when you want full control over messages.

```csharp
var request = new ChatRequest
{
    Model = "gpt-4o-mini",
    Messages = new List<ChatMessage>
    {
        new ChatMessage
        {
            Role = RoleType.System,
            Contents = new List<MessageContent> { MessageContent.FromText("You are concise.") }
        },
        new ChatMessage
        {
            Role = RoleType.User,
            Contents = new List<MessageContent> { MessageContent.FromText("Explain async streams.") }
        }
    }
};

var response = await client.SendAsync(request);
Console.WriteLine(response.Message?.GetTextContent());
```

## Configuration

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

Notes:
- `BaseUrl` and `ProxyUri` can be used to route requests. `BaseUrl` may include `/v1` and will still resolve correctly.

This code base references node-chatgpt-api.
