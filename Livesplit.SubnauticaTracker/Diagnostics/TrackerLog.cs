using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace LiveSplit.SubnauticaTracker.Diagnostics
{
    internal static class TrackerLog
    {
        private static readonly object Sync = new object();
        private static readonly IDictionary<string, DateTime> LastWrites =
            new Dictionary<string, DateTime>(StringComparer.Ordinal);

        public static readonly string FilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "SubnauticaTracker.log");

        public static void StartSession()
        {
            lock (Sync)
            {
                try
                {
                    File.WriteAllText(FilePath, string.Empty);
                    LastWrites.Clear();

                    Assembly assembly = Assembly.GetExecutingAssembly();
                    Append("INFO", "Tracker started (v" + assembly.GetName().Version + ").");
                }
                catch
                {
                    // Diagnostics must never affect the LiveSplit component.
                }
            }
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Exception(string context, Exception exception)
        {
            Write(
                "ERROR",
                context + ": " + exception.GetType().Name + " - " + exception.Message);
        }

        public static void Throttled(string key, string message, TimeSpan interval)
        {
            lock (Sync)
            {
                DateTime now = DateTime.UtcNow;
                DateTime last;
                if (LastWrites.TryGetValue(key, out last) && now - last < interval)
                    return;

                LastWrites[key] = now;
                try { Append("WARN", message); }
                catch { }
            }
        }

        private static void Write(string level, string message)
        {
            lock (Sync)
            {
                try { Append(level, message); }
                catch { }
            }
        }

        private static void Append(string level, string message)
        {
            File.AppendAllText(
                FilePath,
                DateTime.Now.ToString("HH:mm:ss")
                    + " [" + level + "] "
                    + (message ?? string.Empty)
                    + Environment.NewLine);
        }
    }
}
