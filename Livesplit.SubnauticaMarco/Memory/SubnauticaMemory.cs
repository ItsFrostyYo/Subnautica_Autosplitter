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
        private ManagedStaticField exploderMain;

        private ManagedField mainMenuSubMenu;
        private ManagedField mainMenuPrimaryOptions;
        private ManagedField inputModuleGrid;
        private ManagedField uGuiLoading;
        private ManagedField uGuiIntro;
        private ManagedField sceneLoadingFlag;
        private ManagedField sceneIntroMainText;
        private ManagedField textFadeSequence;
        private ManagedField sequenceProgress;
        private ManagedField sequenceTarget;
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

                IntPtr value;
                // Match the launcher's state flow exactly: main menu first, then
                // Player.main, with Unity's m_CachedPtr filtering destroyed
                // managed wrappers before either signal is accepted.
                if (TryReadLiveStaticObject(mainMenu, out value) && value != IntPtr.Zero)
                    return GameMemoryState.MainMenu;
                if (TryReadLiveStaticObject(playerMain, out value) && value != IntPtr.Zero)
                    return GameMemoryState.InGame;
                return GameMemoryState.Unknown;
            }
        }

        public GameMemoryState GetResetState()
        {
            lock (sync)
            {
                if (memory == null || !memory.IsAlive)
                    return GameMemoryState.NoProcess;
                if (runtime == null)
                    return GameMemoryState.Initializing;

                IntPtr value;
                if (TryReadLiveStaticObject(mainMenu, out value) && value != IntPtr.Zero)
                    return GameMemoryState.MainMenu;
                if (TryReadLiveStaticObject(playerMain, out value) && value != IntPtr.Zero)
                    return GameMemoryState.InGame;

                bool loading;
                if (TryReadLoadingCore(out loading) && loading)
                    return GameMemoryState.Loading;
                return GameMemoryState.Unknown;
            }
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
                if (!uGuiMain.TryReadPointer(out gui) || gui == IntPtr.Zero
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
            ResolveStaticIfMissing("CrashedShipExploder", new[] { "main", "_main", "s_main", "<main>k__BackingField" }, ref exploderMain);

            ResolveInstanceIfMissing("uGUI_MainMenu", new[] { "subMenu", "_subMenu", "m_subMenu" }, ref mainMenuSubMenu);
            ResolveInstanceIfMissing("uGUI_MainMenu", new[] { "primaryOptions", "_primaryOptions", "m_primaryOptions" }, ref mainMenuPrimaryOptions);
            ResolveInstanceIfMissing("GamepadInputModule", new[] { "currentGrid", "_currentGrid", "m_currentGrid" }, ref inputModuleGrid);
            ResolveInstanceIfMissing("uGUI", new[] { "loading", "_loading", "m_loading" }, ref uGuiLoading);
            ResolveInstanceIfMissing("uGUI", new[] { "intro", "_intro", "m_intro" }, ref uGuiIntro);
            ResolveInstanceIfMissing("uGUI_SceneLoading", new[] { "isLoading", "_isLoading", "m_isLoading" }, ref sceneLoadingFlag);
            ResolveInstanceIfMissing("uGUI_SceneIntro", new[] { "mainText", "_mainText", "m_mainText" }, ref sceneIntroMainText);
            ResolveInstanceIfMissing("uGUI_TextFade", new[] { "sequence", "_sequence", "m_sequence" }, ref textFadeSequence);
            ResolveInstanceIfMissing("Sequence", new[] { "_t", "t", "m_t" }, ref sequenceProgress);
            ResolveInstanceIfMissing("Sequence", new[] { "_target", "target", "m_target" }, ref sequenceTarget);
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

        private bool TryReadObject(IntPtr instance, ManagedField field, out IntPtr value)
        {
            value = IntPtr.Zero;
            return field != null
                && memory.TryReadPointer(ProcessMemory.Add(instance, field.Offset), out value);
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
            exploderMain = null;
            mainMenuSubMenu = null;
            mainMenuPrimaryOptions = null;
            inputModuleGrid = null;
            uGuiLoading = null;
            uGuiIntro = null;
            sceneLoadingFlag = null;
            sceneIntroMainText = null;
            textFadeSequence = null;
            sequenceProgress = null;
            sequenceTarget = null;
            explosionCountdown = null;
            explosionWarning = null;
            unityObjectCachedPointer = null;
        }
    }
}
