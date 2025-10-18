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

        private Pointer<IntPtr> inventoryDictionaryPtr;
        private Dictionary<TechType, int> playerInventory = new Dictionary<TechType, int>();
        private Dictionary<TechType, int> playerInventoryOld = new Dictionary<TechType, int>();

        private Pointer<IntPtr> knownTechPtr;
        private List<TechType> knownTech = new List<TechType>();
        private List<TechType> knownTechOld = new List<TechType>();
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
                { SplitName.LeaveKelpForestSplit, () => !alreadySplit.Contains(SplitName.LeaveKelpForestSplit) && IsWithinBounds(teethBounds) && IsItemInInventory(playerInventory, TechType.CreepvinePiece) },
                { SplitName.FourToothSplit,       () => !alreadySplit.Contains(SplitName.FourToothSplit) && GetItemCount(playerInventory, TechType.StalkerTooth) == 4 && GetItemCount(playerInventory, TechType.StalkerTooth) != GetItemCount(playerInventoryOld, TechType.StalkerTooth) },
                { SplitName.AuroraDeathSplit,     () => !alreadySplit.Contains(SplitName.AuroraDeathSplit) && !alreadySplit.Contains(SplitName.AuroraBiomeSplit) && isDying.New && !isDying.Old && new[] { "crashedShip", "generatorRoom" }.Contains(biomeString)},
                { SplitName.RocketUnlockSplit,    () => knownTech.Contains(TechType.RocketBase) && !knownTechOld.Contains(TechType.RocketBase) },
                { SplitName.MountainDescendSplit, () => !alreadySplit.Contains(SplitName.MountainDescendSplit) && IsWithinBounds(mountainBounds) },
                { SplitName.IonDeathSplit,        () => isDying.New && !isDying.Old && new[] { "Precursor_LavaCastleBase", "PrecursorThermalRoom" }.Contains(biomeString) },
                { SplitName.GunDeathSplit,        () => isDying.New && !isDying.Old && biomeString == "Precursor_Gun_ControlRoom" },
                { SplitName.SparseDeathSplit,     () => !alreadySplit.Contains(SplitName.SparseDeathSplit) && !alreadySplit.Contains(SplitName.SparseBiomeSplit) && isDying.New && !isDying.Old && new[] { "sparseReef", "seaTreaderPath", "seaTreaderPath_wreck" }.Contains(biomeString) },
                { SplitName.SGLBaseSplit,         () => !alreadySplit.Contains(SplitName.SGLBaseSplit) && isNotInWater.New && !isNotInWater.Old && IsWithinBounds(SGLBaseBounds) },
                { SplitName.SGLShallowsSplit,     () => !alreadySplit.Contains(SplitName.SGLShallowsSplit) && !isNotInWater.New && isAnimationPlaying.New && IsWithinBounds(SGLBaseBounds) && IsItemInInventory(playerInventory, TechType.DoubleTank) },
                { SplitName.UpperTabletSplit,     () => GetItemCount(playerInventory, TechType.PrecursorKey_Purple) > GetItemCount(playerInventoryOld, TechType.PrecursorKey_Purple) && IsWithinBounds(upperTabletBounds) },
                { SplitName.IonUnstuckSplit,      () => isAnimationPlaying.New && !isAnimationPlaying.Old && biomeString == "PrecursorThermalRoom" },
                { SplitName.PCFPoolSplit,         () => !alreadySplit.Contains(SplitName.PCFPoolSplit) && biomeString == "Prison_Aquarium_Upper" && biomeStringOld == "Prison_Moonpool" },
                { SplitName.SparseBiomeSplit,     () => !alreadySplit.Contains(SplitName.SparseBiomeSplit) && !alreadySplit.Contains(SplitName.SparseDeathSplit) && new[] { "sparseReef", "seaTreaderPath", "seaTreaderPath_wreck" }.Contains(biomeStringOld) && new[] { "safeShallows", "kelpForest", "Lifepod" }.Contains(biomeString) },
                { SplitName.AuroraBiomeSplit,     () => !alreadySplit.Contains(SplitName.AuroraBiomeSplit) && !alreadySplit.Contains(SplitName.AuroraDeathSplit) && new[] { "crashedShip", "generatorRoom" }.Contains(biomeStringOld) && new[] { "safeShallows", "kelpForest", "Lifepod" }.Contains(biomeString) },
                { SplitName.EyestalkSplit,        () => !alreadySplit.Contains(SplitName.EyestalkSplit) && IsItemInInventory(playerInventory, TechType.EyesPlantSeed) && !IsItemInInventory(playerInventoryOld, TechType.EyesPlantSeed) },
                { SplitName.IonUnlockSplit,       () => knownTech.Contains(TechType.PrecursorIonBattery) && !knownTechOld.Contains(TechType.PrecursorIonBattery) },
                { SplitName.AuroraExitSplit,      () => !alreadySplit.Contains(SplitName.AuroraExitSplit) && IsWithinBounds(auroraExitBounds) && knownTech.Contains(TechType.RocketBase) },
                { SplitName.HCGSparseSplit,       () => !alreadySplit.Contains(SplitName.HCGSparseSplit) && isAnimationPlaying.New && !isAnimationPlaying.Old && (IsWithinBounds(enterClipABounds) || IsWithinBounds(enterClipCBounds)) && IsItemInInventory(playerInventory, TechType.AluminumOxide) },
                { SplitName.DeathSplit,           () => isDying.New && !isDying.Old },
            };
        }

        public override bool IsReady() => base.IsReady() && mono.IsCompleted;
        Pointer<IntPtr> itemsMapPtr;   // nur für initiales container-lesen genutzt, optional
        Pointer<int> sizeXPtr, sizeYPtr;

        IntPtr iiKlass;
        IntPtr puKlass;

        // gecachte Offsets
        int off_container, off_itemsMap, off_sizeX, off_sizeY;
        int off_ii_techType, off_ii_item;
        int off_pu_overrideUsed, off_pu_overrideTechType;

        // gecachter Static-Klass + Offset für Inventory.main
        IntPtr invStaticKlass;
        int invStaticOffset;

        protected override void OnHook()
        {
            GetGameVersion();

            mono.Run(() =>
            {
                var ptrFactory = new MonoNestedPointerFactory(this, mono);

                var invKlass = mono.GetClass(mono.mainImage, "Inventory");
                var icKlass = mono.GetClass(mono.mainImage, "ItemsContainer");
                iiKlass = mono.GetClass(mono.mainImage, "InventoryItem");
                puKlass = mono.GetClass(mono.mainImage, "Pickupable");

                // Warte bis Inventory.main != 0
                invStaticKlass = mono.GetStaticField(
                    mono.mainImage,
                    "Inventory",
                    "main",
                    out _,
                    out invStaticOffset
                );

                IntPtr invMainPtr = IntPtr.Zero;
                while (invMainPtr == IntPtr.Zero)
                {
                    invMainPtr = Game.Read<IntPtr>(mono.GetStaticData(invStaticKlass) + invStaticOffset);
                    Thread.Sleep(100);
                }
                Logger.Log($"Inventory.main -> {invMainPtr:X}");

                // Offsets holen
                off_container = mono.GetFieldOffset(invKlass, "_container");
                off_itemsMap = mono.GetFieldOffset(icKlass, "itemsMap");
                off_sizeX = mono.GetFieldOffset(icKlass, "<sizeX>k__BackingField");
                off_sizeY = mono.GetFieldOffset(icKlass, "<sizeY>k__BackingField");

                // InventoryItem-Felder (aus deinem Dump)
                //  - <item>k__BackingField @ 0x10
                //  - _techType            @ 0x24
                off_ii_item = mono.GetFieldOffset(iiKlass, "<item>k__BackingField");
                if (off_ii_item == 0) off_ii_item = 0x10; // Fallback für deinen Build
                off_ii_techType = mono.GetFieldOffset(iiKlass, "_techType");
                if (off_ii_techType == 0) off_ii_techType = 0x24; // Fallback

                // Pickupable-Felder (aus deinem Log)
                off_pu_overrideUsed = mono.GetFieldOffset(puKlass, "overrideTechUsed");   // 0x64
                off_pu_overrideTechType = mono.GetFieldOffset(puKlass, "overrideTechType");   // 0x60

                // Einmaliger Test-Read & Log
                // container muss zusätzlich dereferenziert werden
                IntPtr invMain = Game.Read<IntPtr>(mono.GetStaticData(invStaticKlass) + invStaticOffset);
                IntPtr container = Game.Read<IntPtr>(invMain + off_container);
                IntPtr itemsMap = Game.Read<IntPtr>(container + off_itemsMap);
                int sizeX = Game.Read<int>(container + off_sizeX);
                int sizeY = Game.Read<int>(container + off_sizeY);

                Logger.Log($"Inventory.main={invMain:X}, container={container:X}, itemsMap={itemsMap:X}, size={sizeX}x{sizeY}");
                Logger.Log($"off_ii_item={off_ii_item:X}, off_ii_techType={off_ii_techType:X}, off_pu_overrideUsed={off_pu_overrideUsed:X}, off_pu_overrideTechType={off_pu_overrideTechType:X}");
                Logger.Log($"Setup done. Inventory.main = {invMainPtr:X}");
            });
        }

        public override bool Update()
        {
            // 1) Frisch dereferenzieren (kein ptrFactory nötig)
            IntPtr invMain = Game.Read<IntPtr>(mono.GetStaticData(invStaticKlass) + invStaticOffset);
            if (invMain == IntPtr.Zero) return isReady;

            IntPtr container = Game.Read<IntPtr>(invMain + off_container);
            if (container == IntPtr.Zero) return isReady;

            IntPtr pArr = Game.Read<IntPtr>(container + off_itemsMap);
            int sizeX = Game.Read<int>(container + off_sizeX);
            int sizeY = Game.Read<int>(container + off_sizeY);

            // 2) Helper: InventoryItem* an (x,y)
            IntPtr GetInventoryItemAt(int x, int y)
            {
                if (pArr == IntPtr.Zero) return IntPtr.Zero;
                if ((uint)x >= (uint)sizeX || (uint)y >= (uint)sizeY) return IntPtr.Zero;
                int index = y * sizeX + x;
                // 2D-Array-Layout (Unity): Header ~0x20, danach row-major Referenzen
                int elemOffset = 0x20 + index * mono.MonoInfo.pointer_size;
                return Game.Read<IntPtr>(pArr + elemOffset);
            }

            // 3) TechType sauber lesen
            int GetTechTypeAt(int x, int y)
            {
                IntPtr pInvItem = GetInventoryItemAt(x, y);
                if (pInvItem == IntPtr.Zero)
                    return (int)TechType.None;

                // a) bevorzugt: InventoryItem._techType (@0x24)
                int cached = Game.Read<int>(pInvItem + off_ii_techType);
                if (cached != (int)TechType.None)
                    return cached;

                // b) Fallback: Pickupable.override***, aber nur wenn override aktiv
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

            //4) Beispiel: ein paar Slots prüfen(Debug)
             // => Du kannst hier natürlich dein komplettes Grid dumpen
            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    int tt = GetTechTypeAt(x, y);
                    if (tt != (int)TechType.None)
                        Logger.Log($"Slot [{x},{y}] = {(TechType)tt}");
                }
            }

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


        private void UpdateInventory()
        {
            var inv = new Dictionary<TechType, int>();

            // Offsets (wie gehabt)
            int sizeOffset = gameVersion == GameVersion.Sept2018 ? 0xBBD4 : 0x94;
            int startOffset = gameVersion == GameVersion.Sept2018 ? 0x20 : 0x9C;
            int itemStride = gameVersion == GameVersion.Sept2018 ? 0x8 : 0x94;

            const int itemTypeOff = 0x18; // TechType (int)
            const int listPtrOff = 0x10; // pointer auf List<>
            const int listCountOff = 0x18; // Count in List<>

            // 1) Was liefert deine Pointer-Chain wirklich?
            //    a) direkt Objektbasis?
            IntPtr candidateA = inventoryDictionaryPtr.New;                               // ggf. schon das Objekt
            int sizeA = Game.Read<int>(candidateA + sizeOffset);

            //    b) oder erst noch auf das Objekt zeigen (eine Ebene tiefer)?
            IntPtr candidateB = Game.Read<IntPtr>(inventoryDictionaryPtr.New);     // explizite Extra-Deref
            int sizeB = Game.Read<int>(candidateB + sizeOffset);

            // 2) Nimm die plausible Variante (Verifikation über Größe)
            IntPtr startAddr;
            int size;
            if (sizeA > 0 && sizeA < 4096)
            {
                startAddr = candidateA;
                size = sizeA;
            }
            else
            {
                startAddr = candidateB;
                size = sizeB;
            }

            // 3) Items lesen (unverändert zur Logik von dir)
            for (int i = 0; i < size; i++)
            {
                IntPtr itemGroup = Game.Read<IntPtr>(startAddr + startOffset + (itemStride * i));
                if (itemGroup == IntPtr.Zero) continue;

                TechType itemType = (TechType)Game.Read<int>(itemGroup + itemTypeOff);
                IntPtr list = Game.Read<IntPtr>(itemGroup + listPtrOff);
                int itemCount = Game.Read<int>(list + listCountOff);

                if (!inv.ContainsKey(itemType))
                    inv.Add(itemType, itemCount);
            }

            playerInventoryOld = playerInventory;
            playerInventory = inv;
        }

        private bool IsItemInInventory(Dictionary<TechType, int> inv, TechType techtype, int? count = null)
        {
            if (!inv.TryGetValue(techtype, out int New))
                return false;
            return count == null || New >= count.Value;
        }
        private int GetItemCount(Dictionary<TechType, int> inv, TechType techtype)
        {
            if (!inv.TryGetValue(techtype, out int New))
                return 0;
            return New;
        }

        private void UpdateBlueprints()
        {
            List<TechType> blueprints = new List<TechType>();

            IntPtr startAddr = knownTechPtr.New;

            int slotsOffset = gameVersion == GameVersion.Sept2018 ? 0x20 : 0x108;
            IntPtr slots = Game.ReadPointer(startAddr + slotsOffset);
            int countOffset = gameVersion == GameVersion.Sept2018 ? 0x40 : 0x124;
            int count = Game.ReadValue<int>(startAddr + countOffset);


            int slotBeginningOffset = gameVersion == GameVersion.Sept2018 ? 0x20 : 0x20;
            int slotSize = gameVersion == GameVersion.Sept2018 ? 0x4 : 0xC;

            for (int i = 0; i < count; i++)
            {
                int tech = Game.ReadValue<int>(slots + slotBeginningOffset + slotSize * i);
                if (tech > 0 && tech < 10005)
                {
                    //WriteDebug(((TechType)tech).ToString());
                    TechType type = (TechType)tech;
                    blueprints.Add(type);
                }
            }
            knownTechOld = knownTech;
            knownTech = blueprints;
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
    }
}
