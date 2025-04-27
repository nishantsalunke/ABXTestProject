using System;
using System.IO;

namespace ABXClient.Utils
{
    public static class Logger
    {
        private static readonly string logDirectory = Path.Combine(AppContext.BaseDirectory, "ErrorLog");

        public static void LogError(string message, Exception ex)
        {
            try
            {
                Directory.CreateDirectory(logDirectory);
                TimeZoneInfo indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime istDateTime = TimeZoneInfo.ConvertTime(DateTime.Now, indiaTimeZone);
                string logFileName = $"log-{istDateTime:yyyy-MM-dd}.txt";
                string logFilePath = Path.Combine(logDirectory, logFileName);
                // Create the log entry
                string logEntry = $"{istDateTime:yyyy-MM-dd HH:mm:ss} - {message}\nException: {ex}\n\n";
                File.AppendAllText(logFilePath, logEntry);
            }
            catch (Exception loggingEx)
            {
                Console.WriteLine("Failed to write error to log file.");
                Console.WriteLine($"Original Error: {message}");
                Console.WriteLine($"Logging Error: {loggingEx.Message}");
            }
        }
    }
}