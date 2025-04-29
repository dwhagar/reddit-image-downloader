using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace reddit_fetch
{
    /// <summary>
    /// Helper class for managing the image hash database.
    /// Handles storage, lookup, and maintenance of image hashes.
    /// </summary>
    public static class HashDatabaseHelper
    {
        public static AppConfig Config { get; set; }

        /// <summary>
        /// Ensures the database and table exist.
        /// </summary>
        public static void EnsureDatabaseExists()
        {
            if (!File.Exists(Config.DatabasePath))
            {
                Logger.LogInfo("Creating new hash database...");
            }

            using var connection = new SqliteConnection($"Data Source={Config.DatabasePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ImageHashes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FileName TEXT NOT NULL,
                    HashValue INTEGER NOT NULL,
                    FileExists BOOLEAN NOT NULL
                );
            ";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Inserts a new image hash record into the database.
        /// </summary>
        public static void InsertNewImage(string filename, ulong hash)
        {
            EnsureDatabaseExists();

            using var connection = new SqliteConnection($"Data Source={Config.DatabasePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ImageHashes (FileName, HashValue, FileExists)
                VALUES (@FileName, @HashValue, 1);
            ";
            command.Parameters.AddWithValue("@FileName", filename);
            command.Parameters.AddWithValue("@HashValue", (long)hash);
            command.ExecuteNonQuery();

            Logger.LogVerbose($"Inserted new image hash for '{filename}'.");
        }

        /// <summary>
        /// Checks if a new image hash is similar to any existing hash where the file no longer exists.
        /// </summary>
        public static bool IsDuplicate(ulong newHash, float maxSimilarity = 90.0f)
        {
            EnsureDatabaseExists();

            using var connection = new SqliteConnection($"Data Source={Config.DatabasePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT HashValue FROM ImageHashes
                WHERE FileExists = 0;
            ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                ulong existingHash = (ulong)(long)reader.GetInt64(0);

                float similarity = ComputeSimilarityPercent(newHash, existingHash);

                if (similarity > maxSimilarity)
                {
                    Logger.LogVerbose($"Found similar hash ({similarity}% similar).");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the FileExists flag for a given filename.
        /// </summary>
        public static void UpdateFileExists(string filename, bool exists)
        {
            EnsureDatabaseExists();

            using var connection = new SqliteConnection($"Data Source={Config.DatabasePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ImageHashes
                SET FileExists = @FileExists
                WHERE FileName = @FileName;
            ";
            command.Parameters.AddWithValue("@FileExists", exists ? 1 : 0);
            command.Parameters.AddWithValue("@FileName", filename);
            command.ExecuteNonQuery();

            Logger.LogVerbose($"Updated file existence for '{filename}' to {(exists ? "exists" : "deleted")}.");
        }

        /// <summary>
        /// Updates the FileExists flags in the database by checking the filesystem.
        /// If a file no longer exists, sets FileExists = 0.
        /// </summary>
        public static void RefreshFileExistence()
        {
            EnsureDatabaseExists();

            using var connection = new SqliteConnection($"Data Source={Config.DatabasePath}");
            connection.Open();

            using var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"
        SELECT FileName FROM ImageHashes
        WHERE FileExists = 1;
    ";

            using var reader = selectCommand.ExecuteReader();

            int updatedCount = 0;

            while (reader.Read())
            {
                string fullPath = reader.GetString(0); // Full file path now

                if (!File.Exists(fullPath))
                {
                    UpdateFileExists(fullPath, false);
                    updatedCount++;
                }
            }

            Logger.LogVerbose($"File existence refresh complete. {updatedCount} files marked as missing.");
        }

        /// <summary>
        /// Computes the similarity percentage between two 64-bit image hashes.
        /// </summary>
        private static double ComputeSimilarityPercent(ulong x, ulong y)
        {
            int distance = HammingDistance(x, y);
            double similarity = (1.0 - (distance / 64.0)) * 100.0;
            return similarity;
        }

        /// <summary>
        /// Computes the Hamming distance between two 64-bit hashes.
        /// </summary>
        private static int HammingDistance(ulong x, ulong y)
        {
            ulong val = x ^ y;
            int distance = 0;
            while (val != 0)
            {
                distance++;
                val &= val - 1;
            }
            return distance;
        }
    }
}
