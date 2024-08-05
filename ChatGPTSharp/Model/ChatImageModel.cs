using System;
using System.Drawing;
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

    public class ChatImageModel: ChatModel
    {
        public string Url { get; set; }

        /// <summary>
        /// Only local file
        /// </summary>
        public int TokensCount { get; set; }

        public ImageDetailMode Mode { get; set; }
        

        public static ChatImageModel CreateWithBytes(byte[] imageBytes, ImageDetailMode imageDetailMode = ImageDetailMode.None)
        {
            ChatImageModel chatImageContent = new ChatImageModel();
            string base64ImageRepresentation = Convert.ToBase64String(imageBytes);

            var (width, height) = GetImageDimensions(imageBytes);
            chatImageContent.TokensCount = CalculateImageTokens(width, height, imageDetailMode);

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


        public static (int width, int height) GetImageDimensions(byte[] imageBytes)
        {
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                using (Image image = Image.FromStream(ms))
                {
                    return (image.Width, image.Height);
                }
            }
        }

        public static int CalculateImageTokens(int width, int height, ImageDetailMode detailMode)
        {
            const int lowDetailCost = 85;
            const int highDetailCostPerSquare = 170;
            const int highDetailBaseCost = 85;
            const int maxDimension = 2048;
            const int targetShortSide = 768;
            const int squareSize = 512;


            if (detailMode == ImageDetailMode.None)
            {
                detailMode = ImageDetailMode.High; //
            }

            if (detailMode == ImageDetailMode.Low)
            {
                return lowDetailCost;
            }
            else if (detailMode == ImageDetailMode.High)
            {
                bool scaledToMax = false;

                // Scale down the image if either dimension exceeds the maximum allowed.
                if (width > maxDimension || height > maxDimension)
                {
                    double scaleFactor = Math.Min((double)maxDimension / width, (double)maxDimension / height);
                    width = (int)(width * scaleFactor);
                    height = (int)(height * scaleFactor);
                    scaledToMax = true;
                }

                // Further scale down the image only if it has been scaled in the previous step.
                if (scaledToMax)
                {
                    double scaleToShortestSideFactor = (double)targetShortSide / Math.Min(width, height);
                    width = (int)(width * scaleToShortestSideFactor);
                    height = (int)(height * scaleToShortestSideFactor);
                }

                // Calculate how many 512px squares are needed to cover the image.
                int squaresAcross = (int)Math.Ceiling((double)width / squareSize);
                int squaresDown = (int)Math.Ceiling((double)height / squareSize);
                int totalSquares = squaresAcross * squaresDown;

                // Calculate final token cost for high detail images.
                return highDetailCostPerSquare * totalSquares + highDetailBaseCost;
            }

            return lowDetailCost;
        }


    }
}
