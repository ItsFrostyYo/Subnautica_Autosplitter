using LiveSplit.ComponentUtil;
using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Voxif.AutoSplitter;
using Voxif.Helpers.Unity;
using Voxif.IO;
using Voxif.Memory;
using static Livesplit.Subnautica.SubnauticaSplitSettings;
using HSLayout = Voxif.Helpers.Unity.UnityHelperTask.HashSetLayout;

namespace Livesplit.Subnautica
{
    public class SubnauticaMemory : Memory
    {
        protected override string[] ProcessNames => new string[] { "Subnautica" };
        
        LiveSplitState _state;

        IMonoHelper mono;

        public bool startedTimerBefore = false;
        public bool isInMainMenu = false;
        public bool fakePortalLoading = false;
        private int tickCounter = 0;
        public bool pointersInitialized;
        public GameVersion gameVersion;

        public readonly Dictionary<SplitName, Func<bool>> splitConditions;
        public readonly HashSet<SplitName> alreadySplit = new HashSet<SplitName>();

        private SubnauticaSettings settings;

        #region Pointer stuff
        public Pointer<bool> isIntroCinematicActive;
        public Pointer<bool> isAnimationPlaying;
        public Pointer<float> timeCured;
        public Pointer<float> health;
        public Pointer<IntPtr> mainMenu;
        public StringPointer biome;

        public List<TechType> playerInventory = new List<TechType>();
        public List<TechType> playerInventoryOld = new List<TechType>();

        public List<TechType> knownTech = new List<TechType>();
        public List<TechType> knownTechOld = new List<TechType>();

        public List<string> completedGoals = new List<string>();
        public List<string> completedGoalsOld = new List<string>();

        IntPtr invKlass;
        IntPtr icKlass;
        int off_container;
        int off_itemsDict;
        IntPtr itemGroupKlass;
        int off_itemGroup_items;
        int off_list_size;
        int dict_off_entries;
        int arr_off_len;
        int arr_data_base;

        IntPtr invStaticKlass;
        int invStaticOffset;

        IntPtr ktStaticKlass;
        int ktStaticOffset;
        HSLayout hsLayoutKnownTech;
        bool hsLayoutKnownTechReady = false;

        public MemoryWatcher<bool> isLoadingScreen = new MemoryWatcher<bool>(IntPtr.Zero);
        public MemoryWatcher<bool> isPortalLoading = new MemoryWatcher<bool>(IntPtr.Zero);
        public MemoryWatcher<bool> isEggsHatching = new MemoryWatcher<bool>(IntPtr.Zero);
        public MemoryWatcher<bool> isNotInWater = new MemoryWatcher<bool>(IntPtr.Zero);
        public MemoryWatcher<bool> isDying = new MemoryWatcher<bool>(IntPtr.Zero);
        public MemoryWatcher<int> isFabiOpen = new MemoryWatcher<int>(IntPtr.Zero); // 2 means that the esc menu is open
        public MemoryWatcher<int> isPDAOpen = new MemoryWatcher<int>(IntPtr.Zero); // true = 1051931443, false = 1056964608
        public MemoryWatcher<int> isRocketLaunching = new MemoryWatcher<int>(IntPtr.Zero); // 2018 = 1, 2023 = 256
        public MemoryWatcher<int> oxygen = new MemoryWatcher<int>(IntPtr.Zero);
        public MemoryWatcher<float> walkDir = new MemoryWatcher<float>(IntPtr.Zero);
        public MemoryWatcher<float> strafeDir = new MemoryWatcher<float>(IntPtr.Zero);
        public MemoryWatcher<float> posX = new MemoryWatcher<float>(IntPtr.Zero);
        public MemoryWatcher<float> posY = new MemoryWatcher<float>(IntPtr.Zero);
        public MemoryWatcher<float> posZ = new MemoryWatcher<float>(IntPtr.Zero);
        #endregion
        private UnityHelperTask unityTask;

        public SubnauticaMemory(LiveSplitState state, Logger logger, SubnauticaSettings settings) : base(logger)
        {            
            OnHook += () =>
            {
                GetGameVersion();
                unityTask = new UnityHelperTask(game, logger);
                unityTask.Run(InitPointers);
            };

            OnExit += () => {
                if (unityTask != null)
                {
                    pointersInitialized = false;
                    unityTask.Dispose();
                    unityTask = null;
                }
            };

            _state = state;
            this.settings = settings;
            
            splitConditions = new Dictionary<SplitName, Func<bool>>
            {
                { SplitName.RocketSplit,          () => isRocketLaunching.Current != isRocketLaunching.Old && (isRocketLaunching.Current == 1 || isRocketLaunching.Current == 256) },
                { SplitName.PCFTabletSplit,       () => isAnimationPlaying.New && !isAnimationPlaying.Old && IsWithinBounds(PCFEntrBounds) },
                { SplitName.PortalSplit,          () => !alreadySplit.Contains(SplitName.PortalSplit) && isPortalLoading.Current && !isPortalLoading.Old && IsWithinBounds(portalBounds) },
                { SplitName.HatchSplit,           () => isEggsHatching.Current && !isEggsHatching.Old },
                { SplitName.CureSplit,            () => timeCured.New > timeCured.Old },
                { SplitName.BoostersSplit,        () => knownTech.Contains(TechType.RocketStage2) && !knownTechOld.Contains(TechType.RocketStage2) },
                { SplitName.FuelReservesSplit,    () => knownTech.Contains(TechType.RocketStage3) && !knownTechOld.Contains(TechType.RocketStage3) },
                { SplitName.GunDeactivationSplit, () => !alreadySplit.Contains(SplitName.GunDeactivationSplit) && isAnimationPlaying.New && !isAnimationPlaying.Old && IsWithinBounds(gunBounds) },
                { SplitName.BaseDeathSplit,       () => health.New <= 0 && health.Old > 0 && (IsWithinBounds(deathClipABounds) || IsWithinBounds(deathClipCBounds)) },
                { SplitName.LeaveKelpForestSplit, () => !alreadySplit.Contains(SplitName.LeaveKelpForestSplit) && IsWithinBounds(teethBounds) && playerInventory.Contains(TechType.CreepvinePiece) },
                { SplitName.FourToothSplit,       () => !alreadySplit.Contains(SplitName.FourToothSplit) && playerInventory.Count(t => t == TechType.StalkerTooth) == 4 && playerInventoryOld.Count(t => t == TechType.StalkerTooth) != 4 },
                { SplitName.AuroraDeathSplit,     () => !alreadySplit.Contains(SplitName.AuroraDeathSplit) && !alreadySplit.Contains(SplitName.AuroraBiomeSplit) && health.New <= 0 && health.Old > 0 && new[] { "crashedShip", "generatorRoom" }.Contains(biome.New)},
                { SplitName.RocketUnlockSplit,    () => knownTech.Contains(TechType.RocketBase) && !knownTechOld.Contains(TechType.RocketBase) },
                { SplitName.MountainDescendSplit, () => !alreadySplit.Contains(SplitName.MountainDescendSplit) && IsWithinBounds(mountainBounds) },
                { SplitName.IonDeathSplit,        () => health.New <= 0 && health.Old > 0 && new[] { "Precursor_LavaCastleBase", "PrecursorThermalRoom" }.Contains(biome.New) },
                { SplitName.GunDeathSplit,        () => health.New <= 0 && health.Old > 0 && biome.New == "Precursor_Gun_ControlRoom" },
                { SplitName.SparseDeathSplit,     () => !alreadySplit.Contains(SplitName.SparseDeathSplit) && !alreadySplit.Contains(SplitName.SparseBiomeSplit) && health.New <= 0 && health.Old > 0 && new[] { "sparseReef", "seaTreaderPath", "seaTreaderPath_wreck" }.Contains(biome.New) },
                { SplitName.SGLBaseSplit,         () => !alreadySplit.Contains(SplitName.SGLBaseSplit) && isNotInWater.Current && !isNotInWater.Old && IsWithinBounds(SGLBaseBounds) },
                { SplitName.SGLShallowsSplit,     () => !alreadySplit.Contains(SplitName.SGLShallowsSplit) && !isNotInWater.Current && isAnimationPlaying.New && IsWithinBounds(SGLBaseBounds) && playerInventory.Contains(TechType.DoubleTank) },
                { SplitName.UpperTabletSplit,     () => playerInventory.Count(t => t == TechType.PrecursorKey_Purple) > playerInventoryOld.Count(t => t == TechType.PrecursorKey_Purple) && IsWithinBounds(upperTabletBounds) },
                { SplitName.IonUnstuckSplit,      () => isAnimationPlaying.New && !isAnimationPlaying.Old && biome.New == "PrecursorThermalRoom" },
                { SplitName.PCFPoolSplit,         () => !alreadySplit.Contains(SplitName.PCFPoolSplit) && biome.New == "Prison_Aquarium_Upper" && biome.Old == "Prison_Moonpool" },
                { SplitName.SparseBiomeSplit,     () => !alreadySplit.Contains(SplitName.SparseBiomeSplit) && !alreadySplit.Contains(SplitName.SparseDeathSplit) && new[] { "sparseReef", "seaTreaderPath", "seaTreaderPath_wreck" }.Contains(biome.Old) && new[] { "safeShallows", "kelpForest", "Lifepod" }.Contains(biome.New) },
                { SplitName.AuroraBiomeSplit,     () => !alreadySplit.Contains(SplitName.AuroraBiomeSplit) && !alreadySplit.Contains(SplitName.AuroraDeathSplit) && new[] { "crashedShip", "generatorRoom" }.Contains(biome.Old) && new[] { "safeShallows", "kelpForest", "Lifepod" }.Contains(biome.New) },
                { SplitName.EyestalkSplit,        () => !alreadySplit.Contains(SplitName.EyestalkSplit) && playerInventory.Contains(TechType.EyesPlantSeed) && !playerInventoryOld.Contains(TechType.EyesPlantSeed) },
                { SplitName.IonUnlockSplit,       () => knownTech.Contains(TechType.PrecursorIonBattery) && !knownTechOld.Contains(TechType.PrecursorIonBattery) },
                { SplitName.AuroraExitSplit,      () => !alreadySplit.Contains(SplitName.AuroraExitSplit) && IsWithinBounds(auroraExitBounds) && knownTech.Contains(TechType.RocketBase) },
                { SplitName.HCGSparseSplit,       () => !alreadySplit.Contains(SplitName.HCGSparseSplit) && isAnimationPlaying.New && !isAnimationPlaying.Old && (IsWithinBounds(enterClipABounds) || IsWithinBounds(enterClipCBounds)) && playerInventory.Contains(TechType.AluminumOxide) },
                { SplitName.DeathSplit,           () => health.New <= 0 && health.Old > 0 },
            };
        }
        public override bool Update()
        {           
            if(!pointersInitialized)
                return base.Update();

            UpdateMemoryWatchers();

            isInMainMenu = IsInMainMenu();
            if (isInMainMenu)
                startedTimerBefore = false;

            //logger.Log($"New={playerInventory.Count}, Old={playerInventoryOld.Count}, items={t}");
            var dict = ReadInventoryCounts();
            foreach (var kv in dict)
                logger.Log($"{kv.Key}: {kv.Value}");
            UpdateBlueprints();
            
            
            return base.Update();
        }

        #region Memory stuff
        private void GetGameVersion()
        {
            System.Diagnostics.ProcessModule firstModule = game.Process.Modules.Cast<System.Diagnostics.ProcessModule>().FirstOrDefault();
            if (firstModule == null) return;
            int moduleLen = firstModule.ModuleMemorySize;
            switch (moduleLen)
            {
                case 23801856:
                    gameVersion = GameVersion.Sept2018;
                    logger.Log("Game version Sept 2018");
                    break;

                case 675840:
                    gameVersion = GameVersion.Mar2023;
                    logger.Log("Game version March 2023");
                    break;

                default:
                    gameVersion = GameVersion.Mar2023;
                    MessageBox.Show($"Module length {moduleLen} does not match a version, defaulting to most recent (March 2023)",
                                    "Subnautica Autosplitter",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                    break;
            }
        }

        private void InitPointers(IMonoHelper mono)
        {
            this.mono = mono;
            var ptrFactory = new MonoNestedPointerFactory(game, mono);

            #region Intro Cinematic
            Pointer<IntPtr> introCinematicPtr = ptrFactory.Make<IntPtr>("EscapePod", "main", "introCinematic");
            IntPtr pccKlass = mono.FindClass("PlayerCinematicController");
            int off_cinematicModeActive = mono.GetFieldOffset(pccKlass, "cinematicModeActive");
            isIntroCinematicActive = ptrFactory.Make<bool>(introCinematicPtr, off_cinematicModeActive);
            #endregion Intro Cinematic
            #region Is Animation Playing
            isAnimationPlaying = ptrFactory.Make<bool>("Player", "main", "_cinematicModeActive");
            #endregion Is Animation Playing
            #region Time Cured
            timeCured = ptrFactory.Make<float>("Player", "main", "timePlayerInfectionCured");
            #endregion
            #region Health
            Pointer<IntPtr> liveMixingPtr = ptrFactory.Make<IntPtr>("Player", "main", "liveMixin");
            IntPtr lmKlass = mono.FindClass("LiveMixin");
            int off_health = mono.GetFieldOffset(lmKlass, "health");
            health = ptrFactory.Make<float>(liveMixingPtr, off_health);
            #endregion
            #region Inventory
            invKlass = mono.FindClass("Inventory", mono.MainImage);
            icKlass = mono.FindClass("ItemsContainer", mono.MainImage);

            invStaticOffset = mono.GetFieldOffset(invKlass, "main");
            invStaticKlass = invKlass;

            off_container = ((UnityHelperTask.UnityHelperBase)mono)
                .ResolveFieldOffsetByNameOrPredicate(invKlass, new[] { "_container" },
                    fname => UnityHelperTask.UnityNameUtil.NameHas(fname, "container"));

            off_itemsDict = ((UnityHelperTask.UnityHelperBase)mono)
                .ResolveFieldOffsetByNameOrPredicate(icKlass, new[] { "_items" },
                    fname => UnityHelperTask.UnityNameUtil.NameHas(fname, "items"));

            itemGroupKlass = mono.FindClass("ItemGroup", mono.MainImage);
            off_itemGroup_items = (itemGroupKlass != IntPtr.Zero)
                ? mono.GetFieldOffset(itemGroupKlass, "items") : 0;
            if (off_itemGroup_items == 0 && itemGroupKlass != IntPtr.Zero)
            {
                off_itemGroup_items = ((UnityHelperTask.UnityHelperBase)mono)
                    .ResolveFieldOffsetByNameOrPredicate(itemGroupKlass, new[] { "items" },
                        fname => UnityHelperTask.UnityNameUtil.NameHas(fname, "items"));
            }

            // List<T>._size
            IntPtr core = ((UnityHelperTask.UnityHelperBase)mono).TryFindImageOnce(
                "mscorlib", "mscorlib.dll", "System.Private.CoreLib", "System.Private.CoreLib.dll", "netstandard", "netstandard.dll");
            IntPtr listKlass = core != IntPtr.Zero ? ((UnityHelperTask.UnityHelperBase)mono).TryFindClassOnce("List`1", core) : IntPtr.Zero;
            off_list_size = 0x18;
            if (listKlass != IntPtr.Zero)
            {
                int cand = mono.GetFieldOffset(listKlass, "_size");
                if (cand != 0) off_list_size = cand;
            }

            // Dictionary<>.entries, Array header
            dict_off_entries = 0x18;
            arr_off_len = 0x18;
            arr_data_base = 0x20;
            if (core != IntPtr.Zero)
            {
                IntPtr dictKlass = ((UnityHelperTask.UnityHelperBase)mono).TryFindClassOnce("Dictionary`2", core);
                if (dictKlass != IntPtr.Zero)
                {
                    int oe = mono.GetFieldOffset(dictKlass, "entries");
                    if (oe == 0) oe = mono.GetFieldOffset(dictKlass, "_entries");
                    if (oe != 0) dict_off_entries = oe;
                }
            }
            #endregion Inventory
            #region Known Tech
            ktStaticKlass = mono.GetStaticField(mono.MainImage, "KnownTech", "knownTech", out _, out ktStaticOffset);
            logger.Log($"KnownTech static base={ktStaticKlass:X}, staticOffset={ktStaticOffset:X}");

            var baseHelper = (UnityHelperTask.UnityHelperBase)mono;
            hsLayoutKnownTech = baseHelper.ResolveHashSetLayoutForInt();
            hsLayoutKnownTechReady = hsLayoutKnownTech.Ready || true;
            #endregion Known Tech
            #region Main Menu
            mainMenu = ptrFactory.Make<IntPtr>("uGUI_MainMenu", "main");
            #endregion
            #region Biome
            biome = ptrFactory.MakeString("Player", "main", "biomeString", 0x14);
            #endregion
            #region Goals
            var sgm = mono.FindClass("StoryGoalManager", mono.MainImage);

            int offMain = mono.GetFieldOffset(sgm, "<main>k__BackingField");
            int offCompleted = mono.GetFieldOffset(sgm, "completedGoals");

            IntPtr statBase = mono.GetStaticAddress(sgm);
            IntPtr sgmInst = game.Read<IntPtr>(statBase + offMain);
            if (sgmInst == IntPtr.Zero) {  }

            IntPtr hs = game.Read<IntPtr>(sgmInst + offCompleted);
            completedGoals = ((UnityHelperTask.UnityHelperBase)mono).ReadHashSetString(hs);
            #endregion
            #region Memory Watchers
            DeepPointer loadingScreenPtr;
            DeepPointer portalLoadingPtr;
            DeepPointer hatchPtr;
            DeepPointer notInWaterPtr;
            DeepPointer dyingPtr;
            DeepPointer fabiPtr;
            DeepPointer PDAPtr;
            DeepPointer rocketPtr;
            DeepPointer oxygenPtr;
            DeepPointer walkDirPtr;
            DeepPointer strafePtr;
            DeepPointer posX;
            DeepPointer posY;
            DeepPointer posZ;

            switch (gameVersion)
            {
                case GameVersion.Sept2018:
                    loadingScreenPtr = new DeepPointer("mono.dll", 0x266180, 0x50, 0x2C0, 0x0, 0x30, 0x8, 0x18, 0x20, 0x10, 0x44);
                    portalLoadingPtr = new DeepPointer("Subnautica.exe", 0x142B740, 0x8, 0x10, 0x30, 0x1F8, 0x28, 0x28);
                    hatchPtr = new DeepPointer("fmodstudio.dll", 0x304A30, 0x88, 0x18, 0x158, 0x498, 0x108);
                    notInWaterPtr = new DeepPointer("Subnautica.exe", 0x14BC6A0, 0x7C);
                    dyingPtr = new DeepPointer("Subnautica.exe", 0x142B740, 0x8, 0x8, 0x10, 0x30, 0x2C8, 0x28, 0x20);
                    fabiPtr = new DeepPointer("mono.dll", 0x296BC8, 0x20, 0xA58, 0x20);
                    PDAPtr = new DeepPointer("mono.dll", 0x2655E0, 0x40, 0x18, 0xA0, 0x920, 0x64);
                    rocketPtr = new DeepPointer("mono.dll", 0x27EAD8, 0x40, 0x70, 0x50, 0x90, 0x30, 0x8, 0x80);
                    oxygenPtr = new DeepPointer("Subnautica.exe", 0x142ADA8, 0x8, 0x10, 0x30, 0x30, 0x18, 0x28, 0x70);
                    walkDirPtr = new DeepPointer("Subnautica.exe", 0x142B8C8, 0x158, 0x40, 0xA0);
                    strafePtr = new DeepPointer("Subnautica.exe", 0x142B8C8, 0x158, 0x40, 0x160);
                    posX = new DeepPointer("Subnautica.exe", 0x142B8C8, 0x180, 0x40, 0xA8, 0x7C0);
                    posY = new DeepPointer("Subnautica.exe", 0x142B8C8, 0x180, 0x40, 0xA8, 0x7C4);
                    posZ = new DeepPointer("Subnautica.exe", 0x142B8C8, 0x180, 0x40, 0xA8, 0x7C8);
                    break;

                default: // GameVersion.Mar2023
                    loadingScreenPtr = new DeepPointer("UnityPlayer.dll", 0x18AB2E0, 0x430, 0x8, 0x10, 0x48, 0x30, 0x7AC);
                    portalLoadingPtr = new DeepPointer("UnityPlayer.dll", 0x17FBE70, 0x10, 0x10, 0x30, 0x1F8, 0x28, 0x28);
                    hatchPtr = new DeepPointer("fmodstudio.dll", 0x2CED70, 0x78, 0x18, 0x190, 0x4D8, 0xB0, 0x20, 0x28);
                    notInWaterPtr = new DeepPointer("UnityPlayer.dll", 0x18AB130, 0x48, 0x0, 0x68);
                    dyingPtr = new DeepPointer("UnityPlayer.dll", 0x17FBE70, 0x8, 0x10, 0x30, 0x318, 0x28, 0x50);
                    fabiPtr = new DeepPointer("UnityPlayer.dll", 0x183BF48, 0x8, 0x10, 0x30, 0x30, 0x28, 0x128);
                    PDAPtr = new DeepPointer("mono-2.0-bdwgc.dll", 0x499C40, 0xE84);
                    rocketPtr = new DeepPointer("UnityPlayer.dll", 0x17FC238, 0x10, 0x3C);
                    oxygenPtr = new DeepPointer(IntPtr.Zero);
                    walkDirPtr = new DeepPointer("UnityPlayer.dll", 0x17FBC28, 0x30, 0x98);
                    strafePtr = new DeepPointer("UnityPlayer.dll", 0x17FBC28, 0x30, 0x150);
                    posX = new DeepPointer("UnityPlayer.dll", 0x1839CE0, 0x28, 0x10, 0x150, 0xA58);
                    posY = new DeepPointer("UnityPlayer.dll", 0x1839CE0, 0x28, 0x10, 0x150, 0xA5C);
                    posZ = new DeepPointer("UnityPlayer.dll", 0x1839CE0, 0x28, 0x10, 0x150, 0xA60);
                    break;
            }

            isRocketLaunching = new MemoryWatcher<int>(rocketPtr);
            isLoadingScreen = new MemoryWatcher<bool>(loadingScreenPtr);
            isPortalLoading = new MemoryWatcher<bool>(portalLoadingPtr);
            isEggsHatching = new MemoryWatcher<bool>(hatchPtr);
            isNotInWater = new MemoryWatcher<bool>(notInWaterPtr);
            isDying = new MemoryWatcher<bool>(dyingPtr);
            isFabiOpen = new MemoryWatcher<int>(fabiPtr);
            isPDAOpen = new MemoryWatcher<int>(PDAPtr);
            oxygen = new MemoryWatcher<int>(oxygenPtr);
            walkDir = new MemoryWatcher<float>(walkDirPtr);
            strafeDir = new MemoryWatcher<float>(strafePtr);
            this.posX = new MemoryWatcher<float>(posX);
            this.posY = new MemoryWatcher<float>(posY);
            this.posZ = new MemoryWatcher<float>(posZ);
            #endregion Memory Watchers

            logger.Log("Pointers initialized");
            pointersInitialized = true;
        }

        Dictionary<TechType, int> ReadInventoryCounts()
        {
            var result = new Dictionary<TechType, int>();

            IntPtr invMain = game.Read<IntPtr>(mono.GetStaticAddress(invStaticKlass) + invStaticOffset);
            if (invMain == IntPtr.Zero) return result;

            IntPtr container = game.Read<IntPtr>(invMain + off_container);
            if (container == IntPtr.Zero) return result;

            IntPtr dict = game.Read<IntPtr>(container + off_itemsDict);
            if (dict == IntPtr.Zero) return result;

            IntPtr entriesArr = game.Read<IntPtr>(dict + dict_off_entries);
            if (entriesArr == IntPtr.Zero) return result;

            int len = game.Read<int>(entriesArr + arr_off_len);
            if (len <= 0 || len > 200000) return result;

            IntPtr basePtr = entriesArr + arr_data_base;

            int off_itemGroup_id = mono.GetFieldOffset(itemGroupKlass, "id");
            int stride = PickDictStrideByKeyMatches(basePtr, len, off_itemGroup_id,
                                                    addr => game.Read<int>((IntPtr)addr),
                                                    addr => (long)game.Read<IntPtr>((IntPtr)addr));

            for (int i = 0; i < len; i++)
            {
                IntPtr entry = basePtr + i * stride;
                int hashCode = game.Read<int>(entry + 0x0);
                if (hashCode < 0) continue;

                int keyInt = game.Read<int>(entry + 0x8);
                IntPtr pGroup = game.Read<IntPtr>(entry + 0x10);
                if (pGroup == IntPtr.Zero) continue;

                int id = game.Read<int>(pGroup + off_itemGroup_id);
                if (id != keyInt) continue;

                IntPtr pList = game.Read<IntPtr>(pGroup + off_itemGroup_items);
                if (pList == IntPtr.Zero) continue;

                int count = game.Read<int>(pList + off_list_size);
                if ((uint)count > 100000) continue;

                result[(TechType)keyInt] = count;
            }

            return result;
        }

        int PickDictStrideByKeyMatches(IntPtr basePtr, int length, int off_itemGroup_id, Func<long, int> ReadInt32, Func<long, long> ReadPtr)
        {
            int probe = Math.Min(length, 128);
            int hits24 = 0, hits32 = 0;

            for (int i = 0; i < probe; i++)
            {
                long e24 = (long)basePtr + i * 24;
                int h24 = ReadInt32(e24 + 0x0);
                if (h24 >= 0)
                {
                    int k24 = ReadInt32(e24 + 0x8);
                    long g24 = ReadPtr(e24 + 0x10);
                    if (g24 != 0)
                    {
                        int id24 = ReadInt32(g24 + off_itemGroup_id);
                        if (id24 == k24) hits24++;
                    }
                }

                long e32 = (long)basePtr + i * 32;
                int h32 = ReadInt32(e32 + 0x0);
                if (h32 >= 0)
                {
                    int k32 = ReadInt32(e32 + 0x8);
                    long g32 = ReadPtr(e32 + 0x10);
                    if (g32 != 0)
                    {
                        int id32 = ReadInt32(g32 + off_itemGroup_id);
                        if (id32 == k32) hits32++;
                    }
                }
            }

            return (hits24 > hits32 * 2) ? 24 : 32;
        }

        private void UpdateMemoryWatchers()
        {
            if (settings.introStart && gameVersion == GameVersion.Sept2018)
                oxygen.Update(game.Process);

            if (settings.creativeStart)
            {
                walkDir.Update(game.Process);
                strafeDir.Update(game.Process);
                isFabiOpen.Update(game.Process);
                isPDAOpen.Update(game.Process);
                isLoadingScreen.Update(game.Process);
            }

            if (Needs(SplitName.PortalSplit))
                isPortalLoading.Update(game.Process);

            if (Needs(SplitName.HatchSplit))
                isEggsHatching.Update(game.Process);

            if (Needs(SplitName.SGLBaseSplit, SplitName.SGLShallowsSplit))
                isNotInWater.Update(game.Process);

            if (Needs(SplitName.BaseDeathSplit,
                      SplitName.AuroraDeathSplit,
                      SplitName.IonDeathSplit,
                      SplitName.SparseDeathSplit,
                      SplitName.GunDeathSplit))
                isDying.Update(game.Process);

            if (Needs(SplitName.RocketSplit))
                isRocketLaunching.Update(game.Process);

            if (Needs(SplitName.PCFTabletSplit,
                      SplitName.GunDeactivationSplit,
                      SplitName.BaseDeathSplit,
                      SplitName.LeaveKelpForestSplit,
                      SplitName.MountainDescendSplit,
                      SplitName.SGLBaseSplit,
                      SplitName.SGLShallowsSplit,
                      SplitName.UpperTabletSplit,
                      SplitName.AuroraExitSplit,
                      SplitName.HCGSparseSplit) ||
                      settings.reset)
                UpdatePosition();

            if (Needs(SplitName.LeaveKelpForestSplit,
                      SplitName.FourToothSplit,
                      SplitName.HCGSparseSplit,
                      SplitName.SGLShallowsSplit,
                      SplitName.UpperTabletSplit))
                UpdatePosition();//UpdateInventory();

            if (Needs(SplitName.BoostersSplit,
                      SplitName.FuelReservesSplit,
                      SplitName.RocketUnlockSplit,
                      SplitName.AuroraExitSplit,
                      SplitName.IonUnlockSplit))
                UpdatePosition();//UpdateBlueprints();
        }
        private void UpdatePosition() { posX.Update(game.Process); posY.Update(game.Process); posZ.Update(game.Process); }
        private bool Needs(params SplitName[] required) => required.Any(r => settings.Splits.Contains(r));
        #endregion Memory stuff
        #region World/Player Checks
        public bool IsInMainMenu() => posX.Current == 0 && posZ.Current == 0 && posY.Current == 1.75f;

        private void UpdateBlueprints()
        {
            IntPtr hs = game.Read<IntPtr>(mono.GetStaticAddress(ktStaticKlass) + ktStaticOffset);

            List<TechType> current = new List<TechType>();
            if (hs != IntPtr.Zero)
            {
                var baseHelper = (UnityHelperTask.UnityHelperBase)mono;

                var ints = baseHelper.ReadHashSetInt(hs, hsLayoutKnownTech);

                foreach (int v in ints)
                {
                    if (v > 0 && v < 10005)
                        current.Add((TechType)v);
                }
            }

            knownTechOld = knownTech;
            knownTech = current;
        }

        private bool IsWithinBounds(float[] bounds)
        {
            float x = posX.Current;
            float y = posY.Current;
            float z = posZ.Current;
            if (x >= Math.Min(bounds[0], bounds[1]) && x <= Math.Max(bounds[0], bounds[1]) &&
                y >= Math.Min(bounds[2], bounds[3]) && y <= Math.Max(bounds[2], bounds[3]) &&
                z >= Math.Min(bounds[4], bounds[5]) && z <= Math.Max(bounds[4], bounds[5]))
                return true;
            else
                return false;
        }

        public bool ShouldPause()
        {
            if (isInMainMenu)
                return false;

            if (settings.SRCLoadtimes)
            {
                // Start of portal load
                if (isPortalLoading.Current && !isPortalLoading.Old)
                {
                    fakePortalLoading = true;
                    tickCounter = gameVersion == GameVersion.Sept2018 ? 30 : 33;
                }

                // End of portal load
                if (!isPortalLoading.Current && isPortalLoading.Old)
                {
                    fakePortalLoading = false;
                    tickCounter = gameVersion == GameVersion.Sept2018 ? 21 : 0;
                }

                if (tickCounter > 0)
                    tickCounter--;
                else
                {
                    if (fakePortalLoading)
                        return true;
                    else
                        return false;
                }
            }
            else
            {
                return isPortalLoading.Current;
            }
            return false;
        }

        #endregion
        #region Bounds
        // xmin, xmax, ymin, ymax, zmin, zmax
        private readonly float[] teethBounds = { -212f, 27f, -100f, 100f, 159f, 177f };
        private readonly float[] auroraExitBounds = { 545f, 550f, -10f, 10f, -265f, 256f };
        private readonly float[] mountainBounds = { 475f, 534f, -510f, -191f, 745f, 810f };
        private readonly float[] PCFEntrBounds = { 216f, 224f, -1453f, -1445f, -276f, -267f };
        private readonly float[] portalBounds = { 240f, 250f, -1590f, -1580f, -2000f, 2000f };
        private readonly float[] gunBounds = { 359f, 365f, -75f, -66f, 1079f, 1085f };
        private readonly float[] upperTabletBounds = { 380f, 386f, 10f, 30f, 1084f, 1090f };
        private readonly float[] SGLBaseBounds = { 20f, 80f, -45f, -17f, 290f, 360f };
        private readonly float[] deathClipABounds = { 33f, 65f, -20f, -8f, 118f, 96f };
        private readonly float[] deathClipCBounds = { -155f, -133f, -20f, -10f, 73f, 96f };
        private readonly float[] enterClipABounds = { 48f, 55f, -20f, -5f, 106f, 111f };
        private readonly float[] enterClipCBounds = { -142f, -132f, -20f, -5f, 82f, 90f };
        #endregion
    }
}