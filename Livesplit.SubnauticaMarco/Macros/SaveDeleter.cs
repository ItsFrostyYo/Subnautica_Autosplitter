using LiveSplit.SubnauticaMarco.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LiveSplit.SubnauticaMarco.Macros
{
    // Only the active Subnautica installation's SavedGames/slot#### folders are eligible.
    internal static class SaveDeleter
    {
        private static readonly Regex SlotName = new Regex(@"^slot[0-9]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex GameMode = new Regex("\"gameMode\"\\s*:\\s*([0-9]+)", RegexOptions.CultureInvariant);

        public static string GetSaveRoot(Process process)
        {
            try
            {
                string executable = process.MainModule.FileName;
                if (!string.Equals(Path.GetFileName(executable), "Subnautica.exe", StringComparison.OrdinalIgnoreCase))
                    return null;
                string root = Path.Combine(Path.GetDirectoryName(executable), "SNAppData", "SavedGames");
                return Directory.Exists(root) ? Path.GetFullPath(root) : null;
            }
            catch { return null; }
        }

        public static Dictionary<string, DateTime> ListSlots(string root)
        {
            var slots = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return slots;
            try
            {
                foreach (string path in Directory.GetDirectories(root, "slot*", SearchOption.TopDirectoryOnly))
                {
                    if (!IsValidSlot(root, path)) continue;
                    // Creation time identifies a newly created slot even if gameinfo is written later.
                    slots[path] = Directory.GetCreationTimeUtc(path);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return slots;
        }

        public static string MostRecentMatching(string root, SaveDeletionGameMode mode)
        {
            return ListSlots(root).Keys.Where(p => Matches(root, p, mode))
                .OrderByDescending(p => File.GetLastWriteTimeUtc(Path.Combine(p, "gameinfo.json")))
                .ThenByDescending(p => Directory.GetCreationTimeUtc(p)).FirstOrDefault();
        }

        public static bool Matches(string root, string slot, SaveDeletionGameMode mode)
        {
            if (!IsValidSlot(root, slot)) return false;
            try
            {
                string info = Path.Combine(slot, "gameinfo.json");
                if (!File.Exists(info) || (File.GetAttributes(info) & FileAttributes.ReparsePoint) != 0) return false;
                Match match = GameMode.Match(File.ReadAllText(info));
                int gameMode;
                return match.Success && int.TryParse(match.Groups[1].Value, out gameMode)
                    && (mode == SaveDeletionGameMode.AllGamemodes || gameMode == 2);
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        public static bool Delete(string root, string slot, SaveDeletionGameMode mode)
        {
            if (!Matches(root, slot, mode)) return false;
            // Check again immediately before deletion; no broad directory glob deletions.
            Directory.Delete(slot, true);
            return true;
        }

        private static bool IsValidSlot(string root, string path)
        {
            try
            {
                if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(path) || !SlotName.IsMatch(Path.GetFileName(path))) return false;
                string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
                if (!string.Equals(Path.GetFullPath(Path.GetDirectoryName(path)).TrimEnd(Path.DirectorySeparatorChar), normalizedRoot, StringComparison.OrdinalIgnoreCase)) return false;
                return Directory.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0;
            }
            catch { return false; }
        }
    }
}
