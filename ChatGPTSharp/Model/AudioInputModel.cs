using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatGPTSharp.Model
{
    /// <summary>
    /// Represents an audio input for the chat.
    /// </summary>
    public class AudioInputModel
    {
        /// <summary>
        /// The URL of the audio file. This can be a publicly accessible URL or a base64 encoded data URI.
        /// For example: "data:audio/mpeg;base64,{base64_encoded_audio}"
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioInputModel"/> class.
        /// </summary>
        /// <param name="url">The URL of the audio file.</param>
        public AudioInputModel(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("Audio URL cannot be null or whitespace.", nameof(url));
            }
            Url = url;
        }
    }
}
