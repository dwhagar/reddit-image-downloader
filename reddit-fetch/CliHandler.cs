using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;

namespace reddit_fetch
{
    public static class CliHandler
    {
        public static AppConfig Config { get; set; }

        /// <summary>
        /// List of files successfully downloaded in this session.
        /// </summary>
        public static List<string> DownloadedFiles { get; } = new List<string>();

        public static async Task<int> HandleAsync(string[] args)
        {
            var rootCommand = new RootCommand("Reddit Image Downloader CLI");

            var verbosityOption = new Option<int>(
                name: "--verbose",
                description: "Set verbosity level (0 = Errors, 1 = Info, 2 = Verbose)",
                getDefaultValue: () => 1
            );

            rootCommand.AddGlobalOption(verbosityOption);

            rootCommand.AddMiddleware(async (context, next) =>
            {
                // Set Logger.LogLevel early before any handler runs
                var verbosity = context.ParseResult.GetValueForOption(verbosityOption);
                Logger.LogLevel = verbosity;

                await next(context); // Continue to handler
            });

            // Download Command
            var downloadCommand = new Command("download", "Download images from configured subreddits");
            downloadCommand.SetHandler(() => DownloadHandler());

            // Add Command
            var addCommand = new Command("add", "Add a subreddit to watch")
            {
                new Argument<string>("subreddit", "Name of the subreddit to add")
            };
            addCommand.SetHandler((string subreddit) => AddHandler(subreddit), new Argument<string>("subreddit"));

            // Remove Command
            var removeCommand = new Command("remove", "Remove a subreddit from the watch list")
            {
                new Argument<string>("subreddit", "Name of the subreddit to remove")
            };
            removeCommand.SetHandler((string subreddit) => RemoveHandler(subreddit), new Argument<string>("subreddit"));

            // List Command
            var listCommand = new Command("list", "List all watched subreddits");
            listCommand.SetHandler(() => ListHandler());

            rootCommand.AddCommand(downloadCommand);
            rootCommand.AddCommand(addCommand);
            rootCommand.AddCommand(removeCommand);
            rootCommand.AddCommand(listCommand);

            return await rootCommand.InvokeAsync(args);
        }

        public static async Task DownloadHandler()
        {
            Logger.LogInfo("Starting download operation...");

            if (Config?.Subreddits == null || Config.Subreddits.Count == 0)
            {
                Logger.LogError("No subreddits listed.");
                return;
            }

            foreach (var subreddit in Config.Subreddits)
            {
                // Skip if subreddit was checked recently
                var nextAllowedCheck = subreddit.LastCheckDate.AddMinutes(Config.MinutesBetweenChecks);
                if (nextAllowedCheck > DateTime.UtcNow)
                {
                    Logger.LogInfo($"Skipping subreddit '{subreddit.Name}' (checked recently at {subreddit.LastCheckDate:u}). Next check allowed at {nextAllowedCheck:u}.");
                    continue;
                }

                Logger.LogVerbose($"Checking subreddit: {subreddit.Name}...");

                string newPosts;
                try
                {
                    newPosts = await RedditApiHelper.FetchNewPostsAsync(subreddit.Name);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to fetch posts for subreddit '{subreddit.Name}': {ex.Message}");
                    subreddit.LastCheckDate = DateTime.UtcNow;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(newPosts))
                {
                    Logger.LogInfo($"No data returned for subreddit '{subreddit.Name}'. Skipping.");
                    subreddit.LastCheckDate = DateTime.UtcNow;
                    continue;
                }

                RedditListing redditListing;
                try
                {
                    redditListing = JsonSerializer.Deserialize<RedditListing>(newPosts);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error parsing JSON for subreddit '{subreddit.Name}': {ex.Message}");
                    subreddit.LastCheckDate = DateTime.UtcNow;
                    continue;
                }

                if (redditListing?.Data?.Children == null || redditListing.Data.Children.Count == 0)
                {
                    Logger.LogInfo($"No posts found in subreddit '{subreddit.Name}'.");
                    subreddit.LastCheckDate = DateTime.UtcNow;
                    continue;
                }

                DateTime mostRecentPostDate = subreddit.LastPostDate; // Start with the existing last post date

                foreach (var child in redditListing.Data.Children)
                {
                    var post = child.Data;

                    // Convert Reddit's created_utc (seconds since epoch) to DateTime
                    DateTime postDateUtc = DateTimeOffset.FromUnixTimeSeconds((long)post.CreatedUtc).UtcDateTime;

                    // Skip old posts we've already seen
                    if (postDateUtc <= subreddit.LastPostDate)
                    {
                        Logger.LogVerbose($"Skipping old post '{post.Title}' (posted at {postDateUtc:u}).");
                        continue;
                    }

                    if (post.PostHint == "image" && !string.IsNullOrEmpty(post.Url))
                    {
                        string safeTitle = PathHelper.MakeSafeFilename(post.Title);
                        string baseFileName = $"{safeTitle}_{post.Id}";

                        bool success = await RedditApiHelper.DownloadImageAsync(post.Url, baseFileName);

                        if (success)
                        {
                            Logger.LogInfo($"Successfully downloaded post '{post.Title}'.");

                            if (postDateUtc > mostRecentPostDate)
                            {
                                mostRecentPostDate = postDateUtc;
                            }
                        }
                    }
                }

                // After processing all posts:
                subreddit.LastPostDate = mostRecentPostDate;
                subreddit.LastCheckDate = DateTime.UtcNow;
            }

            // After all subreddits processed, save the updated config
            Config.Save();
            Logger.LogInfo("Download operation completed.");
        }

        public static void AddHandler(string subredditName)
        {
            if (string.IsNullOrWhiteSpace(subredditName))
            {
                Logger.LogError("Subreddit name cannot be empty.");
                return;
            }

            // Check if subreddit already exists (case-insensitive)
            if (Config.Subreddits.Any(s => s.Name.Equals(subredditName, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.LogInfo($"Subreddit '{subredditName}' is already configured.");
                return;
            }

            // Create a temporary SubredditInfo object
            var newSubreddit = new SubredditInfo
            {
                Name = subredditName,
                LastCheckDate = new DateTime(1970, 1, 1),
                LastPostDate = new DateTime(1970, 1, 1)
            };

            // Validate subreddit name using its own method
            if (!newSubreddit.IsValid())
            {
                Logger.LogError($"Invalid subreddit name '{subredditName}'. Subreddit names must be 3–21 characters and only contain letters, numbers, or underscores.");
                return;
            }

            // Add to list and save
            Config.Subreddits.Add(newSubreddit);
            Config.Save();
            Logger.LogInfo($"Subreddit '{subredditName}' successfully added.");
        }

        public static void RemoveHandler(string subredditName)
        {
            if (string.IsNullOrWhiteSpace(subredditName))
            {
                Logger.LogError("Subreddit name cannot be empty.");
                return;
            }

            // Find the subreddit (case-insensitive)
            var subreddit = Config.Subreddits
                .FirstOrDefault(s => s.Name.Equals(subredditName, StringComparison.OrdinalIgnoreCase));

            if (subreddit == null)
            {
                Logger.LogInfo($"Subreddit '{subredditName}' was not found.");
                return;
            }

            // Remove the subreddit
            Config.Subreddits.Remove(subreddit);
            Config.Save();
            Logger.LogInfo($"Subreddit '{subredditName}' has been removed.");
        }

        private static void ListHandler()
        {
            Logger.LogInfo("Listing all tracked subreddits...");

            if (Config?.Subreddits == null || Config.Subreddits.Count == 0)
            {
                Console.WriteLine("(List Empty)");
                return;
            }

            foreach (var subreddit in Config.Subreddits)
            {
                Console.WriteLine($"- {subreddit.Name}");
            }
        }

    }
}
