using System;
using System.Drawing;
using System.IO;



namespace ChatGPTSharp.Model
{


    public enum ImageDetailMode
    {
        Auto,
        Low,
        High,
    }

    public class ChatImageContent
    {
        public string Url { get; set; }


        public static ChatImageContent CreateWithBytes(byte[] imageBytes)
        {
            ChatImageContent chatImageContent = new ChatImageContent();
            string base64ImageRepresentation = Convert.ToBase64String(imageBytes);

            //var (width, height) = GetImageDimensions(imageBytes);
            //Console.WriteLine($"Width: {width}, Height: {height}");
            //获取图片的

            chatImageContent.Url = $"data:image/jpeg;base64,{base64ImageRepresentation}";
            return chatImageContent;
        }

        public static ChatImageContent CreateWithUrl(string url)
        {
            ChatImageContent chatImageContent = new ChatImageContent();
            chatImageContent.Url = url;
            return chatImageContent;
        }

        public static ChatImageContent CreateWithFile(string filePath)
        {
            var image = File.ReadAllBytes(filePath);
            return CreateWithBytes(image);
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


            if (detailMode == ImageDetailMode.Auto)
            {
                if (width > squareSize || height > squareSize) //Be verified
                {
                    detailMode = ImageDetailMode.High;
                }
                else
                {
                    detailMode = ImageDetailMode.Low;
                }

            }


            if (detailMode == ImageDetailMode.Low)
            {
                return lowDetailCost;
            }
            else if (detailMode == ImageDetailMode.High)
            {
                // Scale down the image if either dimension exceeds the maximum allowed.
                if (width > maxDimension || height > maxDimension)
                {
                    double scaleFactor = Math.Min((double)maxDimension / width, (double)maxDimension / height);
                    width = (int)(width * scaleFactor);
                    height = (int)(height * scaleFactor);
                }

                // Further scale down the image such that the shortest side is 768px long.
                double scaleToShortestSideFactor = (double)targetShortSide / Math.Min(width, height);
                width = (int)(width * scaleToShortestSideFactor);
                height = (int)(height * scaleToShortestSideFactor);

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
