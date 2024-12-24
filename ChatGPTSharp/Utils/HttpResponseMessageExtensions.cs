using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChatGPTSharp.Utils
{
    public static class HttpResponseMessageExtensions
    {
        public static void EnsureSuccessStatusCodeWithContent(this HttpResponseMessage response, string responseContent)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Status: {(int)response.StatusCode} ({response.StatusCode}), Response: {responseContent}");
            }
        }
    }
}
