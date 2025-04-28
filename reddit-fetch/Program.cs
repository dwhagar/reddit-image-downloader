using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace reddit_fetch
{
    public static class Program
    {
        /// <summary>
        /// Main entry point for the application.
        /// Determines whether to run in CLI mode or GUI mode based on provided arguments.
        /// </summary>
        [STAThread]
        public static async Task Main(string[] args)
        {
            try
            {
                // Load the configuration information.
                string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
                var config = new AppConfig();
                CliHandler.Config = config;
                config.ConfigFilePath = configPath;
                config.Load();

                if (args.Length > 0)
                {
                    // CLI Mode
                    await CliHandler.HandleAsync(args);
                }
                else
                {
                    // GUI Mode
                    var app = new App();
                    app.InitializeComponent();
                    app.Run(new MainWindow());
                }
            }
            catch (Exception ex)
            {
                // Catch any unhandled exception at the top level
                Console.Error.WriteLine($"Fatal error: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
