using LiveSplit.ComponentUtil;
using LiveSplit.Model;
using LiveSplit.Options;
using LiveSplit.VoxSplitter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
        private enum GameVersion
        {
            Sept2018,
            Mar2023
        }

        private readonly TimerModel timerModel;
        LiveSplitState _state;
        private bool isReady = true;

        private bool startedTimerBefore = false;
        public bool isInMainMenu = false;
        private bool fakePortalLoading = false;
        private int tickCounter = 0;
        private bool pointersInitialized;
        private GameVersion gameVersion;

        private readonly Dictionary<SplitName, Func<bool>> splitConditions;
        private readonly HashSet<SplitName> alreadySplit = new HashSet<SplitName>();

        private readonly MonoHelper mono;
        private SubnauticaSettings settings;

        #region Pointer stuff
        private Pointer<bool> isIntroCinematicActive;
        private Pointer<bool> isAnimationPlaying;
        private Pointer<float> timeCured;
        private Pointer<float> health;
        private Pointer<IntPtr> mainMenu;
        private StringPointer biome;

        private List<TechType> playerInventory = new List<TechType>();
        private List<TechType> playerInventoryOld = new List<TechType>();

        private List<TechType> knownTech = new List<TechType>();
        private List<TechType> knownTechOld = new List<TechType>();

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

        IntPtr sgmMainPtr;
        int off_completedGoals;

        private MemoryWatcher<bool> isLoadingScreen = new MemoryWatcher<bool>(IntPtr.Zero);
        private MemoryWatcher<bool> isPortalLoading = new MemoryWatcher<bool>(IntPtr.Zero);
        private MemoryWatcher<bool> isEggsHatching = new MemoryWatcher<bool>(IntPtr.Zero);
        private MemoryWatcher<bool> isNotInWater = new MemoryWatcher<bool>(IntPtr.Zero);
        private MemoryWatcher<bool> isDying = new MemoryWatcher<bool>(IntPtr.Zero);
        private MemoryWatcher<int> isFabiOpen = new MemoryWatcher<int>(IntPtr.Zero); // 2 means that the esc menu is open
        private MemoryWatcher<int> isPDAOpen = new MemoryWatcher<int>(IntPtr.Zero); // true = 1051931443, false = 1056964608
        private MemoryWatcher<int> isRocketLaunching = new MemoryWatcher<int>(IntPtr.Zero); // 2018 = 1, 2023 = 256
        private MemoryWatcher<int> oxygen = new MemoryWatcher<int>(IntPtr.Zero);
        private MemoryWatcher<float> walkDir = new MemoryWatcher<float>(IntPtr.Zero);
        private MemoryWatcher<float> strafeDir = new MemoryWatcher<float>(IntPtr.Zero);
        private MemoryWatcher<float> posX = new MemoryWatcher<float>(IntPtr.Zero);
        private MemoryWatcher<float> posY = new MemoryWatcher<float>(IntPtr.Zero);
        private MemoryWatcher<float> posZ = new MemoryWatcher<float>(IntPtr.Zero);
        #endregion

        public SubnauticaMemory(LiveSplitState state, Logger logger, SubnauticaSettings settings) : base(state, logger)
        {
            SetProcessNames("Subnautica");
            _state = state;
            mono = new MonoHelper(this);
            this.settings = settings;
            timerModel = new TimerModel() { CurrentState = state };

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

        public override bool IsReady() => base.IsReady() && mono.IsCompleted;

        protected override void OnHook()
        {
            GetGameVersion();
            InitPointers();

            mono.Run(() =>
            {
                var ptrFactory = new MonoNestedPointerFactory(this, mono);
                #region Intro Cinematic
                Pointer<IntPtr> introCinematicPtr = ptrFactory.Make<IntPtr>("EscapePod", "main", "introCinematic");
                IntPtr pccKlass = mono.GetClass(mono.mainImage, "PlayerCinematicController");
                int off_cinematicModeActive = mono.GetFieldOffset(pccKlass, "cinematicModeActive");
                isIntroCinematicActive = ptrFactory.Make<bool>(introCinematicPtr, off_cinematicModeActive);
                #endregion
                #region Is Animation Playing
                isAnimationPlaying = ptrFactory.Make<bool>("Player", "main", "_cinematicModeActive");
                #endregion
                #region Completed Goals
                IntPtr sgmKlass = mono.GetClass(mono.mainImage, "StoryGoalManager");

                off_completedGoals = ResolveFieldOffsetByNameOrPredicate(
                    sgmKlass,
                    new[] { "completedGoals" },
                    (fname, ftype) => NameHas(fname, "completed") && NameHas(fname, "goal")
                );

                int off_mainBacking = ResolveFieldOffsetByNameOrPredicate(
                    sgmKlass,
                    new[] { "<main>k__BackingField" }, 
                    (fname, ftype) => NameHas(fname, "main") && NameHas(fname, "k__BackingField")
                );

                IntPtr sgmStaticKlass = mono.GetStaticAddress(sgmKlass);
                sgmMainPtr = IntPtr.Zero;
                while (sgmMainPtr == IntPtr.Zero)
                {
                    if (sgmStaticKlass == IntPtr.Zero || off_mainBacking == 0)
                        break; 

                    sgmMainPtr = Game.Read<IntPtr>(mono.GetStaticData(sgmStaticKlass) + off_mainBacking);
                    Thread.Sleep(50);
                }
                Logger.Log($"StoryGoalManager.main -> {sgmMainPtr:X}");
                #endregion              
                #region Time Cured
                timeCured = ptrFactory.Make<float>("Player", "main", "timePlayerInfectionCured");
                #endregion
                #region Health
                Pointer<IntPtr> liveMixingPtr = ptrFactory.Make<IntPtr>("Player", "main", "liveMixin");
                IntPtr lmKlass = mono.GetClass(mono.mainImage, "LiveMixin");
                int off_health = mono.GetFieldOffset(lmKlass, "health");
                health = ptrFactory.Make<float>(liveMixingPtr, off_health);
                #endregion
                #region Inventory
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
                ResolveItemsMapDataBase();
                #endregion
                #region Known Tech
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
                #endregion
                #region Main Menu
                mainMenu = ptrFactory.Make<IntPtr>("uGUI_MainMenu", "main");
                #endregion
                #region Biome
                biome = ptrFactory.MakeString("Player", "main", "biomeString", 0x14);
                #endregion
            });
        }
        public override bool Update()
        {
            if(!pointersInitialized)
                return isReady;

            #region Only update watchers when needed
            if (settings.introStart && gameVersion == GameVersion.Sept2018)
                oxygen.Update(Game);

            if (settings.creativeStart)
            {
                walkDir.Update(Game);
                strafeDir.Update(Game);
                isFabiOpen.Update(Game);
                isPDAOpen.Update(Game);
                isLoadingScreen.Update(Game);
            }

            if (Needs(SplitName.PortalSplit))
                isPortalLoading.Update(Game);

            if (Needs(SplitName.HatchSplit))
                isEggsHatching.Update(Game);

            if (Needs(SplitName.SGLBaseSplit, SplitName.SGLShallowsSplit))
                isNotInWater.Update(Game);

            if (Needs(SplitName.BaseDeathSplit,
                      SplitName.AuroraDeathSplit,
                      SplitName.IonDeathSplit,
                      SplitName.SparseDeathSplit,
                      SplitName.GunDeathSplit))
                isDying.Update(Game);

            if (Needs(SplitName.RocketSplit))
                isRocketLaunching.Update(Game);

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
                UpdateInventory();

            if (Needs(SplitName.BoostersSplit,
                      SplitName.FuelReservesSplit,
                      SplitName.RocketUnlockSplit,
                      SplitName.AuroraExitSplit,
                      SplitName.IonUnlockSplit))
                UpdateBlueprints();
            #endregion

            isInMainMenu = IsInMainMenu();
            if (isInMainMenu)
                startedTimerBefore = false;

            foreach (var i in playerInventory)
                Logger.Log(i.ToString());
            Logger.Log($"New={playerInventory.Count}, Old={playerInventoryOld.Count}");
            TryResetOnMainMenu();
            
            return isReady;
        }
        private bool Needs(params SplitName[] required) => required.Any(r => settings.Splits.Contains(r));
        private void UpdatePosition() { posX.Update(Game); posY.Update(Game); posZ.Update(Game); }

        public override bool Start()
        {
            if (startedTimerBefore)
                return false;

            if (settings.introStart)
            {
                if (gameVersion == GameVersion.Sept2018 && oxygen.Current == 45 && oxygen.Old < 45) { Logger.Log("Start of oxygen"); startedTimerBefore = true; return true; }
                if (!isIntroCinematicActive.New && isIntroCinematicActive.Old) { Logger.Log("Start of introCinematic"); startedTimerBefore = true; return true; }
            }
            if (settings.creativeStart && !isLoadingScreen.Current && !isInMainMenu)
            {
                // Start of Move
                if ((walkDir.Current != 0 && walkDir.Old == 0) || (strafeDir.Current != 0 && strafeDir.Old == 0)) { Logger.Log("Start of Move"); startedTimerBefore = true; return true; }

                // Start of Fabricator
                if (isFabiOpen.Current == 1 && isFabiOpen.Old == 0) { Logger.Log("Start of Fabricator"); startedTimerBefore = true; return true; }

                // Start of PDA
                if (isPDAOpen.Current == 1051931443 && isPDAOpen.Current != isPDAOpen.Old) { Logger.Log("Start of PDA"); startedTimerBefore = true; return true; }
            }
            return false;
        }

        public override bool Split()
        {
            if (!pointersInitialized)
                return false;

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

        public override void OnStart() => alreadySplit.Clear();
        public override bool Loading() => ShouldPause();
        public override void OnExit() => isReady = false;
        public override void Dispose() => mono.Dispose();

        #region World/Player Checks
        public bool IsInMainMenu() => posX.Current == 0 && posZ.Current == 0 && posY.Current == 1.75f;

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

        private bool ShouldPause()
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

        private void TryResetOnMainMenu()
        {
            if (!settings.reset)
                return;
            if (mainMenu.New == mainMenu.Old && mainMenu.New != IntPtr.Zero)
                return;
            if (_state.CurrentPhase == TimerPhase.NotRunning)
                return;

            Form ui = _state.Form;
            Action doReset = () =>
            {
                bool GoldSegment = false;
                for (int index = 0; index < _state.Run.Count; index++)
                {
                    if (LiveSplitStateHelper.CheckBestSegment(_state, index, _state.CurrentTimingMethod))
                    {
                        GoldSegment = true;
                        break;
                    }
                }

                bool save = true;
                if (settings.askForGoldSave && GoldSegment)
                {
                    DialogResult r = MessageBox.Show(
                        ui,
                        "Save splits before resetting?",
                        "Reset",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (r == DialogResult.Cancel)
                        return;

                    save = (r == DialogResult.Yes);
                }

                timerModel.Reset(save);
            };

            if (ui.InvokeRequired)
                ui.BeginInvoke(doReset);
            else
                doReset();
        }

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
            //Logger.Log($"KnownTech: hs={hs:X}, slotsOff=+0x{hsSlotsOff:X}, len={arrayLen}, unique={dedup.Count}");

            knownTechOld = knownTech;
            knownTech = dedup;
        }

        void UpdateCompletedGoals()
        {
            if (sgmMainPtr == IntPtr.Zero || off_completedGoals == 0) return;

            IntPtr hs = Game.Read<IntPtr>(sgmMainPtr + off_completedGoals);
            if (hs == IntPtr.Zero) return;

            ResolveHashSetLayoutUsingMono(hs);
            if (!hsLayoutReady) return;

            IntPtr slotsArr = Game.Read<IntPtr>(hs + hsSlotsOff);
            int arrayLen = Game.Read<int>(slotsArr + 0x18);
            IntPtr slotsData = slotsArr + hsArrayDataBase;
            int stride = hsValueStride, voff = hsValueOff;

            var goals = new List<string>();
            for (int i = 0; i < arrayLen; i++)
            {
                IntPtr strPtr = Game.Read<IntPtr>(slotsData + i * stride + voff);
                if (strPtr != IntPtr.Zero)
                {
                    string s = Game.ReadString(strPtr, EStringType.Auto);
                    if (!string.IsNullOrEmpty(s))
                        goals.Add(s);
                }
            }

            var completed = goals.Distinct().ToList();
            Logger.Log($"CompletedGoals ({completed.Count}): {string.Join(", ", completed)}");
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

        private void InitPointers()
        {
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

            Logger.Log("Pointers initialized");

            pointersInitialized = true;
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
            int[] candidates = { 0x10, 0x18 }; // buckets vs slots
            int bestOff = 0; int bestScore = -1;

            foreach (int off in candidates)
            {
                IntPtr arr = Game.Read<IntPtr>(hs + off);
                int len = Game.Read<int>(arr + 0x18);
                if (len <= 0 || len > 50000) continue;

                IntPtr data = arr + 0x20;
                // test Stride 12/16
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