using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using ChatGPTSharp.Utils;

namespace ChatGPTSharp.Model
{
    public sealed class ResponseRequest
    {
        public string? Model { get; set; }
        public JsonNode? Input { get; set; }
        public string? Instructions { get; set; }
        public string? ConversationId { get; set; }
        public string? PreviousResponseId { get; set; }
        public bool? Store { get; set; }
        public IReadOnlyList<ResponseTool>? Tools { get; set; }
        public ResponseToolChoice? ToolChoice { get; set; }
        public bool? ParallelToolCalls { get; set; }
        public IDictionary<string, object?>? ExtraBody { get; set; }
    }

    public sealed class ResponseTool
    {
        public string Type { get; set; } = "function";
        public string? Name { get; set; }
        public string? Description { get; set; }
        public JsonObject? Parameters { get; set; }
        public JsonObject? Config { get; set; }

        public static ResponseTool Function(string name, string? description = null, JsonObject? parameters = null)
        {
            return new ResponseTool
            {
                Type = "function",
                Name = name,
                Description = description,
                Parameters = parameters
            };
        }

        public static ResponseTool BuiltIn(string type, JsonObject? config = null)
        {
            return new ResponseTool
            {
                Type = type,
                Config = config
            };
        }

        public JsonObject ToJsonObject()
        {
            var obj = new JsonObject
            {
                ["type"] = Type
            };

            if (Type == "function")
            {
                obj["name"] = Name ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(Description))
                {
                    obj["description"] = Description;
                }
                if (Parameters != null)
                {
                    obj["parameters"] = Parameters;
                }
            }
            else if (Config != null)
            {
                JsonNodeUtils.Merge(obj, Config);
            }

            return obj;
        }
    }

    public sealed class ResponseToolChoice
    {
        public string? Mode { get; set; }
        public string? Type { get; set; }
        public string? FunctionName { get; set; }

        public static ResponseToolChoice Auto { get; } = new ResponseToolChoice { Mode = "auto" };
        public static ResponseToolChoice None { get; } = new ResponseToolChoice { Mode = "none" };
        public static ResponseToolChoice Required { get; } = new ResponseToolChoice { Mode = "required" };

        public static ResponseToolChoice ForFunction(string functionName)
        {
            return new ResponseToolChoice { Type = "function", FunctionName = functionName };
        }

        public static ResponseToolChoice ForToolType(string type)
        {
            return new ResponseToolChoice { Type = type };
        }

        public JsonNode? ToJsonNode()
        {
            if (!string.IsNullOrWhiteSpace(Type))
            {
                var obj = new JsonObject
                {
                    ["type"] = Type
                };
                if (Type == "function" && !string.IsNullOrWhiteSpace(FunctionName))
                {
                    obj["name"] = FunctionName;
                }
                return obj;
            }

            return !string.IsNullOrWhiteSpace(Mode)
                ? JsonValue.Create(Mode)
                : null;
        }
    }

    public sealed class ResponseResult
    {
        public string? Id { get; set; }
        public string? Model { get; set; }
        public string? ConversationId { get; set; }
        public JsonArray Output { get; set; } = new JsonArray();
        public JsonObject Raw { get; set; } = new JsonObject();

        public string? GetOutputText()
        {
            return ExtractOutputText(Output);
        }

        public static string? ExtractOutputText(JsonArray? output)
        {
            if (output == null || output.Count == 0)
            {
                return null;
            }

            var parts = new List<string>();
            foreach (var item in output.OfType<JsonObject>())
            {
                var type = item["type"]?.GetValue<string>();
                if (type == "message")
                {
                    if (item["content"] is JsonArray contentArray)
                    {
                        foreach (var content in contentArray.OfType<JsonObject>())
                        {
                            var contentType = content["type"]?.GetValue<string>();
                            if (contentType == "output_text" || contentType == "text")
                            {
                                var text = content["text"]?.GetValue<string>();
                                if (!string.IsNullOrEmpty(text))
                                {
                                    parts.Add(text!);
                                }
                            }
                        }
                    }
                }
                else if (type == "output_text")
                {
                    var text = item["text"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(text))
                    {
                        parts.Add(text!);
                    }
                }
            }

            return parts.Count == 0 ? null : string.Concat(parts);
        }
    }

    public sealed class ResponseStreamEvent
    {
        public string? Type { get; set; }
        public string? DeltaText { get; set; }
        public JsonObject Raw { get; set; } = new JsonObject();
        public bool IsDone { get; set; }
    }

    public sealed class ConversationInfo
    {
        public string? Id { get; set; }
        public JsonObject Raw { get; set; } = new JsonObject();
    }
}
