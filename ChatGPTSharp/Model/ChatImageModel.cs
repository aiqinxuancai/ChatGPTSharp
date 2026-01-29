using System;
using System.IO;



namespace ChatGPTSharp.Model
{


    public enum ImageDetailMode
    {
        None,
        Auto,
        Low,
        High,
    }

    public class ChatImageModel
    {
        public string Url { get; set; } = string.Empty;

        public ImageDetailMode Mode { get; set; }
        

        public static ChatImageModel CreateWithBytes(byte[] imageBytes, ImageDetailMode imageDetailMode = ImageDetailMode.None)
        {
            ChatImageModel chatImageContent = new ChatImageModel();
            string base64ImageRepresentation = Convert.ToBase64String(imageBytes);

            chatImageContent.Mode = imageDetailMode;
            chatImageContent.Url = $"data:image/jpeg;base64,{base64ImageRepresentation}";
            return chatImageContent;
        }

        public static ChatImageModel CreateWithUrl(string url, ImageDetailMode imageDetailMode = ImageDetailMode.None)
        {
            ChatImageModel chatImageContent = new ChatImageModel();
            chatImageContent.Url = url;
            chatImageContent.Mode = imageDetailMode;
            return chatImageContent;
        }

        public static ChatImageModel CreateWithFile(string filePath, ImageDetailMode imageDetailMode = ImageDetailMode.None)
        {
            var image = File.ReadAllBytes(filePath);
            return CreateWithBytes(image, imageDetailMode);
        }


        /// <summary>
        /// Converts a file to a base64 string with a data URI prefix.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <param name="mimeType">The MIME type of the file (e.g., "image/jpeg", "audio/mpeg").</param>
        /// <returns>A base64 encoded string with data URI.</returns>
        public static string ConvertFileToBase64(string filePath, string mimeType)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
            }
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found.", filePath);
            }
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                throw new ArgumentException("MIME type cannot be null or whitespace.", nameof(mimeType));
            }

            byte[] fileBytes = File.ReadAllBytes(filePath);
            string base64Representation = Convert.ToBase64String(fileBytes);
            return $"data:{mimeType};base64,{base64Representation}";
        }

    }
}
