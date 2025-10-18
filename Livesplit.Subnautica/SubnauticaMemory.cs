using LiveSplit.ComponentUtil;
using LiveSplit.Model;
using LiveSplit.Options;
using LiveSplit.VoxSplitter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Livesplit.Subnautica.SubnauticaSplitSettings;

namespace Livesplit.Subnautica
{
    public class SubnauticaMemory : Memory
    {
        private bool isReady = true;

        private bool startedTimerBefore = false;
        public bool isInMainMenu = false;
        public bool isInMainMenuOld = false;
        private bool fakePortalLoading = false;
        private int tickCounter = 0;
        private GameVersion gameVersion;

        private readonly Dictionary<SplitName, Func<bool>> splitConditions;
        private readonly HashSet<SplitName> alreadySplit = new HashSet<SplitName>();

        private readonly MonoHelper mono;
        private SubnauticaSettings settings;

        #region Pointers
        private Pointer<bool> isIntroCinematicActive;
        private Pointer<bool> isLoadingScreen;
        private Pointer<bool> isAnimationPlaying;
        private Pointer<bool> isPortalLoading;
        private Pointer<bool> isEggsHatching;
        private Pointer<bool> isNotInWater;
        private Pointer<bool> isDying;
        private Pointer<int> isFabiOpen; // 2 means that the esc menu is open
        private Pointer<int> isPDAOpen; // true = 1051931443, false = 1056964608
        private Pointer<int> isRocketLaunching; // 2018 = 1, 2023 = 256
        private Pointer<int> oxygen;
        private Pointer<float> timeCured;
        private Pointer<float> walkDir;
        private Pointer<float> strafeDir;
        private Pointer<float> posX;
        private Pointer<float> posY;
        private Pointer<float> posZ;

        // pointer to the beginning of the string
        private Pointer<IntPtr> biomePtr;
        private string biomeString;
        private string biomeStringOld;

        private List<TechType> playerInventory = new List<TechType>();
        private List<TechType> playerInventoryOld = new List<TechType>();

        private List<TechType> knownTech = new List<TechType>();
        private List<TechType> knownTechOld = new List<TechType>();


        Pointer<IntPtr> itemsMapPtr;
        Pointer<int> sizeXPtr, sizeYPtr;

        IntPtr iiKlass;
        IntPtr puKlass;

        int off_container, off_itemsMap, off_sizeX, off_sizeY;
        int off_ii_techType, off_ii_item;
        int off_pu_overrideUsed, off_pu_overrideTechType;

        IntPtr invStaticKlass;
        int invStaticOffset;

        IntPtr ktStaticKlass; 
        int ktStaticOffset;   
        int off_knownTech;


        int hsCountOff = 0;
        int hsSlotsOff = 0;
        int hsArrayDataBase = 0x20; 
        int hsValueStride = 0;     
        int hsValueOff = 0;     
        bool hsLayoutReady = false;
        #endregion        

        public SubnauticaMemory(LiveSplitState state, Logger logger, SubnauticaSettings settings) : base(state, logger)
        {
            SetProcessNames("Subnautica");
            mono = new MonoHelper(this);
            this.settings = settings;

            splitConditions = new Dictionary<SplitName, Func<bool>>
            {
                { SplitName.RocketSplit,          () => isRocketLaunching.New != isRocketLaunching.Old && (isRocketLaunching.New == 1 || isRocketLaunching.New == 256) },
                { SplitName.PCFTabletSplit,       () => isAnimationPlaying.New && !isAnimationPlaying.Old && IsWithinBounds(PCFEntrBounds) },
                { SplitName.PortalSplit,          () => !alreadySplit.Contains(SplitName.PortalSplit) && isPortalLoading.New && !isPortalLoading.Old && IsWithinBounds(portalBounds) },
                { SplitName.HatchSplit,           () => isEggsHatching.New && !isEggsHatching.Old },
                { SplitName.CureSplit,            () => timeCured.New > timeCured.Old },
                { SplitName.BoostersSplit,        () => knownTech.Contains(TechType.RocketStage2) && !knownTechOld.Contains(TechType.RocketStage2) },
                { SplitName.FuelReservesSplit,    () => knownTech.Contains(TechType.RocketStage3) && !knownTechOld.Contains(TechType.RocketStage3) },
                { SplitName.GunDeactivationSplit, () => !alreadySplit.Contains(SplitName.GunDeactivationSplit) && isAnimationPlaying.New && !isAnimationPlaying.Old && IsWithinBounds(gunBounds) },
                { SplitName.BaseDeathSplit,       () => isDying.New && !isDying.Old && (IsWithinBounds(deathClipABounds) || IsWithinBounds(deathClipCBounds)) },
                { SplitName.LeaveKelpForestSplit, () => !alreadySplit.Contains(SplitName.LeaveKelpForestSplit) && IsWithinBounds(teethBounds) && playerInventory.Contains(TechType.CreepvinePiece) },
                { SplitName.FourToothSplit,       () => !alreadySplit.Contains(SplitName.FourToothSplit) && playerInventory.Count(t => t == TechType.StalkerTooth) == 4 && playerInventoryOld.Count(t => t == TechType.StalkerTooth) != 4 },
                { SplitName.AuroraDeathSplit,     () => !alreadySplit.Contains(SplitName.AuroraDeathSplit) && !alreadySplit.Contains(SplitName.AuroraBiomeSplit) && isDying.New && !isDying.Old && new[] { "crashedShip", "generatorRoom" }.Contains(biomeString)},
                { SplitName.RocketUnlockSplit,    () => knownTech.Contains(TechType.RocketBase) && !knownTechOld.Contains(TechType.RocketBase) },
                { SplitName.MountainDescendSplit, () => !alreadySplit.Contains(SplitName.MountainDescendSplit) && IsWithinBounds(mountainBounds) },
                { SplitName.IonDeathSplit,        () => isDying.New && !isDying.Old && new[] { "Precursor_LavaCastleBase", "PrecursorThermalRoom" }.Contains(biomeString) },
                { SplitName.GunDeathSplit,        () => isDying.New && !isDying.Old && biomeString == "Precursor_Gun_ControlRoom" },
                { SplitName.SparseDeathSplit,     () => !alreadySplit.Contains(SplitName.SparseDeathSplit) && !alreadySplit.Contains(SplitName.SparseBiomeSplit) && isDying.New && !isDying.Old && new[] { "sparseReef", "seaTreaderPath", "seaTreaderPath_wreck" }.Contains(biomeString) },
                { SplitName.SGLBaseSplit,         () => !alreadySplit.Contains(SplitName.SGLBaseSplit) && isNotInWater.New && !isNotInWater.Old && IsWithinBounds(SGLBaseBounds) },
                { SplitName.SGLShallowsSplit,     () => !alreadySplit.Contains(SplitName.SGLShallowsSplit) && !isNotInWater.New && isAnimationPlaying.New && IsWithinBounds(SGLBaseBounds) && playerInventory.Contains(TechType.DoubleTank) },
                { SplitName.UpperTabletSplit,     () => playerInventory.Count(t => t == TechType.PrecursorKey_Purple) > playerInventoryOld.Count(t => t == TechType.PrecursorKey_Purple) && IsWithinBounds(upperTabletBounds) },
                { SplitName.IonUnstuckSplit,      () => isAnimationPlaying.New && !isAnimationPlaying.Old && biomeString == "PrecursorThermalRoom" },
                { SplitName.PCFPoolSplit,         () => !alreadySplit.Contains(SplitName.PCFPoolSplit) && biomeString == "Prison_Aquarium_Upper" && biomeStringOld == "Prison_Moonpool" },
                { SplitName.SparseBiomeSplit,     () => !alreadySplit.Contains(SplitName.SparseBiomeSplit) && !alreadySplit.Contains(SplitName.SparseDeathSplit) && new[] { "sparseReef", "seaTreaderPath", "seaTreaderPath_wreck" }.Contains(biomeStringOld) && new[] { "safeShallows", "kelpForest", "Lifepod" }.Contains(biomeString) },
                { SplitName.AuroraBiomeSplit,     () => !alreadySplit.Contains(SplitName.AuroraBiomeSplit) && !alreadySplit.Contains(SplitName.AuroraDeathSplit) && new[] { "crashedShip", "generatorRoom" }.Contains(biomeStringOld) && new[] { "safeShallows", "kelpForest", "Lifepod" }.Contains(biomeString) },
                { SplitName.EyestalkSplit,        () => !alreadySplit.Contains(SplitName.EyestalkSplit) && playerInventory.Contains(TechType.EyesPlantSeed) && !playerInventoryOld.Contains(TechType.EyesPlantSeed) },
                { SplitName.IonUnlockSplit,       () => knownTech.Contains(TechType.PrecursorIonBattery) && !knownTechOld.Contains(TechType.PrecursorIonBattery) },
                { SplitName.AuroraExitSplit,      () => !alreadySplit.Contains(SplitName.AuroraExitSplit) && IsWithinBounds(auroraExitBounds) && knownTech.Contains(TechType.RocketBase) },
                { SplitName.HCGSparseSplit,       () => !alreadySplit.Contains(SplitName.HCGSparseSplit) && isAnimationPlaying.New && !isAnimationPlaying.Old && (IsWithinBounds(enterClipABounds) || IsWithinBounds(enterClipCBounds)) && playerInventory.Contains(TechType.AluminumOxide) },
                { SplitName.DeathSplit,           () => isDying.New && !isDying.Old },
            };
        }

        public override bool IsReady() => base.IsReady() && mono.IsCompleted;

        protected override void OnHook()
        {
            GetGameVersion();

            mono.Run(() =>
            {
                var invKlass = mono.GetClass(mono.mainImage, "Inventory");
                var icKlass = mono.GetClass(mono.mainImage, "ItemsContainer");
                iiKlass = mono.GetClass(mono.mainImage, "InventoryItem");
                puKlass = mono.GetClass(mono.mainImage, "Pickupable");

                invStaticKlass = mono.GetStaticField(mono.mainImage, "Inventory", "main", out _, out invStaticOffset);
                IntPtr invMainPtr = IntPtr.Zero;
                while (invMainPtr == IntPtr.Zero)
                {
                    invMainPtr = Game.Read<IntPtr>(mono.GetStaticData(invStaticKlass) + invStaticOffset);
                    Thread.Sleep(50);
                }
                Logger.Log($"Inventory.main -> {invMainPtr:X}");

                off_container = ResolveFieldOffsetByNameOrPredicate(
                    invKlass,
                    new[] { "_container" },
                    (fname, ftype) => NameHas(fname, "container")
                );

                off_itemsMap = ResolveFieldOffsetByNameOrPredicate(
                    icKlass,
                    new[] { "itemsMap" },
                    (fname, ftype) => NameHas(fname, "itemsmap")
                );

                off_sizeX = ResolveFieldOffsetByNameOrPredicate(
                    icKlass,
                    new[] { "<sizeX>k__BackingField", "sizeX" },
                    (fname, ftype) => NameHas(fname, "sizex")
                );
                off_sizeY = ResolveFieldOffsetByNameOrPredicate(
                    icKlass,
                    new[] { "<sizeY>k__BackingField", "sizeY" },
                    (fname, ftype) => NameHas(fname, "sizey")
                );

                off_ii_techType = ResolveFieldOffsetByNameOrPredicate(
                    iiKlass,
                    new[] { "_techType", "techType", "<TechType>k__BackingField", "m_TechType" },
                    (fname, ftype) => mono.GetClassName(ftype).EndsWith("TechType", StringComparison.Ordinal)
                );

                off_ii_item = ResolveFieldOffsetByNameOrPredicate(
                    iiKlass,
                    new[] { "<item>k__BackingField", "item", "m_Item" },
                    (fname, ftype) => mono.GetClassName(ftype).EndsWith("Pickupable", StringComparison.Ordinal)
                );

                off_pu_overrideUsed = ResolveFieldOffsetByNameOrPredicate(
                    puKlass,
                    new[] { "overrideTechUsed" },
                    (fname, ftype) => NameHas(fname, "override") && NameHas(fname, "used")
                );
                off_pu_overrideTechType = ResolveFieldOffsetByNameOrPredicate(
                    puKlass,
                    new[] { "overrideTechType" },
                    (fname, ftype) => NameHas(fname, "override") && NameHas(fname, "tech")
                );

                var ktKlass = mono.GetClass(mono.mainImage, "KnownTech");

                off_knownTech = ResolveFieldOffsetByNameOrPredicate(
                    ktKlass,
                    new[] { "knownTech" },
                    (fname, ftype) => NameHas(fname, "known") && NameHas(fname, "tech")
                );

                // sanity log
                Logger.Log($"off_container={off_container:X}, off_itemsMap={off_itemsMap:X}, off_sizeX={off_sizeX:X}, off_sizeY={off_sizeY:X}");
                Logger.Log($"off_ii_item={off_ii_item:X}, off_ii_techType={off_ii_techType:X}, off_pu_used={off_pu_overrideUsed:X}, off_pu_tt={off_pu_overrideTechType:X}");
                ktStaticKlass = mono.GetStaticField(mono.mainImage, "KnownTech", "knownTech", out _, out ktStaticOffset);
                Logger.Log($"KnownTech static base={ktStaticKlass:X}, off_knownTech={off_knownTech:X}, staticOffset={ktStaticOffset:X}");
            });
        }

        public override bool Update()
        {
            UpdateBlueprints();
            foreach (var i in knownTech)
                Logger.Log(i.ToString());
            Logger.Log(knownTech.Count.ToString());

            return isReady;
        }

        public override bool Start()
        {
            if (startedTimerBefore)
                return false;

            if (settings.introStart)
            {
                if (gameVersion == GameVersion.Sept2018 && oxygen.New == 45 && oxygen.Old < 45) { Logger.Log("Start of oxygen"); startedTimerBefore = true; return true; }
                if (!isIntroCinematicActive.New && isIntroCinematicActive.Old) { Logger.Log("Start of introCinematic"); startedTimerBefore = true; return true; }
            }
            if (settings.creativeStart && !isLoadingScreen.New && !isInMainMenu)
            {
                // Start of Move
                if ((walkDir.New != 0 && walkDir.Old == 0) || (strafeDir.New != 0 && strafeDir.Old == 0)) { Logger.Log("Start of Move"); startedTimerBefore = true; return true; }

                // Start of Fabricator
                if (isFabiOpen.New == 1 && isFabiOpen.Old == 0) { Logger.Log("Start of Fabricator"); startedTimerBefore = true; return true; }

                // Start of PDA
                if (isPDAOpen.New == 1051931443 && isPDAOpen.New != isPDAOpen.Old) { Logger.Log("Start of PDA"); startedTimerBefore = true; return true; }
            }
            return false;
        }

        public override void OnStart()
        {
           
        }

        public override bool Split()
        {
            foreach (var split in settings.Splits)
            {
                if (splitConditions.TryGetValue(split, out var condition) && condition())
                {
                    alreadySplit.Add(split);
                    Logger.Log($"{split} triggered");
                    return true;
                }
            }
            return false;
        }

        public override bool Loading()
        {
            return false;
        }

        public override void OnExit()
        {
            isReady = false;
        }

        public override void Dispose() => mono.Dispose();

        #region World/Player Checks

        private bool IsWithinBounds(float[] bounds)
        {
            float x = posX.New;
            float y = posY.New;
            float z = posZ.New;
            if (x >= Math.Min(bounds[0], bounds[1]) && x <= Math.Max(bounds[0], bounds[1]) &&
                y >= Math.Min(bounds[2], bounds[3]) && y <= Math.Max(bounds[2], bounds[3]) &&
                z >= Math.Min(bounds[4], bounds[5]) && z <= Math.Max(bounds[4], bounds[5]))
                return true;
            else
                return false;
        }

        private bool ShouldPause()
        {
            if (isInMainMenu)
                return false;

            if (settings.SRCLoadtimes)
            {
                // Start of portal load
                if (isPortalLoading.New && !isPortalLoading.Old)
                {
                    fakePortalLoading = true;
                    tickCounter = gameVersion == GameVersion.Sept2018 ? 30 : 33;
                }

                // End of portal load
                if (!isPortalLoading.New && isPortalLoading.Old)
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
                return isPortalLoading.New;
            }
            return false;
        }

        public bool IsInMainMenu() => posX.New == 0 && posZ.New == 0 && posY.New == 1.75f;

        private void UpdateInventory()
        {
            var inv = new List<TechType>();
            IntPtr invMain = Game.Read<IntPtr>(mono.GetStaticData(invStaticKlass) + invStaticOffset);
            if (invMain == IntPtr.Zero) return;

            IntPtr container = Game.Read<IntPtr>(invMain + off_container);
            if (container == IntPtr.Zero) return;

            IntPtr itemsMap = Game.Read<IntPtr>(container + off_itemsMap);
            int sizeX = Game.Read<int>(container + off_sizeX);
            int sizeY = Game.Read<int>(container + off_sizeY);
            if (itemsMap == IntPtr.Zero || sizeX <= 0 || sizeY <= 0) return;

            IntPtr GetInventoryItemAt(int x, int y)
            {
                if ((uint)x >= (uint)sizeX || (uint)y >= (uint)sizeY) return IntPtr.Zero;
                int index = y * sizeX + x;
                int elemOffset = arrayDataBase + index * mono.MonoInfo.pointer_size;
                return Game.Read<IntPtr>(itemsMap + elemOffset);
            }

            int GetTechTypeAt(int x, int y)
            {
                IntPtr pInvItem = GetInventoryItemAt(x, y);
                if (pInvItem == IntPtr.Zero) return (int)TechType.None;

                if (off_ii_techType != 0)
                {
                    int tt = Game.Read<int>(pInvItem + off_ii_techType);
                    if (tt != (int)TechType.None) return tt;
                }

                if (off_ii_item != 0 && off_pu_overrideUsed != 0 && off_pu_overrideTechType != 0)
                {
                    IntPtr pPickupable = Game.Read<IntPtr>(pInvItem + off_ii_item);
                    if (pPickupable != IntPtr.Zero)
                    {
                        bool used = Game.Read<byte>(pPickupable + off_pu_overrideUsed) != 0;
                        if (used)
                            return Game.Read<int>(pPickupable + off_pu_overrideTechType);
                    }
                }

                return (int)TechType.None;
            }

            for (int y = 0; y < sizeY; y++)
                for (int x = 0; x < sizeX; x++)
                {
                    int tt = GetTechTypeAt(x, y);
                    if (tt != (int)TechType.None)
                        inv.Add((TechType)tt);
                }

            playerInventoryOld = playerInventory;
            playerInventory = inv;
        }

        void UpdateBlueprints()
        {
            var blueprints = new List<TechType>();

            IntPtr hs = Game.Read<IntPtr>(mono.GetStaticData(ktStaticKlass) + ktStaticOffset);
            if (hs == IntPtr.Zero) { knownTechOld = knownTech; knownTech = new List<TechType>(); return; }

            ResolveHashSetLayoutUsingMono(hs);
            if (!hsLayoutReady) { knownTechOld = knownTech; knownTech = new List<TechType>(); return; }

            IntPtr slotsArr = Game.Read<IntPtr>(hs + hsSlotsOff);
            int arrayLen = Game.Read<int>(slotsArr + 0x18);
            IntPtr slotsData = slotsArr + hsArrayDataBase;
            int stride = hsValueStride, voff = hsValueOff;
            int probe = Math.Min(arrayLen, 64);
            int hits12 = 0, hits16 = 0;
            for (int i = 0; i < probe; i++)
            {
                int v12 = Game.Read<int>(slotsData + i * 12 + 8);
                if (v12 > 0 && v12 < 10005) hits12++;
                int v16 = Game.Read<int>(slotsData + i * 16 + 8);
                if (v16 > 0 && v16 < 10005) hits16++;
            }
            if (hits16 > hits12 * 2) { stride = 16; voff = 8; }

            // Auslesen
            for (int i = 0; i < arrayLen; i++)
            {
                int tech = Game.Read<int>(slotsData + i * stride + voff);
                if (tech > 0 && tech < 10005)
                    blueprints.Add((TechType)tech);
            }

            var dedup = blueprints.Distinct().ToList();
            Logger.Log($"KnownTech: hs={hs:X}, slotsOff=+0x{hsSlotsOff:X}, len={arrayLen}, unique={dedup.Count}");

            knownTechOld = knownTech;
            knownTech = dedup;
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

        private enum GameVersion
        {
            Sept2018,
            Mar2023
        }

        #region memory stuff
        private void GetGameVersion()
        {
            ProcessModule firstModule = Game.Modules.Cast<ProcessModule>().FirstOrDefault();
            if (firstModule == null) return;
            int moduleLen = firstModule.ModuleMemorySize;
            switch (moduleLen)
            {
                case 23801856:
                    gameVersion = GameVersion.Sept2018;
                    Logger.Log("Game version Sept 2018");
                    break;

                case 675840:
                    gameVersion = GameVersion.Mar2023;
                    Logger.Log("Game version March 2023");
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

        int ResolveFieldOffsetByNameOrPredicate(IntPtr klass, string[] nameCandidates, Func<string, IntPtr, bool> predicate)
        {
            foreach (var n in nameCandidates)
            {
                if (string.IsNullOrEmpty(n)) continue;
                int off = mono.GetFieldOffset(klass, n);
                if (off != 0) return off;
            }

            foreach (var f in mono.FieldSequence(klass, includeParents: true))
            {
                string fname = mono.GetFieldName(f);
                IntPtr ftype = mono.GetType(f);
                if (predicate(fname, ftype))
                    return mono.GetFieldOffset(f);
            }

            return 0;
        }

        static bool NameHas(string name, params string[] needles)
        {
            name = name ?? "";
            foreach (var s in needles)
                if (name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        int arrayDataBase = 0x20; // default
        void ResolveItemsMapDataBase()
        {
            IntPtr invMain = Game.Read<IntPtr>(mono.GetStaticData(invStaticKlass) + invStaticOffset);
            IntPtr container = Game.Read<IntPtr>(invMain + off_container);
            if (container == IntPtr.Zero) return;

            IntPtr itemsMap = Game.Read<IntPtr>(container + off_itemsMap);
            int[] candidates = { 0x20, 0x18, 0x28 }; // test a few

            foreach (int cand in candidates)
            {
                // lies slot (0,0)
                IntPtr p0 = Game.Read<IntPtr>(itemsMap + cand + 0 * mono.MonoInfo.pointer_size);
                if (p0 == IntPtr.Zero) continue;

                if (off_ii_item != 0 || off_ii_techType != 0)
                {
                    int off_ii_container = ResolveFieldOffsetByNameOrPredicate(
                        iiKlass,
                        new[] { "container" },
                        (fname, ftype) => NameHas(fname, "container")
                    );
                    if (off_ii_container != 0)
                    {
                        IntPtr back = Game.Read<IntPtr>(p0 + off_ii_container);
                        if (back == container)
                        {
                            arrayDataBase = cand;
                            Logger.Log($"itemsMap data base resolved: 0x{arrayDataBase:X}");
                            return;
                        }
                    }
                }
            }

            Logger.Log($"itemsMap data base fallback: 0x{arrayDataBase:X}");
        }
        int PickSlotsOffset(IntPtr hs)
        {
            int[] candidates = { 0x10, 0x18 }; // buckets vs. slots
            int bestOff = 0; int bestScore = -1;

            foreach (int off in candidates)
            {
                IntPtr arr = Game.Read<IntPtr>(hs + off);
                int len = Game.Read<int>(arr + 0x18);
                if (len <= 0 || len > 50000) continue;

                IntPtr data = arr + 0x20;
                // teste Stride 12/16
                int score12 = 0, score16 = 0, probe = Math.Min(len, 64);
                for (int i = 0; i < probe; i++)
                {
                    int v12 = Game.Read<int>(data + i * 12 + 8);
                    if (v12 > 0 && v12 < 10005) score12++;
                    int v16 = Game.Read<int>(data + i * 16 + 8);
                    if (v16 > 0 && v16 < 10005) score16++;
                }
                int score = Math.Max(score12, score16);
                if (score > bestScore) { bestScore = score; bestOff = off; hsValueStride = (score16 > score12) ? 16 : 12; hsValueOff = 8; }
            }
            return bestOff; // 0 = fail
        }
        void ResolveHashSetLayoutUsingMono(IntPtr hs)
        {
            if (hsLayoutReady) return;
            if (hs == IntPtr.Zero) return;

            IntPtr sysImg = IntPtr.Zero;
            foreach (var name in new[] {
        "mscorlib", "mscorlib.dll",
        "System.Private.CoreLib", "System.Private.CoreLib.dll",
        "netstandard", "netstandard.dll"
    })
            {
                sysImg = mono.GetModuleImage(name);
                if (sysImg != IntPtr.Zero) break;
            }

            if (sysImg == IntPtr.Zero)
            {
                Logger.Log("Could not find any CoreLib image -> using picker fallback");
                hsSlotsOff = PickSlotsOffset(hs);
                hsArrayDataBase = 0x20;
                hsLayoutReady = hsSlotsOff != 0;
                Logger.Log($"KnownTech HashSet picked slots@+{hsSlotsOff:X}, stride={hsValueStride}");
                return;
            }

            IntPtr hsKlass = mono.GetClass(sysImg, "HashSet`1");
            if (hsKlass == IntPtr.Zero)
            {
                Logger.Log("HashSet`1 class not found in CoreLib image -> using picker fallback");
                hsSlotsOff = PickSlotsOffset(hs);
                hsArrayDataBase = 0x20;
                hsLayoutReady = hsSlotsOff != 0;
                Logger.Log($"KnownTech HashSet picked slots@+{hsSlotsOff:X}, stride={hsValueStride}");
                return;
            }

            hsCountOff = ResolveFieldOffsetByNameOrPredicate(
                hsKlass, new[] { "m_count", "count" },
                (n, t) => n.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0);

            hsSlotsOff = ResolveFieldOffsetByNameOrPredicate(
                hsKlass, new[] { "m_slots", "slots" },
                (n, t) => n.IndexOf("slot", StringComparison.OrdinalIgnoreCase) >= 0);

            hsArrayDataBase = 0x20;
            hsValueStride = 12;
            hsValueOff = 8;

            hsLayoutReady = hsCountOff != 0 && hsSlotsOff != 0;
            Logger.Log($"KnownTech HashSet fixed: count@+{hsCountOff:X}, slots@+{hsSlotsOff:X}");
        }
        #endregion
    }
}
