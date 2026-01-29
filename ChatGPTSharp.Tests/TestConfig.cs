using System;
using System.Collections.Generic;
using System.IO;
using ChatGPTSharp;

namespace ChatGPTSharp.Tests
{
    internal static class TestConfig
    {
        public static bool TryCreateClient(out ChatGPTClient? client)
        {
            var key = GetOpenAIKey();
            if (string.IsNullOrWhiteSpace(key))
            {
                Console.WriteLine("Missing OpenAI key. Set OPENAI_KEY or OPENAI_API_KEY, or place KEY.txt/am.txt in the repo root.");
                client = null;
                return false;
            }

            var settings = new ChatGPTClientSettings
            {
                OpenAIKey = key,
                ModelName = GetEnv("OPENAI_MODEL") ?? ChatGPTClient.DefaultModel,
                BaseUrl = GetEnv("OPENAI_BASE_URL") ?? "https://api.openai.com/",
                Temperature = 0,
                TimeoutSeconds = 120
            };

            client = new ChatGPTClient(settings);
            return true;
        }

        public static ChatGPTClient CreateClient()
        {
            if (!TryCreateClient(out var client) || client == null)
            {
                throw new InvalidOperationException(
                    "Missing OpenAI key. Set OPENAI_KEY or OPENAI_API_KEY, or place KEY.txt/am.txt in the repo root.");
            }

            return client;
        }

        public static string GetOpenAIKey()
        {
            var envKey = GetEnv("OPENAI_KEY") ?? GetEnv("OPENAI_API_KEY");
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                return envKey;
            }

            var repoRoot = FindRepoRoot();
            if (repoRoot == null)
            {
                return string.Empty;
            }

            foreach (var fileName in new[] { "KEY.txt", "key.txt", "am.txt" })
            {
                var path = Path.Combine(repoRoot, fileName);
                if (File.Exists(path))
                {
                    return File.ReadAllText(path).Trim();
                }
            }

            return string.Empty;
        }

        private static string? GetEnv(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "ChatGPTSharp.sln")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            return null;
        }
    }
}
