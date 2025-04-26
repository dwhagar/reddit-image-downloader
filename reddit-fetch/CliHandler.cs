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

        private static void AddHandler(string subreddit)
        {
            Logger.LogInfo($"Adding subreddit: {subreddit}");
            // Add logic here
        }

        private static void RemoveHandler(string subreddit)
        {
            Logger.LogInfo($"Removing subreddit: {subreddit}");
            // Remove logic here
        }

        private static void ListHandler()
        {
            Logger.LogInfo("Listing all tracked subreddits...");
            // List logic here
        }
    }
}
