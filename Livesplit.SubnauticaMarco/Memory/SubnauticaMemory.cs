using LiveSplit.SubnauticaTracker.Memory;
using LiveSplit.SubnauticaTracker.Versions;
using System;
using System.Diagnostics;

namespace LiveSplit.SubnauticaMarco.Memory
{
    internal enum GameMemoryState
    {
        NoProcess,
        Initializing,
        MainMenu,
        InGame,
        Loading,
        SkipCutscene,
        InGamePauseMenuOpen,
        InGameCraftingMenuOpen,
        Unknown
    }

    internal sealed class SubnauticaMemory : IDisposable
    {
        private readonly object sync = new object();
        private DateTime nextProcessSearchUtc;
        private DateTime nextInitializeUtc;
        private ProcessMemory memory;
        private MonoRuntime runtime;
        private GameVersionInfo version;

        private ManagedStaticField mainMenu;
        private ManagedStaticField playerMain;
        private ManagedStaticField uGuiMain;
        private ManagedStaticField inputModuleCurrent;
        private ManagedStaticField fpsInputModuleCurrent;
        private ManagedStaticField ingameMenuMain;
        private ManagedStaticField escapePodMain;
        private ManagedStaticField exploderMain;

        private ManagedField mainMenuSubMenu;
        private ManagedField mainMenuPrimaryOptions;
        private ManagedField inputModuleGrid;
        private ManagedField uGuiLoading;
        private ManagedField uGuiIntro;
        private ManagedField uGuiCraftingMenu;
        private ManagedField sceneLoadingFlag;
        private ManagedField sceneIntroCoroutine;
        private ManagedField sceneIntroMainText;
        private ManagedField textFadeSequence;
        private ManagedField sequenceProgress;
        private ManagedField sequenceTarget;
        private ManagedField fpsInputModuleLastGroup;
        private ManagedField escapePodIntroCinematic;
        private ManagedField escapePodAnchorPosition;
        private ManagedField cinematicModeActive;
        private ManagedField ingameMenuCurrentScreen;
        private ManagedField ingameMenuIsQuitting;
        private ManagedField explosionCountdown;
        private ManagedField explosionWarning;
        private ManagedField unityObjectCachedPointer;

        public string VersionName
        {
            get
            {
                lock (sync)
                    return version == null ? string.Empty : version.DisplayName;
            }
        }

        public void Update()
        {
            lock (sync)
            {
                if (memory == null || !memory.IsAlive)
                {
                    Detach();
                    if (DateTime.UtcNow < nextProcessSearchUtc)
                        return;

                    nextProcessSearchUtc = DateTime.UtcNow.AddMilliseconds(500);
                    Attach();
                }

                if (memory != null && runtime == null && DateTime.UtcNow >= nextInitializeUtc)
                {
                    nextInitializeUtc = DateTime.UtcNow.AddSeconds(3);
                    InitializeRuntime();
                }
                else if (runtime != null && DateTime.UtcNow >= nextInitializeUtc)
                {
                    nextInitializeUtc = DateTime.UtcNow.AddSeconds(3);
                    ResolveFields();
                }
            }
        }

        public GameMemoryState GetState()
        {
            lock (sync)
            {
                if (memory == null || !memory.IsAlive)
                    return GameMemoryState.NoProcess;
                if (runtime == null)
                    return GameMemoryState.Initializing;

                bool loading;
                if (TryReadLoadingCore(out loading) && loading)
                    return GameMemoryState.Loading;

                bool introActive;
                bool introCanBeSkipped;
                if (TryReadIntroStateCore(out introActive, out introCanBeSkipped) && introActive)
                {
                    return introCanBeSkipped
                        ? GameMemoryState.SkipCutscene
                        : GameMemoryState.Loading;
                }

                IntPtr value;
                // Match the launcher's state flow exactly: main menu first, then
                // Player.main, with Unity's m_CachedPtr filtering destroyed
                // managed wrappers before either signal is accepted.
                if (TryReadLiveStaticObject(mainMenu, out value) && value != IntPtr.Zero)
                    return GameMemoryState.MainMenu;
                if (TryReadLiveStaticObject(playerMain, out value) && value != IntPtr.Zero)
                    return GetInGameStateCore();
                return GameMemoryState.Unknown;
            }
        }

        public GameMemoryState GetResetState()
        {
            return GetState();
        }

        public bool TryReadLivePlayer(out IntPtr player)
        {
            lock (sync)
                return TryReadLiveStaticObject(playerMain, out player);
        }

        public bool TryGetProcess(out Process process, out SubnauticaVersion detectedVersion)
        {
            lock (sync)
            {
                process = null;
                detectedVersion = SubnauticaVersion.Unknown;
                if (memory == null || !memory.IsAlive || version == null)
                    return false;

                try
                {
                    process = Process.GetProcessById(memory.Process.Id);
                    detectedVersion = version.Version;
                    return true;
                }
                catch
                {
                    if (process != null)
                        process.Dispose();
                    process = null;
                    return false;
                }
            }
        }

        public bool TryReadMainMenuReady(out bool ready)
        {
            lock (sync)
            {
                ready = false;
                if (runtime == null || mainMenu == null)
                    return false;

                IntPtr menu;
                if (!TryReadLiveStaticObject(mainMenu, out menu))
                    return false;
                if (menu == IntPtr.Zero)
                    return true;

                bool loading;
                if (TryReadLoadingCore(out loading) && loading)
                    return true;

                IntPtr subMenu;
                IntPtr primaryOptions;
                if (!TryReadObject(menu, mainMenuSubMenu, out subMenu)
                    || !TryReadObject(menu, mainMenuPrimaryOptions, out primaryOptions))
                {
                    return false;
                }

                if (subMenu == IntPtr.Zero || primaryOptions == IntPtr.Zero)
                {
                    ready = true;
                    return true;
                }

                if (inputModuleCurrent != null && inputModuleGrid != null)
                {
                    IntPtr inputModule;
                    IntPtr currentGrid;
                    if (!TryReadLiveStaticObject(inputModuleCurrent, out inputModule)
                        || inputModule == IntPtr.Zero
                        || !TryReadObject(inputModule, inputModuleGrid, out currentGrid))
                    {
                        return false;
                    }

                    ready = currentGrid == subMenu || currentGrid == primaryOptions;
                    return true;
                }

                ready = true;
                return true;
            }
        }

        public bool TryReadLoading(out bool loading)
        {
            lock (sync)
                return TryReadLoadingCore(out loading);
        }

        public bool TryReadIntroReady(out bool ready)
        {
            lock (sync)
                return TryReadIntroReadyCore(out ready);
        }

        public bool TryReadQuitConfirmation(out bool unsavedWarning)
        {
            lock (sync)
            {
                unsavedWarning = false;
                if (runtime == null || ingameMenuMain == null
                    || ingameMenuCurrentScreen == null)
                    return false;

                IntPtr menu;
                IntPtr currentScreen;
                if (!TryReadLiveStaticObject(ingameMenuMain, out menu) || menu == IntPtr.Zero
                    || !TryReadObject(menu, ingameMenuCurrentScreen, out currentScreen)
                    || currentScreen == IntPtr.Zero)
                {
                    return false;
                }

                string screenName;
                if (!TryReadUnityObjectName(currentScreen, out screenName))
                    return false;

                if (screenName == "QuitConfirmationWithSaveWarning")
                {
                    unsavedWarning = true;
                    return true;
                }

                return screenName == "QuitConfirmation";
            }
        }

        public bool TryReadQuitStarted(out bool started)
        {
            lock (sync)
            {
                started = false;
                if (ingameMenuMain == null || ingameMenuIsQuitting == null)
                    return false;

                IntPtr menu;
                byte value;
                if (!TryReadLiveStaticObject(ingameMenuMain, out menu) || menu == IntPtr.Zero
                    || !memory.TryReadByte(
                        ProcessMemory.Add(menu, ingameMenuIsQuitting.Offset),
                        out value))
                {
                    return false;
                }

                started = value != 0;
                return true;
            }
        }

        public bool TryReadExplosionTime(out float seconds)
        {
            lock (sync)
            {
                seconds = -1f;
                if (runtime == null || exploderMain == null
                    || explosionCountdown == null || explosionWarning == null)
                {
                    return false;
                }

                IntPtr exploder;
                float countdown;
                float warning;
                if (!exploderMain.TryReadPointer(out exploder) || exploder == IntPtr.Zero
                    || !memory.TryReadSingle(ProcessMemory.Add(exploder, explosionCountdown.Offset), out countdown)
                    || !memory.TryReadSingle(ProcessMemory.Add(exploder, explosionWarning.Offset), out warning)
                    || countdown <= 0f || warning <= 0f)
                {
                    return false;
                }

                seconds = countdown - warning;
                return seconds >= 0f;
            }
        }

        public bool TryReadEscapePodSpawn(out float x, out float z)
        {
            lock (sync)
            {
                x = 0f;
                z = 0f;
                if (runtime == null || escapePodMain == null
                    || escapePodAnchorPosition == null)
                {
                    return false;
                }

                // EscapePod.StartAtPosition stores RandomStart's finalized
                // lifepod position in this Vector3 for the lifetime of the save.
                IntPtr escapePod;
                if (!TryReadLiveStaticObject(escapePodMain, out escapePod)
                    || escapePod == IntPtr.Zero
                    || !memory.TryReadSingle(
                        ProcessMemory.Add(escapePod, escapePodAnchorPosition.Offset),
                        out x)
                    || !memory.TryReadSingle(
                        ProcessMemory.Add(escapePod, escapePodAnchorPosition.Offset + 8),
                        out z))
                {
                    return false;
                }

                return !float.IsNaN(x) && !float.IsInfinity(x)
                    && !float.IsNaN(z) && !float.IsInfinity(z);
            }
        }

        public void Dispose()
        {
            lock (sync)
                Detach();
        }

        private void Attach()
        {
            Process[] processes = Process.GetProcessesByName("Subnautica");
            if (processes.Length == 0)
                return;

            Process selected = processes[0];
            for (int i = 1; i < processes.Length; i++)
                processes[i].Dispose();

            try
            {
                version = GameVersionDetector.Detect(selected);
                memory = new ProcessMemory(selected);
                nextInitializeUtc = DateTime.MinValue;
            }
            catch
            {
                selected.Dispose();
                memory = null;
                version = null;
            }
        }

        private void InitializeRuntime()
        {
            var candidate = new MonoRuntime(memory);
            if (!candidate.TryInitialize())
                return;

            runtime = candidate;
            ResolveFields();
        }

        private void ResolveFields()
        {
            ResolveStaticIfMissing("uGUI_MainMenu", new[] { "main", "_main", "s_main", "<main>k__BackingField" }, ref mainMenu);
            ResolveStaticIfMissing("Player", new[] { "main", "_main", "s_main", "<main>k__BackingField" }, ref playerMain);
            ResolveStaticIfMissing("uGUI", new[] { "main", "_main", "s_main", "<main>k__BackingField" }, ref uGuiMain);
            ResolveStaticIfMissing("GamepadInputModule", new[] { "current", "_current", "s_current", "<current>k__BackingField" }, ref inputModuleCurrent);
            ResolveStaticIfMissing("FPSInputModule", new[] { "current", "_current", "s_current", "<current>k__BackingField" }, ref fpsInputModuleCurrent);
            ResolveStaticIfMissing("IngameMenu", new[] { "main", "_main", "s_main", "<main>k__BackingField" }, ref ingameMenuMain);
            ResolveStaticIfMissing("EscapePod", new[] { "main", "_main", "s_main", "<main>k__BackingField" }, ref escapePodMain);
            ResolveStaticIfMissing("CrashedShipExploder", new[] { "main", "_main", "s_main", "<main>k__BackingField" }, ref exploderMain);

            ResolveInstanceIfMissing("uGUI_MainMenu", new[] { "subMenu", "_subMenu", "m_subMenu" }, ref mainMenuSubMenu);
            ResolveInstanceIfMissing("uGUI_MainMenu", new[] { "primaryOptions", "_primaryOptions", "m_primaryOptions" }, ref mainMenuPrimaryOptions);
            ResolveInstanceIfMissing("GamepadInputModule", new[] { "currentGrid", "_currentGrid", "m_currentGrid" }, ref inputModuleGrid);
            ResolveInstanceIfMissing("uGUI", new[] { "loading", "_loading", "m_loading" }, ref uGuiLoading);
            ResolveInstanceIfMissing("uGUI", new[] { "intro", "_intro", "m_intro" }, ref uGuiIntro);
            ResolveInstanceIfMissing("uGUI", new[] { "craftingMenu", "_craftingMenu", "m_craftingMenu" }, ref uGuiCraftingMenu);
            ResolveInstanceIfMissing("uGUI_SceneLoading", new[] { "isLoading", "_isLoading", "m_isLoading" }, ref sceneLoadingFlag);
            ResolveInstanceIfMissing("uGUI_SceneIntro", new[] { "coroutine", "_coroutine", "m_coroutine" }, ref sceneIntroCoroutine);
            ResolveInstanceIfMissing("uGUI_SceneIntro", new[] { "mainText", "_mainText", "m_mainText" }, ref sceneIntroMainText);
            ResolveInstanceIfMissing("uGUI_TextFade", new[] { "sequence", "_sequence", "m_sequence" }, ref textFadeSequence);
            ResolveInstanceIfMissing("Sequence", new[] { "_t", "t", "m_t" }, ref sequenceProgress);
            ResolveInstanceIfMissing("Sequence", new[] { "_target", "target", "m_target" }, ref sequenceTarget);
            ResolveInstanceIfMissing("FPSInputModule", new[] { "lastGroup", "_lastGroup", "m_lastGroup", "<lastGroup>k__BackingField" }, ref fpsInputModuleLastGroup);
            ResolveInstanceIfMissing("EscapePod", new[] { "introCinematic", "_introCinematic", "m_introCinematic" }, ref escapePodIntroCinematic);
            ResolveInstanceIfMissing("EscapePod", new[] { "anchorPosition", "_anchorPosition", "m_anchorPosition" }, ref escapePodAnchorPosition);
            ResolveInstanceIfMissing("PlayerCinematicController", new[] { "cinematicModeActive", "_cinematicModeActive", "m_cinematicModeActive" }, ref cinematicModeActive);
            ResolveInstanceIfMissing("IngameMenu", new[] { "<currentScreen>k__BackingField", "currentScreen", "_currentScreen", "m_currentScreen" }, ref ingameMenuCurrentScreen);
            ResolveInstanceIfMissing("IngameMenu", new[] { "isQuitting", "_isQuitting", "m_isQuitting", "<isQuitting>k__BackingField" }, ref ingameMenuIsQuitting);
            ResolveInstanceIfMissing("CrashedShipExploder", new[] { "timeToStartCountdown" }, ref explosionCountdown);
            ResolveInstanceIfMissing("CrashedShipExploder", new[] { "timeToStartWarning" }, ref explosionWarning);
        }

        private bool TryReadLoadingCore(out bool loading)
        {
            loading = false;
            if (runtime == null || uGuiMain == null || uGuiLoading == null || sceneLoadingFlag == null)
                return false;

            IntPtr gui;
            IntPtr sceneLoading;
            byte value;
            if (!uGuiMain.TryReadPointer(out gui) || gui == IntPtr.Zero
                || !TryReadObject(gui, uGuiLoading, out sceneLoading) || sceneLoading == IntPtr.Zero
                || !memory.TryReadByte(ProcessMemory.Add(sceneLoading, sceneLoadingFlag.Offset), out value))
            {
                return false;
            }

            loading = value != 0;
            return true;
        }

        private GameMemoryState GetInGameStateCore()
        {
            if (fpsInputModuleCurrent == null || fpsInputModuleLastGroup == null)
                return GameMemoryState.InGame;

            IntPtr inputModule;
            IntPtr activeGroup;
            if (!TryReadLiveStaticObject(fpsInputModuleCurrent, out inputModule)
                || inputModule == IntPtr.Zero
                || !TryReadObject(inputModule, fpsInputModuleLastGroup, out activeGroup)
                || activeGroup == IntPtr.Zero)
            {
                return GameMemoryState.InGame;
            }

            IntPtr pauseMenu;
            if (TryReadLiveStaticObject(ingameMenuMain, out pauseMenu)
                && pauseMenu != IntPtr.Zero && activeGroup == pauseMenu)
            {
                return GameMemoryState.InGamePauseMenuOpen;
            }

            IntPtr gui;
            IntPtr craftingMenu;
            if (uGuiCraftingMenu != null
                && TryReadLiveStaticObject(uGuiMain, out gui) && gui != IntPtr.Zero
                && TryReadObject(gui, uGuiCraftingMenu, out craftingMenu)
                && craftingMenu != IntPtr.Zero && activeGroup == craftingMenu)
            {
                return GameMemoryState.InGameCraftingMenuOpen;
            }

            // Only the crafting menu needs to be closed before Escape can open
            // the pause menu. PDA and other in-game UI allow Pause directly.
            return GameMemoryState.InGame;
        }

        private bool TryReadIntroStateCore(out bool active, out bool canBeSkipped)
        {
            active = false;
            canBeSkipped = false;
            if (uGuiMain == null || uGuiIntro == null || sceneIntroCoroutine == null)
                return false;

            IntPtr gui;
            IntPtr intro;
            IntPtr coroutine;
            if (!TryReadLiveStaticObject(uGuiMain, out gui) || gui == IntPtr.Zero
                || !TryReadObject(gui, uGuiIntro, out intro) || intro == IntPtr.Zero
                || !TryReadObject(intro, sceneIntroCoroutine, out coroutine))
            {
                return false;
            }

            active = coroutine != IntPtr.Zero;
            if (!active)
                return true;

            bool promptReady;
            if (TryReadIntroReadyCore(out promptReady) && promptReady)
            {
                canBeSkipped = true;
                return true;
            }

            IntPtr escapePod;
            IntPtr introCinematic;
            byte cinematicActive;
            if (escapePodMain != null && escapePodIntroCinematic != null
                && cinematicModeActive != null
                && TryReadLiveStaticObject(escapePodMain, out escapePod)
                && escapePod != IntPtr.Zero
                && TryReadObject(escapePod, escapePodIntroCinematic, out introCinematic)
                && introCinematic != IntPtr.Zero
                && memory.TryReadByte(
                    ProcessMemory.Add(introCinematic, cinematicModeActive.Offset),
                    out cinematicActive))
            {
                canBeSkipped = cinematicActive != 0;
            }

            return true;
        }

        private bool TryReadIntroReadyCore(out bool ready)
        {
            ready = false;
            if (runtime == null || uGuiMain == null || uGuiIntro == null
                || sceneIntroMainText == null || textFadeSequence == null
                || sequenceProgress == null || sequenceTarget == null)
            {
                return false;
            }

            IntPtr gui;
            IntPtr intro;
            IntPtr mainText;
            IntPtr sequence;
            float progress;
            byte target;
            if (!TryReadLiveStaticObject(uGuiMain, out gui) || gui == IntPtr.Zero
                || !TryReadObject(gui, uGuiIntro, out intro) || intro == IntPtr.Zero
                || !TryReadObject(intro, sceneIntroMainText, out mainText) || mainText == IntPtr.Zero
                || !TryReadObject(mainText, textFadeSequence, out sequence) || sequence == IntPtr.Zero
                || !memory.TryReadSingle(ProcessMemory.Add(sequence, sequenceProgress.Offset), out progress)
                || !memory.TryReadByte(ProcessMemory.Add(sequence, sequenceTarget.Offset), out target))
            {
                return false;
            }

            // IntroSequence displays PressAnyButton and immediately calls
            // mainText.SetState(true), which forces Sequence._t to 1 with
            // _target=true. It cannot reach this state earlier in a new load.
            ready = target != 0 && progress >= 0.999f;
            return true;
        }

        private bool TryReadObject(IntPtr instance, ManagedField field, out IntPtr value)
        {
            value = IntPtr.Zero;
            return field != null
                && memory.TryReadPointer(ProcessMemory.Add(instance, field.Offset), out value);
        }

        private bool TryReadUnityObjectName(IntPtr managedObject, out string name)
        {
            name = string.Empty;
            if (managedObject == IntPtr.Zero)
                return false;

            if (unityObjectCachedPointer == null)
            {
                IntPtr objectClass;
                ManagedField cachedPointer;
                if (!runtime.TryGetObjectClass(managedObject, out objectClass)
                    || !runtime.TryFindFieldAny(
                        objectClass,
                        new[] { "m_CachedPtr", "m_cachedPtr", "cachedPtr" },
                        out cachedPointer)
                    || cachedPointer.IsStatic)
                {
                    return false;
                }

                unityObjectCachedPointer = cachedPointer;
            }

            IntPtr nativeObject;
            byte[] nativeBytes;
            if (!memory.TryReadPointer(
                    ProcessMemory.Add(managedObject, unityObjectCachedPointer.Offset),
                    out nativeObject)
                || nativeObject == IntPtr.Zero
                || !memory.TryReadBytes(nativeObject, 0x100, out nativeBytes))
            {
                return false;
            }

            string[] confirmationNames =
            {
                "Main",
                "QuitConfirmation",
                "QuitConfirmationWithSaveWarning"
            };

            for (int offset = 0; offset <= nativeBytes.Length - memory.PointerSize; offset += memory.PointerSize)
            {
                long rawPointer = BitConverter.ToInt64(nativeBytes, offset);
                if (rawPointer < 0x10000 || rawPointer > 0x00007FFFFFFFFFFF)
                    continue;

                string candidate = memory.ReadUtf8String(new IntPtr(rawPointer), 40);
                foreach (string confirmationName in confirmationNames)
                {
                    if (candidate == confirmationName)
                    {
                        name = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryReadLiveStaticObject(ManagedStaticField staticField, out IntPtr value)
        {
            value = IntPtr.Zero;
            if (staticField == null)
                return false;

            IntPtr rawValue;
            if (!staticField.TryReadPointer(out rawValue))
                return false;
            if (rawValue == IntPtr.Zero)
                return true;

            if (unityObjectCachedPointer == null)
            {
                IntPtr objectClass;
                ManagedField cachedPointer;
                if (runtime.TryGetObjectClass(rawValue, out objectClass)
                    && runtime.TryFindFieldAny(
                        objectClass,
                        new[] { "m_CachedPtr", "m_cachedPtr", "cachedPtr" },
                        out cachedPointer)
                    && !cachedPointer.IsStatic)
                {
                    unityObjectCachedPointer = cachedPointer;
                }
            }

            // This is the same fallback used by the launcher when Unity's base
            // Object metadata is unavailable on a compatible nearby build.
            if (unityObjectCachedPointer == null)
            {
                value = rawValue;
                return true;
            }

            IntPtr cachedNativeObject;
            if (!memory.TryReadPointer(
                    ProcessMemory.Add(rawValue, unityObjectCachedPointer.Offset),
                    out cachedNativeObject))
            {
                return false;
            }

            value = cachedNativeObject != IntPtr.Zero ? rawValue : IntPtr.Zero;
            return true;
        }

        private void ResolveStaticIfMissing(
            string className,
            string[] names,
            ref ManagedStaticField field)
        {
            if (field != null)
                return;

            ManagedStaticField resolved;
            if (runtime.TryResolveStaticField(className, names, out resolved))
                field = resolved;
        }

        private void ResolveInstanceIfMissing(
            string className,
            string[] names,
            ref ManagedField field)
        {
            if (field != null)
                return;

            IntPtr managedClass = runtime.FindClass(className);
            ManagedField found;
            if (managedClass != IntPtr.Zero
                && runtime.TryFindFieldAny(managedClass, names, out found)
                && !found.IsStatic)
            {
                field = found;
            }
        }

        private void Detach()
        {
            if (memory != null)
                memory.Dispose();
            memory = null;
            runtime = null;
            version = null;
            mainMenu = null;
            playerMain = null;
            uGuiMain = null;
            inputModuleCurrent = null;
            fpsInputModuleCurrent = null;
            ingameMenuMain = null;
            escapePodMain = null;
            exploderMain = null;
            mainMenuSubMenu = null;
            mainMenuPrimaryOptions = null;
            inputModuleGrid = null;
            uGuiLoading = null;
            uGuiIntro = null;
            uGuiCraftingMenu = null;
            sceneLoadingFlag = null;
            sceneIntroCoroutine = null;
            sceneIntroMainText = null;
            textFadeSequence = null;
            sequenceProgress = null;
            sequenceTarget = null;
            fpsInputModuleLastGroup = null;
            escapePodIntroCinematic = null;
            escapePodAnchorPosition = null;
            cinematicModeActive = null;
            ingameMenuCurrentScreen = null;
            ingameMenuIsQuitting = null;
            explosionCountdown = null;
            explosionWarning = null;
            unityObjectCachedPointer = null;
        }
    }
}
