using LiveSplit.SubnauticaTracker.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiveSplit.SubnauticaTracker.Diagnostics
{
    internal static class MissingReportWriter
    {
        public static readonly string FilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "SubnauticaTracker.Missing.Log");

        public static void Write(MissingItems missing)
        {
            var lines = new List<string>
            {
                "\"" + FilePath + "\"",
                string.Empty,
                "# Subnautica Tracker - Missing Blueprints, Databanks & Achievements | Save \""
                    + missing.SaveSlot + "\"",
                string.Empty
            };
            AddSection(lines, "# Blueprints", missing.Blueprints);
            AddSection(lines, "# Databanks", missing.Databanks);
            AddSection(lines, "# Achievements", missing.Achievements);

            File.WriteAllLines(FilePath, lines, new UTF8Encoding(false));
        }

        private static void AddSection(List<string> lines, string heading, string[] items)
        {
            lines.Add(heading + " | Missing " + items.Length);
            lines.Add(string.Empty);
            lines.AddRange(items);
            if (items.Length > 0)
                lines.Add(string.Empty);
        }
    }
}
