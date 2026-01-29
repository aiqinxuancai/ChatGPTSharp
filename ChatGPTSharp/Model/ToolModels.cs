using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace ChatGPTSharp.Model
{
    public sealed class ToolFunctionDefinition
    {
        public ToolFunctionDefinition(string name, string? description = null, JsonObject? parameters = null)
        {
            Name = name;
            Description = description;
            Parameters = parameters;
        }

        public string Name { get; }
        public string? Description { get; }
        public JsonObject? Parameters { get; }

        public JsonObject ToJsonObject()
        {
            var obj = new JsonObject
            {
                ["name"] = Name
            };

            if (!string.IsNullOrWhiteSpace(Description))
            {
                obj["description"] = Description;
            }

            if (Parameters != null)
            {
                obj["parameters"] = Parameters;
            }

            return obj;
        }
    }

    public sealed class ToolDefinition
    {
        public ToolDefinition(string type, ToolFunctionDefinition function)
        {
            Type = type;
            Function = function;
        }

        public string Type { get; }
        public ToolFunctionDefinition Function { get; }

        public static ToolDefinition CreateFunction(ToolFunctionDefinition function)
        {
            return new ToolDefinition("function", function);
        }

        public JsonObject ToJsonObject()
        {
            return new JsonObject
            {
                ["type"] = Type,
                ["function"] = Function.ToJsonObject()
            };
        }
    }

    public sealed class ToolFunctionCall
    {
        public ToolFunctionCall(string name, string arguments)
        {
            Name = name;
            Arguments = arguments;
        }

        public string Name { get; }
        public string Arguments { get; }

        public JsonObject ToJsonObject()
        {
            return new JsonObject
            {
                ["name"] = Name,
                ["arguments"] = Arguments
            };
        }
    }

    public sealed class ToolCall
    {
        public ToolCall(string id, string type, ToolFunctionCall function)
        {
            Id = id;
            Type = type;
            Function = function;
        }

        public string Id { get; }
        public string Type { get; }
        public ToolFunctionCall Function { get; }

        public JsonObject ToJsonObject()
        {
            return new JsonObject
            {
                ["id"] = Id,
                ["type"] = Type,
                ["function"] = Function.ToJsonObject()
            };
        }

        public static ToolCall FromJsonObject(JsonObject obj)
        {
            var id = obj["id"]?.GetValue<string>() ?? string.Empty;
            var type = obj["type"]?.GetValue<string>() ?? "function";
            var function = obj["function"] as JsonObject;
            var name = function?["name"]?.GetValue<string>() ?? string.Empty;
            var arguments = function?["arguments"]?.GetValue<string>() ?? string.Empty;
            return new ToolCall(id, type, new ToolFunctionCall(name, arguments));
        }
    }

    public sealed class ToolChoice
    {
        public string? Mode { get; set; }
        public string? FunctionName { get; set; }

        public static ToolChoice Auto { get; } = new ToolChoice { Mode = "auto" };
        public static ToolChoice None { get; } = new ToolChoice { Mode = "none" };
        public static ToolChoice Required { get; } = new ToolChoice { Mode = "required" };

        public static ToolChoice ForFunction(string functionName)
        {
            return new ToolChoice { FunctionName = functionName };
        }

        public JsonNode? ToJsonNode()
        {
            if (!string.IsNullOrWhiteSpace(FunctionName))
            {
                return new JsonObject
                {
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = FunctionName
                    }
                };
            }

            return !string.IsNullOrWhiteSpace(Mode)
                ? JsonValue.Create(Mode)
                : null;
        }
    }

    public class AudioOutputModel
    {
        public string? Id { get; set; }
        public string? Data { get; set; }
        public string? Transcript { get; set; }
        public string? Format { get; set; }
        public JsonObject? Raw { get; set; }

        public static AudioOutputModel FromJsonObject(JsonObject audio)
        {
            return new AudioOutputModel
            {
                Id = audio["id"]?.GetValue<string>(),
                Data = audio["data"]?.GetValue<string>(),
                Transcript = audio["transcript"]?.GetValue<string>(),
                Format = audio["format"]?.GetValue<string>(),
                Raw = audio
            };
        }
    }
}
