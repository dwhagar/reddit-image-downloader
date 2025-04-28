using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace reddit_fetch
{
    public static class ImageFilterHelper
    {
        public static AppConfig Config { get; set; }

        public static bool IsImageAcceptable(string filePath)
        {
            try
            {
                using var image = Image.Load(filePath);

                int width = image.Width;
                int height = image.Height;

                float aspectRatio = (float)width / height;
                float megapixels = (width * height) / 1_000_000f;
                float brightness = CalculateAverageBrightness(image);

                // Check against config
                if (aspectRatio < Config.MinAspectRatio || aspectRatio > config.MaxAspectRatio)
                    return false;

                if (megapixels < Config.MinResolutionMegapixels)
                    return false;

                if (brightness < Config.MinBrightness || brightness > config.MaxBrightness)
                    return false;

                return true; // Passed all checks
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error processing image '{filePath}': {ex.Message}");
                return false; // Treat errors as failed checks
            }
        }

        private static float CalculateAverageBrightness(Image image)
        {
            float totalBrightness = 0;
            int pixelCount = 0;

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);

                    for (int x = 0; x < row.Length; x++)
                    {
                        var pixel = row[x];
                        // Simple brightness estimation: average of R, G, B
                        totalBrightness += (pixel.R + pixel.G + pixel.B) / (255f * 3f);
                        pixelCount++;
                    }
                }
            });

            if (pixelCount == 0)
                return 0.5f; // Safe fallback

            return totalBrightness / pixelCount;
        }
    }
}