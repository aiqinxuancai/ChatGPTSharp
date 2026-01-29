using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace ChatGPTSharp.Model
{
    public enum MessageContentType
    {
        Text,
        ImageUrl,
        AudioUrl
    }

    public sealed class MessageContent
    {
        public MessageContentType Type { get; set; }
        public string? Text { get; set; }
        public string? Url { get; set; }
        public ImageDetailMode Detail { get; set; } = ImageDetailMode.None;

        public static MessageContent FromText(string text)
        {
            return new MessageContent { Type = MessageContentType.Text, Text = text };
        }

        public static MessageContent FromImageUrl(string url, ImageDetailMode detail = ImageDetailMode.None)
        {
            return new MessageContent { Type = MessageContentType.ImageUrl, Url = url, Detail = detail };
        }

        public static MessageContent FromImageFile(string filePath, ImageDetailMode detail = ImageDetailMode.None)
        {
            var mimeType = GuessImageMimeType(filePath);
            var dataUrl = ChatImageModel.ConvertFileToBase64(filePath, mimeType);
            return FromImageUrl(dataUrl, detail);
        }

        public static MessageContent FromAudioUrl(string url)
        {
            return new MessageContent { Type = MessageContentType.AudioUrl, Url = url };
        }

        public static MessageContent FromAudioFile(string filePath, string mimeType)
        {
            var dataUrl = ChatImageModel.ConvertFileToBase64(filePath, mimeType);
            return FromAudioUrl(dataUrl);
        }

        public JsonObject ToJsonObject()
        {
            switch (Type)
            {
                case MessageContentType.Text:
                    return new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = Text ?? string.Empty
                    };
                case MessageContentType.ImageUrl:
                    var url = new JsonObject
                    {
                        ["url"] = Url ?? string.Empty
                    };
                    if (Detail == ImageDetailMode.Auto)
                    {
                        url["detail"] = "auto";
                    }
                    else if (Detail == ImageDetailMode.Low)
                    {
                        url["detail"] = "low";
                    }
                    else if (Detail == ImageDetailMode.High)
                    {
                        url["detail"] = "high";
                    }

                    return new JsonObject
                    {
                        ["type"] = "image_url",
                        ["image_url"] = url
                    };
                case MessageContentType.AudioUrl:
                    return new JsonObject
                    {
                        ["type"] = "audio",
                        ["audio_url"] = Url ?? string.Empty
                    };
                default:
                    return new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = Text ?? string.Empty
                    };
            }
        }

        public JsonObject ToResponseContentItem()
        {
            switch (Type)
            {
                case MessageContentType.Text:
                    return new JsonObject
                    {
                        ["type"] = "input_text",
                        ["text"] = Text ?? string.Empty
                    };
                case MessageContentType.ImageUrl:
                    var image = new JsonObject
                    {
                        ["type"] = "input_image"
                    };

                    if (Detail == ImageDetailMode.None)
                    {
                        image["image_url"] = Url ?? string.Empty;
                    }
                    else
                    {
                        var imageUrl = new JsonObject
                        {
                            ["url"] = Url ?? string.Empty
                        };

                        if (Detail == ImageDetailMode.Auto)
                        {
                            imageUrl["detail"] = "auto";
                        }
                        else if (Detail == ImageDetailMode.Low)
                        {
                            imageUrl["detail"] = "low";
                        }
                        else if (Detail == ImageDetailMode.High)
                        {
                            imageUrl["detail"] = "high";
                        }

                        image["image_url"] = imageUrl;
                    }

                    return image;
                case MessageContentType.AudioUrl:
                    if (TryParseAudioDataUrl(Url, out var data, out var format))
                    {
                    return new JsonObject
                    {
                        ["type"] = "input_audio",
                        ["input_audio"] = new JsonObject
                        {
                            ["data"] = data,
                            ["format"] = format
                        }
                    };
                }

                    return new JsonObject
                    {
                        ["type"] = "input_file",
                        ["file_url"] = Url ?? string.Empty
                    };
                default:
                    return new JsonObject
                    {
                        ["type"] = "input_text",
                        ["text"] = Text ?? string.Empty
                    };
            }
        }

        public static JsonArray BuildResponseInput(RoleType role, List<MessageContent> contents)
        {
            var contentArray = new JsonArray();
            foreach (var content in contents ?? new List<MessageContent>())
            {
                contentArray.Add(content.ToResponseContentItem());
            }

            return new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "message",
                    ["role"] = RoleToString(role),
                    ["content"] = contentArray
                }
            };
        }

        public static List<MessageContent> FromJsonNode(JsonNode? contentToken)
        {
            var list = new List<MessageContent>();
            if (contentToken == null)
            {
                return list;
            }

            if (contentToken is JsonValue)
            {
                var text = contentToken.GetValue<string?>();
                if (!string.IsNullOrEmpty(text))
                {
                    list.Add(FromText(text!));
                }
                return list;
            }

            if (contentToken is JsonArray contentArray)
            {
                foreach (var token in contentArray)
                {
                    if (token is not JsonObject obj)
                    {
                        continue;
                    }

                    var type = obj["type"]?.GetValue<string>();
                    if (type == "text")
                    {
                        var text = obj["text"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(text))
                        {
                            list.Add(FromText(text!));
                        }
                        continue;
                    }

                    if (type == "image_url")
                    {
                        var imageUrl = obj["image_url"] as JsonObject;
                        var url = imageUrl?["url"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(url))
                        {
                            var detail = imageUrl?["detail"]?.GetValue<string>() switch
                            {
                                "low" => ImageDetailMode.Low,
                                "high" => ImageDetailMode.High,
                                "auto" => ImageDetailMode.Auto,
                                _ => ImageDetailMode.None
                            };
                            list.Add(FromImageUrl(url!, detail));
                        }
                        continue;
                    }

                    if (type == "audio" || type == "audio_url" || type == "input_audio")
                    {
                        var url = obj["audio_url"]?.GetValue<string>() ?? obj["url"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(url))
                        {
                            list.Add(FromAudioUrl(url!));
                        }
                    }
                }
            }

            return list;
        }

        public static string? ExtractText(IEnumerable<MessageContent>? contents)
        {
            if (contents == null)
            {
                return null;
            }

            var parts = new List<string>();
            foreach (var content in contents)
            {
                if (content.Type == MessageContentType.Text && !string.IsNullOrEmpty(content.Text))
                {
                    parts.Add(content.Text!);
                }
            }

            return parts.Count == 0 ? null : string.Concat(parts);
        }

        private static string GuessImageMimeType(string filePath)
        {
            var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
            return ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "image/jpeg"
            };
        }

        private static bool TryParseAudioDataUrl(string? url, out string data, out string format)
        {
            data = string.Empty;
            format = "mp3";

            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            var value = url!;
            if (!value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var marker = ";base64,";
            var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index <= 5)
            {
                return false;
            }

            var mime = value.Substring(5, index - 5);
            data = value.Substring(index + marker.Length);
            if (string.IsNullOrEmpty(data))
            {
                return false;
            }

            format = GuessAudioFormat(mime);
            return true;
        }

        private static string GuessAudioFormat(string? mime)
        {
            if (string.IsNullOrWhiteSpace(mime))
            {
                return "mp3";
            }

            var normalized = mime!.ToLowerInvariant();
            if (normalized.Contains("wav"))
            {
                return "wav";
            }
            if (normalized.Contains("ogg"))
            {
                return "ogg";
            }
            if (normalized.Contains("webm"))
            {
                return "webm";
            }
            if (normalized.Contains("mpeg") || normalized.Contains("mp3"))
            {
                return "mp3";
            }

            return "mp3";
        }

        private static string RoleToString(RoleType role)
        {
            return role switch
            {
                RoleType.User => "user",
                RoleType.Assistant => "assistant",
                RoleType.System => "system",
                RoleType.Tool => "tool",
                _ => "user",
            };
        }
    }
}
