using System.Text.Json.Nodes;

namespace ChatGPTSharp.Utils
{
    internal static class JsonNodeUtils
    {
        public static void Merge(JsonObject target, JsonObject source)
        {
            foreach (var entry in source)
            {
                if (entry.Value is JsonObject sourceObj && target[entry.Key] is JsonObject targetObj)
                {
                    Merge(targetObj, sourceObj);
                }
                else
                {
                    target[entry.Key] = entry.Value?.DeepClone();
                }
            }
        }
    }
}
