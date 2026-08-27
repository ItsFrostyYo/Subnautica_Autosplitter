using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LiveSplit.SubnauticaTracker.Versions
{
    internal enum SubnauticaVersion
    {
        Unknown,
        Build2018,
        Build2021,
        Build2023,
        Build2025
    }

    internal sealed class GameVersionInfo
    {
        public GameVersionInfo(SubnauticaVersion version, string gameRoot, string assemblyPath, bool exactMatch)
        {
            Version = version;
            GameRoot = gameRoot ?? string.Empty;
            AssemblyPath = assemblyPath ?? string.Empty;
            ExactMatch = exactMatch;
        }

        public SubnauticaVersion Version { get; }
        public string GameRoot { get; }
        public string AssemblyPath { get; }
        public bool ExactMatch { get; }

        public string DisplayName
        {
            get
            {
                switch (Version)
                {
                    case SubnauticaVersion.Build2018: return "2018";
                    case SubnauticaVersion.Build2021: return "2021";
                    case SubnauticaVersion.Build2023: return "2023";
                    case SubnauticaVersion.Build2025: return "2025";
                    default: return "Compatible";
                }
            }
        }
    }

    internal static class GameVersionDetector
    {
        private static readonly IDictionary<string, SubnauticaVersion> KnownAssemblyHashes =
            new Dictionary<string, SubnauticaVersion>(StringComparer.OrdinalIgnoreCase)
            {
                { "1667EC5EF4475659FAD2487BFD67BFF1F1560721304B1605629898870849ABB7", SubnauticaVersion.Build2018 },
                { "064517BDC1230FF471C7C9FC76FCB0D3E61E2A4E7392D7BD3BF9469297BDD637", SubnauticaVersion.Build2021 },
                { "450CAAD45063CA5965B74E88A029DE834DAB323AD57FB23B83831D6B179DBEB9", SubnauticaVersion.Build2023 },
                { "3D4D4EE8153D9ADEFC8A63E516822755716D03DD17BF52B87ECDC39206D62BBD", SubnauticaVersion.Build2025 }
            };

        public static GameVersionInfo Detect(Process process)
        {
            string executablePath = process.MainModule.FileName;
            string gameRoot = Path.GetDirectoryName(executablePath);
            string assemblyPath = Path.Combine(
                gameRoot,
                "Subnautica_Data",
                "Managed",
                "Assembly-CSharp.dll");

            if (!File.Exists(assemblyPath))
                return new GameVersionInfo(SubnauticaVersion.Unknown, gameRoot, assemblyPath, false);

            string hash = ComputeSha256(assemblyPath);
            SubnauticaVersion exactVersion;
            if (KnownAssemblyHashes.TryGetValue(hash, out exactVersion))
                return new GameVersionInfo(exactVersion, gameRoot, assemblyPath, true);

            // Structural fallback keeps nearby patches compatible. These markers
            // describe code generations, while memory fields are resolved live.
            if (!File.Exists(Path.Combine(gameRoot, "UnityPlayer.dll")))
                return new GameVersionInfo(SubnauticaVersion.Build2018, gameRoot, assemblyPath, false);

            if (ContainsAscii(assemblyPath, "GameInputSteam"))
                return new GameVersionInfo(SubnauticaVersion.Build2025, gameRoot, assemblyPath, false);

            if (ContainsAscii(assemblyPath, "UnlockAchievementSampleLogic"))
                return new GameVersionInfo(SubnauticaVersion.Build2023, gameRoot, assemblyPath, false);

            return new GameVersionInfo(SubnauticaVersion.Build2021, gameRoot, assemblyPath, false);
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        private static bool ContainsAscii(string path, string value)
        {
            byte[] needle = Encoding.ASCII.GetBytes(value);
            byte[] bytes = File.ReadAllBytes(path);

            for (int i = 0; i <= bytes.Length - needle.Length; i++)
            {
                int j = 0;
                while (j < needle.Length && bytes[i + j] == needle[j])
                    j++;

                if (j == needle.Length)
                    return true;
            }

            return false;
        }
    }
}
