using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace reddit_fetch
{
    /// <summary>
    /// Provides logging functionality with support for different output targets and log levels.
    /// </summary>
    public static class Logger
    {
        private static string _logFilePath = "app.log";
        private static int _logLevel = 1; // Default to Information (1)

        /// <summary>
        /// Gets or sets the full file path for log file output.
        /// Validates writability when set. Disables file logging if validation fails.
        /// </summary>
        public static string LogFilePath
        {
            get => _logFilePath;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Log file path cannot be empty.");

                try
                {
                    var directory = Path.GetDirectoryName(value);

                    if (string.IsNullOrWhiteSpace(directory))
                        throw new ArgumentException("Log file path must include a valid directory.");

                    if (!Directory.Exists(directory))
                        throw new DirectoryNotFoundException($"Directory does not exist: {directory}");

                    using (var stream = new FileStream(value, FileMode.Append, FileAccess.Write))
                    {
                        // Test writability
                    }

                    _logFilePath = value;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Logger fallback: Cannot write to log file '{value}'. Reason: {ex.Message}");
                    _logFilePath = null;
                    UseFile = false;
                    UseConsole = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the logging level.
        /// 0 = Errors only, 1 = Errors and Information, 3 = Verbose logging.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an invalid log level is set.</exception>
        public static int LogLevel
        {
            get => _logLevel;
            set
            {
                if (value != 0 && value != 1 && value != 3)
                    throw new ArgumentOutOfRangeException(nameof(value), "LogLevel must be 0 (Error), 1 (Info), or 3 (Verbose).");

                _logLevel = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to output logs to a GUI control.
        /// </summary>
        public static bool UseGui { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to output logs to a file.
        /// </summary>
        public static bool UseFile { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to output logs to the console.
        /// </summary>
        public static bool UseConsole { get; set; } = true;

        /// <summary>
        /// Gets or sets the action that handles GUI logging output.
        /// Should be assigned a method that appends text to a GUI control like a TextBox.
        /// </summary>
        public static Action<string> GuiLogAction { get; set; }

        /// <summary>
        /// Logs a message if its level meets or exceeds the current LogLevel.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The level of the message (0=Error, 1=Info, 3=Verbose).</param>
        public static void Log(string message, int level)
        {
            if (level > _logLevel)
            {
                return; // Suppress messages that are too detailed
            }

            string prefix = level switch
            {
                0 => "[ERROR] ",
                1 => "[INFO] ",
                3 => "[VERBOSE] ",
                _ => ""
            };

            string timestamped = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {prefix}{message}";

            if (UseConsole)
            {
                Console.WriteLine(timestamped);
            }

            if (UseGui && GuiLogAction != null)
            {
                GuiLogAction.Invoke(timestamped);
            }

            if (UseFile && !string.IsNullOrWhiteSpace(_logFilePath))
            {
                try
                {
                    File.AppendAllText(_logFilePath, timestamped + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Logger error writing to file: {ex.Message}");
                    UseFile = false;
                }
            }
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The informational message to log.</param>
        public static void LogInfo(string message)
        {
            Log(message, 1);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public static void LogError(string message)
        {
            Log(message, 0);
        }

        /// <summary>
        /// Logs a verbose diagnostic message.
        /// </summary>
        /// <param name="message">The verbose message to log.</param>
        public static void LogVerbose(string message)
        {
            Log(message, 3);
        }
    }
}
