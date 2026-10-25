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
        private readonly object missingItemsSync = new object();

        private ManagedStaticField knownTechField;
        private ManagedStaticField encyclopediaMappingField;
        private ManagedStaticField encyclopediaEntriesField;
        private ManagedStaticField playerMainField;
        private ManagedStaticField saveManagerMainField;
        private ManagedStaticField storyGoalManagerMainField;
        private ManagedStaticField crashedShipExploderMainField;
        private ManagedStaticField biomeGoalTrackerMainField;
        private ManagedStaticField dayNightCycleMainField;
        private ManagedStaticField storyGoalSchedulerMainField;
        private ManagedStaticField gameModeField;
        private ManagedField currentSlotField;
        private ManagedField completedGoalsField;
        private ManagedField onGoalUnlockTrackerField;
        private ManagedField unlockDataField;
        private ManagedField onGoalUnlocksField;
        private ManagedField goalField;
        private ManagedField achievementsField;
        private ManagedField playerLiveMixinField;
        private ManagedField playerBiomeStringField;
        private ManagedField liveMixinDataField;
        private ManagedField liveMixinHealthField;
        private ManagedField liveMixinTempDamageField;
        private ManagedField liveMixinDataMaxHealthField;
        private ManagedField timeToStartCountdownField;
        private ManagedField timeToStartWarningField;
        private ManagedField biomeGoalTrackerGoalsField;
        private ManagedField biomeGoalTrackerLastBiomeField;
        private ManagedField biomeGoalTrackerTimeLastBiomeChangedField;
        private ManagedField biomeGoalKeyField;
        private ManagedField biomeGoalBiomeField;
        private ManagedField dayNightCycleTimePassedField;
        private ManagedField storyGoalSchedulerScheduleField;
        private ManagedField scheduledGoalTimeExecuteField;
        private ManagedField scheduledGoalKeyField;

        private HashSet<int> blueprintUnlockables;
        private HashSet<string> databankUnlockables;
        private Dictionary<string, HashSet<int>> achievementRules;
        private HashSet<int> achievementUnlockables;
        private DateTime nextCatalogRefreshUtc = DateTime.MinValue;
        private bool catalogLogged;
        private HashSet<int> lastBlueprints;
        private HashSet<string> lastDatabanks;
        private HashSet<int> lastAchievements;
        private HashSet<string> lastCompletedGoals;
        private string lastSaveSlot;

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
            if (!runtime.TryResolveStaticField("CrashedShipExploder", MainFieldNames, out crashedShipExploderMainField))
                missing.Add("CrashedShipExploder.main");
            if (!runtime.TryResolveStaticField("BiomeGoalTracker", MainFieldNames, out biomeGoalTrackerMainField))
                missing.Add("BiomeGoalTracker.main");
            if (!runtime.TryResolveStaticField("DayNightCycle", MainFieldNames, out dayNightCycleMainField))
                missing.Add("DayNightCycle.main");
            if (!runtime.TryResolveStaticField("StoryGoalScheduler", MainFieldNames, out storyGoalSchedulerMainField))
                missing.Add("StoryGoalScheduler.main");

            // Game mode is optional so a future game update cannot prevent the
            // progression tracker from initializing if this field changes.
            runtime.TryResolveStaticField(
                "GameModeUtils",
                new[] { "currentGameMode" },
                out gameModeField);

            IntPtr playerClass = runtime.FindClass("Player");
            if (playerClass == IntPtr.Zero)
                missing.Add("class Player");
            else
            {
                if (!runtime.TryFindFieldAny(playerClass, new[] { "liveMixin" }, out playerLiveMixinField))
                    missing.Add("Player.liveMixin");
                if (!runtime.TryFindFieldAny(playerClass, new[] { "biomeString" }, out playerBiomeStringField))
                    missing.Add("Player.biomeString");
            }

            IntPtr crashedShipExploderClass = runtime.FindClass("CrashedShipExploder");
            if (crashedShipExploderClass == IntPtr.Zero)
                missing.Add("class CrashedShipExploder");
            else
            {
                if (!runtime.TryFindFieldAny(
                    crashedShipExploderClass,
                    new[] { "timeToStartCountdown" },
                    out timeToStartCountdownField))
                {
                    missing.Add("CrashedShipExploder.timeToStartCountdown");
                }
                if (!runtime.TryFindFieldAny(
                    crashedShipExploderClass,
                    new[] { "timeToStartWarning" },
                    out timeToStartWarningField))
                {
                    missing.Add("CrashedShipExploder.timeToStartWarning");
                }
            }

            IntPtr biomeGoalTrackerClass = runtime.FindClass("BiomeGoalTracker");
            if (biomeGoalTrackerClass == IntPtr.Zero)
                missing.Add("class BiomeGoalTracker");
            else
            {
                if (!runtime.TryFindFieldAny(biomeGoalTrackerClass, new[] { "goals" }, out biomeGoalTrackerGoalsField))
                    missing.Add("BiomeGoalTracker.goals");
                if (!runtime.TryFindFieldAny(biomeGoalTrackerClass, new[] { "lastBiome" }, out biomeGoalTrackerLastBiomeField))
                    missing.Add("BiomeGoalTracker.lastBiome");
                if (!runtime.TryFindFieldAny(
                    biomeGoalTrackerClass,
                    new[] { "timeLastBiomeChanged" },
                    out biomeGoalTrackerTimeLastBiomeChangedField))
                {
                    missing.Add("BiomeGoalTracker.timeLastBiomeChanged");
                }
            }

            IntPtr biomeGoalClass = runtime.FindClass("BiomeGoal");
            if (biomeGoalClass == IntPtr.Zero)
                missing.Add("class BiomeGoal");
            else
            {
                if (!runtime.TryFindFieldAny(biomeGoalClass, new[] { "key" }, out biomeGoalKeyField))
                    missing.Add("BiomeGoal.key");
                if (!runtime.TryFindFieldAny(biomeGoalClass, new[] { "biome" }, out biomeGoalBiomeField))
                    missing.Add("BiomeGoal.biome");
            }

            IntPtr dayNightCycleClass = runtime.FindClass("DayNightCycle");
            if (dayNightCycleClass == IntPtr.Zero)
                missing.Add("class DayNightCycle");
            else if (!runtime.TryFindFieldAny(
                dayNightCycleClass,
                new[] { "timePassedAsDouble", "timePassed" },
                out dayNightCycleTimePassedField))
            {
                missing.Add("DayNightCycle.timePassedAsDouble");
            }

            IntPtr storyGoalSchedulerClass = runtime.FindClass("StoryGoalScheduler");
            if (storyGoalSchedulerClass == IntPtr.Zero)
                missing.Add("class StoryGoalScheduler");
            else if (!runtime.TryFindFieldAny(
                storyGoalSchedulerClass,
                new[] { "schedule" },
                out storyGoalSchedulerScheduleField))
            {
                missing.Add("StoryGoalScheduler.schedule");
            }

            IntPtr scheduledGoalClass = runtime.FindClass("ScheduledGoal");
            if (scheduledGoalClass == IntPtr.Zero)
                missing.Add("class ScheduledGoal");
            else
            {
                if (!runtime.TryFindFieldAny(scheduledGoalClass, new[] { "timeExecute" }, out scheduledGoalTimeExecuteField))
                    missing.Add("ScheduledGoal.timeExecute");
                if (!runtime.TryFindFieldAny(scheduledGoalClass, new[] { "goalKey" }, out scheduledGoalKeyField))
                    missing.Add("ScheduledGoal.goalKey");
            }

            IntPtr liveMixinClass = runtime.FindClass("LiveMixin");
            if (liveMixinClass == IntPtr.Zero)
                missing.Add("class LiveMixin");
            else if (!runtime.TryFindFieldAny(liveMixinClass, new[] { "health" }, out liveMixinHealthField))
                missing.Add("LiveMixin.health");
            if (liveMixinClass != IntPtr.Zero)
                runtime.TryFindFieldAny(liveMixinClass, new[] { "data" }, out liveMixinDataField);
            if (liveMixinClass != IntPtr.Zero
                && !runtime.TryFindFieldAny(liveMixinClass, new[] { "tempDamage" }, out liveMixinTempDamageField))
            {
                missing.Add("LiveMixin.tempDamage");
            }

            IntPtr liveMixinDataClass = runtime.FindClass("LiveMixinData");
            if (liveMixinDataClass != IntPtr.Zero)
            {
                runtime.TryFindFieldAny(
                    liveMixinDataClass,
                    new[] { "maxHealth" },
                    out liveMixinDataMaxHealthField);
            }

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
                ClearMissingItems();
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
                ClearMissingItems();
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

            HashSet<string> currentDatabanks = new HashSet<string>(
                currentEntries
                .Select(entry => entry.StringKey)
                .Where(key => key != null),
                StringComparer.OrdinalIgnoreCase);
            HashSet<int> currentAchievements = new HashSet<int>(achievementRules
                .Where(rule => completedGoals.Contains(rule.Key))
                .SelectMany(rule => rule.Value)
                .Where(achievementUnlockables.Contains));

            int blueprintCount = currentBlueprints.Count(blueprintUnlockables.Contains);
            int databankCount = currentDatabanks.Count(databankUnlockables.Contains);
            int achievementCount = currentAchievements.Count;

            lock (missingItemsSync)
            {
                lastBlueprints = currentBlueprints;
                lastDatabanks = currentDatabanks;
                lastAchievements = currentAchievements;
                lastCompletedGoals = completedGoals;
                lastSaveSlot = saveSlot;
            }

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

        public bool TryReadPlayerHealth(
            out float health,
            out int displayedHealth,
            out IntPtr player)
        {
            health = 0f;
            displayedHealth = 0;
            player = IntPtr.Zero;
            if (!IsInitialized || !memory.IsAlive)
                return false;

            IntPtr liveMixin;
            float tempDamage;
            if (!playerMainField.TryReadPointer(out player)
                || player == IntPtr.Zero
                || !memory.TryReadPointer(
                    ProcessMemory.Add(player, playerLiveMixinField.Offset),
                    out liveMixin)
                || liveMixin == IntPtr.Zero
                || !memory.TryReadSingle(
                    ProcessMemory.Add(liveMixin, liveMixinHealthField.Offset),
                    out health)
                || !memory.TryReadSingle(
                    ProcessMemory.Add(liveMixin, liveMixinTempDamageField.Offset),
                    out tempDamage))
            {
                return false;
            }

            float maxHealth = 100f;
            if (liveMixinDataField != null && liveMixinDataMaxHealthField != null)
            {
                IntPtr data;
                float configuredMaxHealth;
                if (memory.TryReadPointer(
                        ProcessMemory.Add(liveMixin, liveMixinDataField.Offset),
                        out data)
                    && data != IntPtr.Zero
                    && memory.TryReadSingle(
                        ProcessMemory.Add(data, liveMixinDataMaxHealthField.Offset),
                        out configuredMaxHealth)
                    && !float.IsNaN(configuredMaxHealth)
                    && !float.IsInfinity(configuredMaxHealth)
                    && configuredMaxHealth > 0f
                    && configuredMaxHealth <= 100000f)
                {
                    maxHealth = configuredMaxHealth;
                }
            }

            if (float.IsNaN(health)
                || float.IsInfinity(health)
                || float.IsNaN(tempDamage)
                || float.IsInfinity(tempDamage)
                || health < 0f
                || health > 100000f)
            {
                return false;
            }

            // The HUD ultimately displays CeilToInt(curr * capacity), where
            // curr smooths toward (health - tempDamage) / maxHealth. This is
            // the exact whole-number target the game's health text settles on.
            double visibleHealth = Math.Max(0d, Math.Min(maxHealth, health - tempDamage));
            displayedHealth = (int)Math.Ceiling(visibleHealth);
            return true;
        }

        public bool TryReadGameMode(out int gameMode)
        {
            gameMode = 0;
            if (!IsInitialized
                || !memory.IsAlive
                || gameModeField == null)
            {
                return false;
            }

            return gameModeField.TryReadInt32(out gameMode);
        }

        public bool TryReadBiome(out string biome)
        {
            biome = string.Empty;
            if (!IsInitialized || !memory.IsAlive || playerBiomeStringField == null)
                return false;

            IntPtr player;
            IntPtr biomeString;
            if (!playerMainField.TryReadPointer(out player)
                || player == IntPtr.Zero
                || !memory.TryReadPointer(
                    ProcessMemory.Add(player, playerBiomeStringField.Offset),
                    out biomeString)
                || biomeString == IntPtr.Zero)
            {
                return false;
            }

            biome = memory.ReadMonoString(biomeString, 256);
            return !string.IsNullOrWhiteSpace(biome);
        }

        public bool TryReadBulbTiming(
            out IntPtr world,
            out string playerBiome,
            out string trackerBiome,
            out TimeSpan gameTime,
            out TimeSpan elapsed)
        {
            world = IntPtr.Zero;
            playerBiome = string.Empty;
            trackerBiome = string.Empty;
            gameTime = TimeSpan.Zero;
            elapsed = TimeSpan.Zero;

            IntPtr player;
            IntPtr tracker;
            IntPtr dayNightCycle;
            IntPtr playerBiomeString;
            IntPtr trackerBiomeString;
            double timePassed;
            double timeLastBiomeChanged;
            if (!IsInitialized
                || !memory.IsAlive
                || !playerMainField.TryReadPointer(out player)
                || player == IntPtr.Zero
                || !biomeGoalTrackerMainField.TryReadPointer(out tracker)
                || tracker == IntPtr.Zero
                || !dayNightCycleMainField.TryReadPointer(out dayNightCycle)
                || dayNightCycle == IntPtr.Zero
                || !memory.TryReadPointer(
                    ProcessMemory.Add(player, playerBiomeStringField.Offset),
                    out playerBiomeString)
                || playerBiomeString == IntPtr.Zero
                || !memory.TryReadPointer(
                    ProcessMemory.Add(tracker, biomeGoalTrackerLastBiomeField.Offset),
                    out trackerBiomeString)
                || trackerBiomeString == IntPtr.Zero
                || !memory.TryReadDouble(
                    ProcessMemory.Add(dayNightCycle, dayNightCycleTimePassedField.Offset),
                    out timePassed)
                || !memory.TryReadDouble(
                    ProcessMemory.Add(tracker, biomeGoalTrackerTimeLastBiomeChangedField.Offset),
                    out timeLastBiomeChanged))
            {
                return false;
            }

            playerBiome = memory.ReadMonoString(playerBiomeString, 256);
            trackerBiome = memory.ReadMonoString(trackerBiomeString, 256);
            if (string.IsNullOrWhiteSpace(playerBiome)
                || string.IsNullOrWhiteSpace(trackerBiome)
                || double.IsNaN(timePassed)
                || double.IsInfinity(timePassed)
                || double.IsNaN(timeLastBiomeChanged)
                || double.IsInfinity(timeLastBiomeChanged))
            {
                return false;
            }

            world = tracker;
            gameTime = TimeSpan.FromSeconds(Math.Min(Math.Max(0d, timePassed), 315360000d));
            double elapsedSeconds = Math.Max(0d, timePassed - timeLastBiomeChanged);
            elapsed = TimeSpan.FromSeconds(Math.Min(elapsedSeconds, 86400d));
            return true;
        }

        public bool TryReadBulbTimer(
            out string biome,
            out TimeSpan elapsed,
            out bool goalPending,
            out bool radioScheduled,
            out bool radioCompleted)
        {
            biome = string.Empty;
            elapsed = TimeSpan.Zero;
            goalPending = false;
            radioScheduled = false;
            radioCompleted = false;

            if (!IsInitialized
                || !memory.IsAlive
                || biomeGoalTrackerMainField == null
                || dayNightCycleMainField == null)
            {
                return false;
            }

            IntPtr tracker;
            IntPtr dayNightCycle;
            IntPtr biomeString;
            IntPtr goalsObject;
            double timePassed;
            double timeLastBiomeChanged;
            IList<IntPtr> goals;
            if (!biomeGoalTrackerMainField.TryReadPointer(out tracker)
                || tracker == IntPtr.Zero
                || !dayNightCycleMainField.TryReadPointer(out dayNightCycle)
                || dayNightCycle == IntPtr.Zero
                || !memory.TryReadPointer(
                    ProcessMemory.Add(tracker, biomeGoalTrackerLastBiomeField.Offset),
                    out biomeString)
                || biomeString == IntPtr.Zero
                || !memory.TryReadDouble(
                    ProcessMemory.Add(dayNightCycle, dayNightCycleTimePassedField.Offset),
                    out timePassed)
                || !memory.TryReadDouble(
                    ProcessMemory.Add(tracker, biomeGoalTrackerTimeLastBiomeChangedField.Offset),
                    out timeLastBiomeChanged)
                || !memory.TryReadPointer(
                    ProcessMemory.Add(tracker, biomeGoalTrackerGoalsField.Offset),
                    out goalsObject)
                || !collections.TryReadObjectList(goalsObject, out goals))
            {
                return false;
            }

            biome = memory.ReadMonoString(biomeString, 256);
            if (string.IsNullOrWhiteSpace(biome)
                || double.IsNaN(timePassed)
                || double.IsInfinity(timePassed)
                || double.IsNaN(timeLastBiomeChanged)
                || double.IsInfinity(timeLastBiomeChanged))
            {
                return false;
            }

            double elapsedSeconds = Math.Max(0d, timePassed - timeLastBiomeChanged);
            elapsed = TimeSpan.FromSeconds(Math.Min(elapsedSeconds, 86400d));

            foreach (IntPtr goal in goals)
            {
                if (goal == IntPtr.Zero)
                    continue;

                IntPtr keyString;
                IntPtr goalBiomeString;
                if (!memory.TryReadPointer(
                    ProcessMemory.Add(goal, biomeGoalKeyField.Offset),
                    out keyString)
                    || !memory.TryReadPointer(
                        ProcessMemory.Add(goal, biomeGoalBiomeField.Offset),
                        out goalBiomeString))
                {
                    continue;
                }

                string key = memory.ReadMonoString(keyString, 256);
                string goalBiome = memory.ReadMonoString(goalBiomeString, 256);
                if (string.Equals(key, "RadioRadiationSuit", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(goalBiome, "kooshZone", StringComparison.OrdinalIgnoreCase))
                {
                    goalPending = true;
                    break;
                }
            }

            if (!goalPending)
            {
                IntPtr scheduler;
                IntPtr scheduleObject;
                IList<IntPtr> schedule;
                if (storyGoalSchedulerMainField.TryReadPointer(out scheduler)
                    && scheduler != IntPtr.Zero
                    && memory.TryReadPointer(
                        ProcessMemory.Add(scheduler, storyGoalSchedulerScheduleField.Offset),
                        out scheduleObject)
                    && collections.TryReadObjectList(scheduleObject, out schedule))
                {
                    foreach (IntPtr scheduledGoal in schedule)
                    {
                        if (scheduledGoal == IntPtr.Zero)
                            continue;

                        IntPtr keyString;
                        float timeExecute;
                        if (!memory.TryReadPointer(
                            ProcessMemory.Add(scheduledGoal, scheduledGoalKeyField.Offset),
                            out keyString)
                            || !memory.TryReadSingle(
                                ProcessMemory.Add(scheduledGoal, scheduledGoalTimeExecuteField.Offset),
                                out timeExecute))
                        {
                            continue;
                        }

                        string key = memory.ReadMonoString(keyString, 256);
                        if (string.Equals(key, "RadioRadiationSuit", StringComparison.OrdinalIgnoreCase)
                            && timeExecute <= timePassed + 61d)
                        {
                            radioScheduled = true;
                            break;
                        }
                    }
                }
            }

            HashSet<string> completedGoals = lastCompletedGoals;
            radioCompleted = completedGoals != null
                && completedGoals.Contains("RadioRadiationSuit");

            return true;
        }

        public bool TryReadExplosionTime(out TimeSpan explosionTime)
        {
            explosionTime = TimeSpan.Zero;
            if (!IsInitialized
                || !memory.IsAlive
                || crashedShipExploderMainField == null
                || timeToStartCountdownField == null
                || timeToStartWarningField == null)
            {
                return false;
            }

            IntPtr exploder;
            float countdown;
            float warning;
            if (!crashedShipExploderMainField.TryReadPointer(out exploder)
                || exploder == IntPtr.Zero
                || !memory.TryReadSingle(
                    ProcessMemory.Add(exploder, timeToStartCountdownField.Offset),
                    out countdown)
                || !memory.TryReadSingle(
                    ProcessMemory.Add(exploder, timeToStartWarningField.Offset),
                    out warning))
            {
                return false;
            }

            float seconds = countdown - warning;
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                return false;

            explosionTime = TimeSpan.FromSeconds(seconds);
            return true;
        }

        public bool TryGetMissingItems(out MissingItems missing)
        {
            missing = null;
            lock (missingItemsSync)
            {
                if (lastBlueprints == null
                    || lastDatabanks == null
                    || lastAchievements == null
                    || string.IsNullOrWhiteSpace(lastSaveSlot)
                    || blueprintUnlockables == null
                    || databankUnlockables == null
                    || achievementUnlockables == null
                    || achievementRules == null)
                {
                    return false;
                }

                string[] blueprints = blueprintUnlockables
                    .Except(lastBlueprints)
                    .OrderBy(ProgressNameCatalog.GetBlueprintName, StringComparer.OrdinalIgnoreCase)
                    .Select(ProgressNameCatalog.FormatBlueprint)
                    .ToArray();
                string[] databanks = databankUnlockables
                    .Except(lastDatabanks, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(ProgressNameCatalog.GetDatabankName, StringComparer.OrdinalIgnoreCase)
                    .Select(ProgressNameCatalog.FormatDatabank)
                    .ToArray();
                string[] achievements = achievementUnlockables
                    .Except(lastAchievements)
                    .OrderBy(ProgressNameCatalog.GetAchievementName, StringComparer.OrdinalIgnoreCase)
                    .Select(id => ProgressNameCatalog.GetAchievementName(id)
                        + " - "
                        + string.Join(", ", achievementRules
                            .Where(rule => rule.Value.Contains(id))
                            .Select(rule => rule.Key)
                            .OrderBy(goal => goal, StringComparer.OrdinalIgnoreCase)))
                    .ToArray();

                missing = new MissingItems(lastSaveSlot, blueprints, databanks, achievements);
                return true;
            }
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
                if (DatabankCatalog.IsTracked(entry.StringKey, version) && !IsTimeCapsule(entry.Value))
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

        private void ClearMissingItems()
        {
            lock (missingItemsSync)
            {
                lastBlueprints = null;
                lastDatabanks = null;
                lastAchievements = null;
                lastCompletedGoals = null;
                lastSaveSlot = null;
            }
        }
    }
}
