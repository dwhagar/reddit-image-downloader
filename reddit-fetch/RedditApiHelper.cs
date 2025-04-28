using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace reddit_fetch
{
    /// <summary>
    /// Provides methods for authenticating with Reddit and fetching subreddit data.
    /// </summary>
    public static class RedditApiHelper
    {
        /// <summary>
        /// The application's configuration settings, must be set externally before use.
        /// </summary>
        public static AppConfig Config { get; set; }

        private static string _accessToken;
        private static DateTime _tokenExpiration;

        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Authenticates with Reddit and retrieves an access token.
        /// Automatically reuses a valid token if it has not expired.
        /// </summary>
        private static async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiration > DateTime.UtcNow)
            {
                return _accessToken;
            }

            var authValue = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{Config.ClientId}:{Config.ClientSecret}")
            );

            HttpClient.DefaultRequestHeaders.Clear();
            HttpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);

            var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await HttpClient.PostAsync("https://www.reddit.com/api/v1/access_token", body);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var tokenResult = JsonSerializer.Deserialize<TokenResponse>(content);

            _accessToken = tokenResult.access_token;
            _tokenExpiration = DateTime.UtcNow.AddSeconds(tokenResult.expires_in - 60); // Refresh early

            Logger.LogInfo("Obtained new Reddit access token.");

            return _accessToken;
        }

        /// <summary>
        /// Fetches the newest posts from a given subreddit.
        /// </summary>
        public static async Task<string> FetchNewPostsAsync(string subreddit)
        {
            var token = await GetAccessTokenAsync();

            HttpClient.DefaultRequestHeaders.Clear();
            HttpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Config.UserAgent);

            var response = await HttpClient.GetAsync($"https://oauth.reddit.com/r/{subreddit}/new?limit=10");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return content; // You will later parse this into post objects
        }
        
        /// <summary>
        /// Maps known MIME types to allowed file extensions.
        /// Returns null if unsupported.
        /// </summary>
        private static string GetFileExtensionFromMimeType(string mimeType)
        {
            return mimeType?.ToLower() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                _ => null // If it's anything else (gif, webp, etc), reject
            };
        }

        public static async Task<bool> DownloadImageAsync(string imageUrl, string baseFileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imageUrl) || string.IsNullOrWhiteSpace(baseFileName))
                {
                    Logger.LogError("DownloadImageAsync called with empty URL or filename.");
                    return false;
                }

                Logger.LogVerbose($"Starting download of image: {imageUrl}");

                var response = await HttpClient.GetAsync(imageUrl);

                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogError($"Failed to download image. Status code: {response.StatusCode}");
                    return false;
                }

                // Determine file extension based on MIME type
                string extension = GetFileExtensionFromMimeType(response.Content.Headers.ContentType?.MediaType);

                if (string.IsNullOrEmpty(extension))
                {
                    Logger.LogInfo($"Unsupported image type '{response.Content.Headers.ContentType?.MediaType}', skipping.");
                    return false;
                }

                // Sanitize base filename
                string safeBaseFileName = PathHelper.MakeSafeFilename(baseFileName);

                // Ensure download directory exists
                Directory.CreateDirectory(Config.DownloadPath);

                // Full save path
                string savePath = Path.Combine(Config.DownloadPath, $"{safeBaseFileName}{extension}");

                // Save the file
                var bytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(savePath, bytes);
                CliHandler.DownloadedFiles.Add(savePath);

                Logger.LogInfo($"Image downloaded and saved to '{savePath}'.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error downloading image: {ex.Message}");
                return false;
            }
        }

        private class TokenResponse
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string token_type { get; set; }
            public string scope { get; set; }
        }
    }
}
