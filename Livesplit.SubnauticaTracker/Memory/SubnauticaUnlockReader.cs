using LiveSplit.SubnauticaTracker.Catalogs;
using LiveSplit.SubnauticaTracker.Diagnostics;
using LiveSplit.SubnauticaTracker.Versions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveSplit.SubnauticaTracker.Memory
{
    internal enum UnlockReaderState
    {
        Initializing,
        MainMenu,
        Tracking
    }

    internal sealed class UnlockReadResult
    {
        public UnlockReadResult(
            UnlockReaderState state,
            string saveSlot,
            int blueprintsUnlocked,
            int blueprintsTotal,
            int databanksUnlocked,
            int databanksTotal,
            int achievementsUnlocked,
            int achievementsTotal)
        {
            State = state;
            SaveSlot = saveSlot ?? string.Empty;
            BlueprintsUnlocked = blueprintsUnlocked;
            BlueprintsTotal = blueprintsTotal;
            DatabanksUnlocked = databanksUnlocked;
            DatabanksTotal = databanksTotal;
            AchievementsUnlocked = achievementsUnlocked;
            AchievementsTotal = achievementsTotal;
        }

        public UnlockReaderState State { get; }
        public string SaveSlot { get; }
        public int BlueprintsUnlocked { get; }
        public int BlueprintsTotal { get; }
        public int DatabanksUnlocked { get; }
        public int DatabanksTotal { get; }
        public int AchievementsUnlocked { get; }
        public int AchievementsTotal { get; }
    }

    internal sealed class SubnauticaUnlockReader
    {
        private static readonly string[] MainFieldNames =
            { "main", "_main", "s_main", "<main>k__BackingField" };

        private readonly ProcessMemory memory;
        private readonly MonoRuntime runtime;
        private readonly ManagedCollections collections;
        private readonly SubnauticaVersion version;

        private ManagedStaticField knownTechField;
        private ManagedStaticField encyclopediaMappingField;
        private ManagedStaticField encyclopediaEntriesField;
        private ManagedStaticField playerMainField;
        private ManagedStaticField saveManagerMainField;
        private ManagedStaticField storyGoalManagerMainField;
        private ManagedField currentSlotField;
        private ManagedField completedGoalsField;
        private ManagedField onGoalUnlockTrackerField;
        private ManagedField unlockDataField;
        private ManagedField onGoalUnlocksField;
        private ManagedField goalField;
        private ManagedField achievementsField;

        private HashSet<int> blueprintUnlockables;
        private HashSet<string> databankUnlockables;
        private Dictionary<string, HashSet<int>> achievementRules;
        private HashSet<int> achievementUnlockables;
        private DateTime nextCatalogRefreshUtc = DateTime.MinValue;
        private bool catalogLogged;

        public SubnauticaUnlockReader(ProcessMemory memory, SubnauticaVersion version)
        {
            this.memory = memory;
            this.version = version;
            runtime = new MonoRuntime(memory);
            collections = new ManagedCollections(memory, runtime);
        }

        public bool IsInitialized { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public string RuntimeDescription => runtime.LayoutName;

        public bool TryInitialize()
        {
            LastError = string.Empty;
            if (!runtime.IsInitialized && !runtime.TryInitialize())
            {
                LastError = "Mono runtime: " + runtime.LastError
                    + AppendMemoryError();
                return false;
            }

            var missing = new List<string>();
            if (!runtime.TryResolveStaticField("KnownTech", new[] { "knownTech", "known", "knownTechTypes" }, out knownTechField))
                missing.Add("KnownTech.knownTech");
            if (!runtime.TryResolveStaticField("PDAEncyclopedia", new[] { "mapping" }, out encyclopediaMappingField))
                missing.Add("PDAEncyclopedia.mapping");
            if (!runtime.TryResolveStaticField("PDAEncyclopedia", new[] { "entries", "knownEntries" }, out encyclopediaEntriesField))
                missing.Add("PDAEncyclopedia.entries");
            if (!runtime.TryResolveStaticField("Player", MainFieldNames, out playerMainField))
                missing.Add("Player.main");
            if (!runtime.TryResolveStaticField("SaveLoadManager", MainFieldNames, out saveManagerMainField))
                missing.Add("SaveLoadManager.main");
            if (!runtime.TryResolveStaticField("StoryGoalManager", MainFieldNames, out storyGoalManagerMainField))
                missing.Add("StoryGoalManager.main");

            IntPtr saveManagerClass = runtime.FindClass("SaveLoadManager");
            if (saveManagerClass == IntPtr.Zero)
                missing.Add("class SaveLoadManager");
            else if (!runtime.TryFindFieldAny(
                saveManagerClass,
                new[] { "currentSlot", "_currentSlot", "slotName" },
                out currentSlotField))
                missing.Add("SaveLoadManager.currentSlot");

            IntPtr storyGoalManagerClass = runtime.FindClass("StoryGoalManager");
            IntPtr trackerClass = runtime.FindClass("OnGoalUnlockTracker");
            IntPtr unlockDataClass = runtime.FindClass("OnGoalUnlockData");
            IntPtr unlockClass = runtime.FindClass("OnGoalUnlock");
            if (storyGoalManagerClass == IntPtr.Zero)
                missing.Add("class StoryGoalManager");
            else
            {
                if (!runtime.TryFindFieldAny(storyGoalManagerClass, new[] { "completedGoals" }, out completedGoalsField))
                    missing.Add("StoryGoalManager.completedGoals");
                if (!runtime.TryFindFieldAny(storyGoalManagerClass, new[] { "onGoalUnlockTracker" }, out onGoalUnlockTrackerField))
                    missing.Add("StoryGoalManager.onGoalUnlockTracker");
            }
            if (trackerClass == IntPtr.Zero)
                missing.Add("class OnGoalUnlockTracker");
            else if (!runtime.TryFindFieldAny(trackerClass, new[] { "unlockData" }, out unlockDataField))
                missing.Add("OnGoalUnlockTracker.unlockData");
            if (unlockDataClass == IntPtr.Zero)
                missing.Add("class OnGoalUnlockData");
            else if (!runtime.TryFindFieldAny(unlockDataClass, new[] { "onGoalUnlocks" }, out onGoalUnlocksField))
                missing.Add("OnGoalUnlockData.onGoalUnlocks");
            if (unlockClass == IntPtr.Zero)
                missing.Add("class OnGoalUnlock");
            else
            {
                if (!runtime.TryFindFieldAny(unlockClass, new[] { "goal" }, out goalField))
                    missing.Add("OnGoalUnlock.goal");
                if (!runtime.TryFindFieldAny(unlockClass, new[] { "achievements" }, out achievementsField))
                    missing.Add("OnGoalUnlock.achievements");
            }

            IsInitialized = missing.Count == 0;
            if (!IsInitialized)
                LastError = "Unresolved managed metadata: " + string.Join(", ", missing) + ".";
            return IsInitialized;
        }

        public bool TryRead(out UnlockReadResult result)
        {
            LastError = string.Empty;
            result = new UnlockReadResult(UnlockReaderState.Initializing, string.Empty, 0, 0, 0, 0, 0, 0);
            if (!IsInitialized || !memory.IsAlive)
            {
                LastError = !memory.IsAlive ? "The attached game process exited." : "The reader is not initialized.";
                return false;
            }

            IntPtr player;
            IntPtr saveManager;
            string saveSlot = string.Empty;
            if (!playerMainField.TryReadPointer(out player)
                || !saveManagerMainField.TryReadPointer(out saveManager))
            {
                // Managed metadata is ready, but these singleton values are not
                // guaranteed to exist until a world begins loading. At the main
                // menu this is a completed initialization with no active save,
                // not a tracker initialization failure.
                LastError = "Player/SaveLoadManager singletons are not active; no save is loaded.";
                result = new UnlockReadResult(
                    UnlockReaderState.MainMenu,
                    string.Empty,
                    0, 0, 0, 0, 0, 0);
                return true;
            }

            if (saveManager != IntPtr.Zero)
            {
                IntPtr slotString;
                if (memory.TryReadPointer(ProcessMemory.Add(saveManager, currentSlotField.Offset), out slotString))
                    saveSlot = memory.ReadMonoString(slotString, 128);
            }

            if (player == IntPtr.Zero
                || string.IsNullOrWhiteSpace(saveSlot)
                || saveSlot.Equals("test", StringComparison.OrdinalIgnoreCase))
            {
                LastError = "No active loaded save (Player.main/slot is empty).";
                result = new UnlockReadResult(UnlockReaderState.MainMenu, string.Empty, 0, 0, 0, 0, 0, 0);
                return true;
            }

            DateTime now = DateTime.UtcNow;
            if (blueprintUnlockables == null
                || databankUnlockables == null
                || achievementRules == null
                || now >= nextCatalogRefreshUtc)
            {
                HashSet<int> newBlueprints;
                HashSet<string> newDatabanks;
                Dictionary<string, HashSet<int>> newAchievementRules;
                HashSet<int> newAchievements;
                if (!TryBuildBlueprintCatalog(out newBlueprints))
                {
                    LastError = "Could not build the blueprint catalog from live game data. "
                        + LastError
                        + AppendMemoryError();
                    result = new UnlockReadResult(UnlockReaderState.Initializing, saveSlot, 0, 0, 0, 0, 0, 0);
                    return true;
                }
                if (!TryBuildDatabankCatalog(out newDatabanks))
                {
                    LastError = "Could not build the databank catalog from live game data. "
                        + LastError
                        + AppendMemoryError();
                    result = new UnlockReadResult(UnlockReaderState.Initializing, saveSlot, 0, 0, 0, 0, 0, 0);
                    return true;
                }
                if (!TryBuildAchievementCatalog(out newAchievementRules, out newAchievements))
                {
                    LastError = "Could not build the achievement catalog from OnGoalUnlockData."
                        + AppendMemoryError();
                    result = new UnlockReadResult(UnlockReaderState.Initializing, saveSlot, 0, 0, 0, 0, 0, 0);
                    return true;
                }

                blueprintUnlockables = newBlueprints;
                databankUnlockables = newDatabanks;
                achievementRules = newAchievementRules;
                achievementUnlockables = newAchievements;
                if (!catalogLogged)
                {
                    catalogLogged = true;
                    TrackerLog.Info(
                        "Catalogs ready: blueprints " + blueprintUnlockables.Count
                        + ", databanks " + databankUnlockables.Count
                        + ", achievements " + achievementUnlockables.Count + ".");
                }
                // The version's blueprint set and PDA mapping are immutable for
                // the lifetime of the process. Re-reading them only introduces
                // avoidable races while Unity mutates unrelated collections.
                nextCatalogRefreshUtc = DateTime.MaxValue;
            }

            IntPtr knownTechObject;
            HashSet<int> currentBlueprints;
            if (!knownTechField.TryReadPointer(out knownTechObject)
                || !collections.TryReadIntSet(knownTechObject, out currentBlueprints))
            {
                LastError = "Could not read the current save's KnownTech set."
                    + AppendMemoryError();
                result = new UnlockReadResult(UnlockReaderState.Initializing, saveSlot, 0, 0, 0, 0, 0, 0);
                return true;
            }

            IntPtr currentEntriesObject;
            IList<ManagedDictionaryEntry> currentEntries;
            if (!encyclopediaEntriesField.TryReadPointer(out currentEntriesObject)
                || !collections.TryReadStringObjectDictionary(currentEntriesObject, out currentEntries))
            {
                LastError = "Could not read the current save's PDAEncyclopedia.entries dictionary."
                    + AppendMemoryError();
                result = new UnlockReadResult(UnlockReaderState.Initializing, saveSlot, 0, 0, 0, 0, 0, 0);
                return true;
            }

            IntPtr storyGoalManager;
            IntPtr completedGoalsObject;
            HashSet<string> completedGoals;
            if (!storyGoalManagerMainField.TryReadPointer(out storyGoalManager)
                || storyGoalManager == IntPtr.Zero
                || !memory.TryReadPointer(
                    ProcessMemory.Add(storyGoalManager, completedGoalsField.Offset),
                    out completedGoalsObject)
                || !collections.TryReadStringSet(completedGoalsObject, out completedGoals))
            {
                LastError = "Could not read the current save's StoryGoalManager.completedGoals set."
                    + AppendMemoryError();
                result = new UnlockReadResult(UnlockReaderState.Initializing, saveSlot, 0, 0, 0, 0, 0, 0);
                return true;
            }

            int blueprintCount = currentBlueprints.Count(blueprintUnlockables.Contains);
            int databankCount = currentEntries
                .Select(entry => entry.StringKey)
                .Where(key => key != null)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(databankUnlockables.Contains);
            int achievementCount = achievementRules
                .Where(rule => completedGoals.Contains(rule.Key))
                .SelectMany(rule => rule.Value)
                .Distinct()
                .Count(achievementUnlockables.Contains);

            result = new UnlockReadResult(
                UnlockReaderState.Tracking,
                saveSlot,
                blueprintCount,
                blueprintUnlockables.Count,
                databankCount,
                databankUnlockables.Count,
                achievementCount,
                achievementUnlockables.Count);
            return true;
        }

        private bool TryBuildBlueprintCatalog(out HashSet<int> unlockables)
        {
            unlockables = BlueprintCatalog.Create(version);
            return unlockables.Count > 0;
        }

        private bool TryBuildDatabankCatalog(out HashSet<string> unlockables)
        {
            unlockables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            IntPtr mappingObject;
            IList<ManagedDictionaryEntry> mappingEntries;
            if (!encyclopediaMappingField.TryReadPointer(out mappingObject)
                || !collections.TryReadStringObjectDictionary(mappingObject, out mappingEntries))
            {
                LastError = "PDAEncyclopedia.mapping read failed"
                    + (string.IsNullOrWhiteSpace(collections.LastError)
                        ? string.Empty
                        : ": " + collections.LastError)
                    + ".";
                return false;
            }

            foreach (ManagedDictionaryEntry entry in mappingEntries)
            {
                if (!string.IsNullOrWhiteSpace(entry.StringKey) && !IsTimeCapsule(entry.Value))
                    unlockables.Add(entry.StringKey);
            }

            return unlockables.Count > 0;
        }

        private bool TryBuildAchievementCatalog(
            out Dictionary<string, HashSet<int>> rules,
            out HashSet<int> unlockables)
        {
            rules = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
            unlockables = new HashSet<int>();

            IList<IntPtr> unlockObjects;
            if (!TryGetOnGoalUnlocks(out unlockObjects))
                return false;

            foreach (IntPtr unlockObject in unlockObjects)
            {
                if (unlockObject == IntPtr.Zero)
                    continue;

                IntPtr goalString;
                IntPtr achievementArray;
                IList<int> achievements;
                if (!memory.TryReadPointer(ProcessMemory.Add(unlockObject, goalField.Offset), out goalString)
                    || !memory.TryReadPointer(
                        ProcessMemory.Add(unlockObject, achievementsField.Offset),
                        out achievementArray)
                    || achievementArray == IntPtr.Zero
                    || !collections.TryReadIntArray(achievementArray, out achievements))
                {
                    continue;
                }

                string goal = memory.ReadMonoString(goalString, 512);
                if (string.IsNullOrWhiteSpace(goal))
                    continue;

                HashSet<int> goalAchievements = new HashSet<int>(
                    achievements.Where(id => id > 0 && id < 1000));
                if (goalAchievements.Count == 0)
                    continue;

                rules[goal] = goalAchievements;
                unlockables.UnionWith(goalAchievements);
            }

            return rules.Count > 0 && unlockables.Count > 0;
        }

        private bool TryGetOnGoalUnlocks(out IList<IntPtr> unlockObjects)
        {
            unlockObjects = new List<IntPtr>();
            IntPtr storyGoalManager;
            IntPtr tracker;
            IntPtr unlockData;
            IntPtr unlockArray;
            return storyGoalManagerMainField.TryReadPointer(out storyGoalManager)
                && storyGoalManager != IntPtr.Zero
                && memory.TryReadPointer(
                    ProcessMemory.Add(storyGoalManager, onGoalUnlockTrackerField.Offset),
                    out tracker)
                && tracker != IntPtr.Zero
                && memory.TryReadPointer(ProcessMemory.Add(tracker, unlockDataField.Offset), out unlockData)
                && unlockData != IntPtr.Zero
                && memory.TryReadPointer(ProcessMemory.Add(unlockData, onGoalUnlocksField.Offset), out unlockArray)
                && collections.TryReadObjectArray(unlockArray, out unlockObjects);
        }

        private bool IsTimeCapsule(IntPtr entryData)
        {
            if (entryData == IntPtr.Zero)
                return false;

            IntPtr entryClass;
            if (!runtime.TryGetObjectClass(entryData, out entryClass))
                return false;

            ManagedField kindField;
            int kind;
            if (runtime.TryFindFieldAny(entryClass, new[] { "kind" }, out kindField)
                && memory.TryReadInt32(ProcessMemory.Add(entryData, kindField.Offset), out kind))
            {
                return kind == 2;
            }

            ManagedField timeCapsuleField;
            byte timeCapsule;
            return runtime.TryFindFieldAny(entryClass, new[] { "timeCapsule" }, out timeCapsuleField)
                && memory.TryReadByte(ProcessMemory.Add(entryData, timeCapsuleField.Offset), out timeCapsule)
                && timeCapsule != 0;
        }

        private string AppendMemoryError()
        {
            return string.IsNullOrWhiteSpace(memory.LastError)
                ? string.Empty
                : " Last memory error: " + memory.LastError;
        }
    }
}
