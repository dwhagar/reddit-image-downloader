using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System;
using System.Runtime.InteropServices;


namespace reddit_fetch
{
    /// <summary>
    /// Provides helper methods for retrieving system-specific paths.
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// The GUID for the user's Downloads folder, defined by Windows Known Folders.
        /// </summary>
        private static readonly Guid DownloadsFolderGuid = new Guid("374DE290-123F-4565-9164-39C4925E467B");

        /// <summary>
        /// Retrieves the path to the user's default Downloads folder using the Windows Known Folders API.
        /// This method will return the correct folder even if the user has moved or renamed their Downloads directory.
        /// </summary>
        /// <returns>The full path to the Downloads folder as a string.</returns>
        /// <exception cref="ExternalException">Thrown if the system call to retrieve the folder path fails.</exception>
        public static string GetDefaultDownloadPath()
        {
            IntPtr outPath;

            int result = SHGetKnownFolderPath(DownloadsFolderGuid, 0, IntPtr.Zero, out outPath);

            if (result != 0)
            {
                throw new ExternalException("Failed to retrieve the Downloads folder path.", result);
            }

            string path = Marshal.PtrToStringUni(outPath);
            Marshal.FreeCoTaskMem(outPath);
            return path;
        }

        /// <summary>
        /// Calls the native Windows Shell API to retrieve the path to a known folder.
        /// </summary>
        /// <param name="rfid">A reference to the known folder's GUID.</param>
        /// <param name="dwFlags">Flags specifying special retrieval options (unused here).</param>
        /// <param name="hToken">An access token (unused here, pass IntPtr.Zero).</param>
        /// <param name="ppszPath">A pointer that receives the folder path string.</param>
        /// <returns>Returns 0 if successful; otherwise, returns an error code.</returns>
        [DllImport("shell32.dll")]
        private static extern int SHGetKnownFolderPath(
            [MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
            uint dwFlags,
            IntPtr hToken,
            out IntPtr ppszPath);
    }

    /// <summary>
    /// Manages loading and saving application configuration settings from a JSON file.
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// The full path to the JSON configuration file.
        /// This should be set externally before calling Load or Save.
        /// </summary>
        public string ConfigFilePath { get; set; }

        // Settings properties
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string DownloadPath { get; set; } = PathHelper.GetDefaultDownloadPath();
        public List<SubredditInfo> Subreddits { get; set; } = new();

        /// <summary>
        /// Loads the configuration from the JSON file at ConfigFilePath.
        /// If the file does not exist, creates a default configuration.
        /// </summary>
        public void Load()
        {
            if (string.IsNullOrWhiteSpace(ConfigFilePath))
            {
                throw new InvalidOperationException("ConfigFilePath must be set before loading configuration.");
            }

            if (!File.Exists(ConfigFilePath))
            {
                Logger.LogInfo($"Config file not found at '{ConfigFilePath}', creating default configuration.");
                Save(); // Save default config
                return;
            }

            try
            {
                Logger.LogInfo($"Loading configuration from '{ConfigFilePath}'...");

                var jsonString = File.ReadAllText(ConfigFilePath);
                var loadedConfig = JsonSerializer.Deserialize<AppConfig>(jsonString);

                if (loadedConfig != null)
                {
                    ClientId = loadedConfig.ClientId;
                    ClientSecret = loadedConfig.ClientSecret;
                    UserAgent = loadedConfig.UserAgent;
                    DownloadPath = loadedConfig.DownloadPath;
                    Subreddits = loadedConfig.Subreddits ?? new List<SubredditInfo>();

                    if (Logger.LogLevel >= 3)
                    {
                        Logger.LogVerbose($"ClientId: {ClientId}");
                        Logger.LogVerbose($"ClientSecret: {ClientSecret}");
                        Logger.LogVerbose($"UserAgent: {UserAgent}");
                        Logger.LogVerbose($"DownloadPath: {DownloadPath}");

                        foreach (var sub in Subreddits)
                        {
                            Logger.LogVerbose($"Subreddit: {sub.Name}, LastChecked: {sub.LastCheckDate}, LastPost: {sub.LastPostDate}");
                        }
                    }
                }
                else
                {
                    Logger.LogError("Failed to deserialize configuration file. Creating new default configuration.");
                    Save();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading configuration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves the current configuration settings to the JSON file at ConfigFilePath.
        /// </summary>
        public void Save()
        {
            if (string.IsNullOrWhiteSpace(ConfigFilePath))
            {
                throw new InvalidOperationException("ConfigFilePath must be set before saving configuration.");
            }

            try
            {
                var jsonString = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, jsonString);
                Logger.LogInfo($"Configuration saved to '{ConfigFilePath}'.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving configuration: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Represents a tracked subreddit entry with tracking metadata.
    /// </summary>
    public class SubredditInfo
    {
        /// <summary>
        /// The name of the subreddit.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The last time this subreddit was checked for new posts.
        /// </summary>
        public DateTime LastCheckDate { get; set; } = new DateTime(1970, 1, 1);

        /// <summary>
        /// The last post date from this subreddit that was downloaded.
        /// </summary>
        public DateTime LastPostDate { get; set; } = new DateTime(1970, 1, 1);

        /// <summary>
        /// Validates whether the current subreddit name is acceptable according to basic Reddit naming rules.
        /// </summary>
        /// <returns>True if the name is valid; otherwise, false.</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return false;

            foreach (char c in Name)
            {
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                {
                    return false;
                }
            }

            if (Name.Length < 3 || Name.Length > 21)
                return false;

            return true;
        }
    }
}
