using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;

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

                // Basic quality checks
                if (aspectRatio < Config.MinAspectRatio || aspectRatio > Config.MaxAspectRatio)
                {
                    Logger.LogVerbose($"Rejected '{filePath}' due to aspect ratio {aspectRatio:F2}.");
                    return false;
                }

                if (megapixels < Config.MinResolutionMegapixels)
                {
                    Logger.LogVerbose($"Rejected '{filePath}' due to insufficient megapixels ({megapixels:F2} MP).");
                    return false;
                }

                if (brightness < Config.MinBrightness || brightness > Config.MaxBrightness)
                {
                    Logger.LogVerbose($"Rejected '{filePath}' due to brightness {brightness:F2}.");
                    return false;
                }

                // Now check for perceptual hash similarity
                ulong imageHash = ComputeImageHash(image);

                if (HashDatabaseHelper.IsDuplicate(imageHash))
                {
                    Logger.LogVerbose($"Rejected '{filePath}' due to similarity to a known deleted image.");
                    return false;
                }

                // Store the hash for the passed image in the database.
                HashDatabaseHelper.InsertNewImage(filePath, imageHash);

                // Passed all checks
                return true;
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

        /// <summary>
        /// Computes the perceptual hash (pHash) of a given image.
        /// </summary>
        public static ulong ComputeImageHash(Image image)
        {
            var hasher = new PerceptualHash(); // You can also switch to AverageHash or DifferenceHash
            return hasher.Hash(image);
        }        
    }
}