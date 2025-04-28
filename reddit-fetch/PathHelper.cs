using System;
using System.IO;
using System.Linq;
using System.Text;

namespace reddit_fetch
{
    public static class PathHelper
    {
        /// <summary>
        /// Sanitizes a string to create a safe filename across Windows, Linux, and macOS.
        /// Removes or replaces characters that are illegal in filenames.
        /// </summary>
        /// <param name="title">The original title or filename base.</param>
        /// <returns>A safe filename string.</returns>
        public static string MakeSafeFilename(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return "untitled";
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new StringBuilder(title.Length);

            foreach (var c in title)
            {
                if (invalidChars.Contains(c) || char.IsControl(c))
                {
                    sanitized.Append('_'); // Replace invalid characters with underscore
                }
                else
                {
                    sanitized.Append(c);
                }
            }

            // Optionally trim length to avoid extremely long filenames
            const int MaxFileNameLength = 100;
            string finalName = sanitized.ToString();
            if (finalName.Length > MaxFileNameLength)
            {
                finalName = finalName.Substring(0, MaxFileNameLength);
            }

            return finalName;
        }
    }
}
