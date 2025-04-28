using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace reddit_fetch
{
    public static class CliHandler
    {
        public static AppConfig Config { get; set; }

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

        private static void DownloadHandler()
        {
            Logger.LogInfo("Starting download operation...");
            // Download logic here
        }

        private static void AddHandler(string subredditName)
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

        private static void RemoveHandler(string subredditName)
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
