using LiveSplit.SubnauticaMarco.Configuration;
using LiveSplit.SubnauticaMarco.Input;
using LiveSplit.SubnauticaMarco.Memory;
using LiveSplit.SubnauticaTracker.Versions;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiveSplit.SubnauticaMarco.Macros
{
    internal sealed class MacroController : IDisposable
    {
        private readonly object sync = new object();
        private readonly SubnauticaMemory memory = new SubnauticaMemory();
        private readonly HotkeyWindow hotkeyWindow;
        private CancellationTokenSource cancellation;
        private Task activeTask;
        private int pendingHotkey;
        private int registeredHotkey;
        private bool hotkeyCaptureActive;
        private bool waitForHotkeyRelease;
        private bool running;
        private MacroConfiguration latestConfiguration;
        private string activity = string.Empty;
        private int explosionResets;

        public MacroController(IntPtr liveSplitWindow)
        {
            hotkeyWindow = new HotkeyWindow(liveSplitWindow, delegate
            {
                Interlocked.Exchange(ref pendingHotkey, 1);
            });
        }

        public string DisplayText
        {
            get
            {
                lock (sync)
                {
                    if (running)
                        return activity;
                }

                GameMemoryState state = memory.GetResetState();
                string version = memory.VersionName;
                switch (state)
                {
                    case GameMemoryState.NoProcess: return "No Process";
                    case GameMemoryState.Initializing: return FormatVersion(version, "Initializing");
                    case GameMemoryState.MainMenu: return FormatVersion(version, "Main Menu");
                    case GameMemoryState.InGame: return FormatVersion(version, "Ready");
                    case GameMemoryState.Loading: return FormatVersion(version, "Loading");
                    default: return FormatVersion(version, "Waiting");
                }
            }
        }

        public void Update(MacroConfiguration configuration)
        {
            memory.Update();
            lock (sync)
                latestConfiguration = Copy(configuration);

            ConfigureHotkey(configuration);
            if (Interlocked.Exchange(ref pendingHotkey, 0) != 0)
            {
                if (IsRunning())
                    Cancel();
                else
                    Start(Copy(configuration));
            }
        }

        public void SetHotkeyCaptureActive(bool active)
        {
            hotkeyCaptureActive = active;
            if (active)
            {
                hotkeyWindow.Unregister();
                registeredHotkey = 0;
                Interlocked.Exchange(ref pendingHotkey, 0);
            }
            else
            {
                waitForHotkeyRelease = true;
            }
        }

        public void Dispose()
        {
            Cancel();
            hotkeyWindow.Dispose();
            memory.Dispose();
        }

        private void Start(MacroConfiguration configuration)
        {
            lock (sync)
            {
                if (running)
                    return;
                running = true;
                explosionResets = 0;
                cancellation = new CancellationTokenSource();
            }

            Task operation = configuration.ExplosionReset
                ? RunExplosionResetAsync(configuration, cancellation.Token)
                : RunSingleResetAsync(configuration.GameMode, cancellation.Token);
            activeTask = CompleteOperationAsync(operation);
        }

        private async Task CompleteOperationAsync(Task operation)
        {
            try
            {
                await operation.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                SetActivity("Macro Canceled");
            }
            catch (Exception exception)
            {
                SetActivity("Macro Error");
                Log("ERROR: " + exception);
            }
            finally
            {
                lock (sync)
                {
                    running = false;
                    if (cancellation != null)
                    {
                        cancellation.Dispose();
                        cancellation = null;
                    }
                }
            }
        }

        private void Cancel()
        {
            lock (sync)
            {
                if (cancellation != null)
                    cancellation.Cancel();
            }
        }

        private async Task RunSingleResetAsync(ResetGameMode mode, CancellationToken token)
        {
            Process process;
            SubnauticaVersion version;
            if (!memory.TryGetProcess(out process, out version))
                throw new InvalidOperationException("Subnautica is not running or its version is not initialized.");

            using (process)
            {
                var profile = MacroProfile.Create(version, mode);
                GameMemoryState state = memory.GetState();
                Log("Reset started. Version=" + version + ", Mode=" + mode + ", State=" + state + ".");

                if (state == GameMemoryState.InGame)
                {
                    SetActivity("Quitting to Main Menu");
                    NativeInput.Focus(process);
                    NativeInput.PressEscape();
                    Log("Escape input sent; waiting for the pause menu.");
                    await Task.Delay(50, token).ConfigureAwait(false);
                    await NativeInput.ClickAsync(process, profile.Quit, 20).ConfigureAwait(false);
                    Log("Quit button clicked.");
                    await Task.Delay(50, token).ConfigureAwait(false);
                    await NativeInput.ClickAsync(process, profile.ConfirmQuit, 20).ConfigureAwait(false);
                    Log("First quit confirmation clicked.");
                    await Task.Delay(50, token).ConfigureAwait(false);
                    await NativeInput.ClickAsync(process, profile.ConfirmQuitAgain, 20).ConfigureAwait(false);
                    Log("Final quit confirmation clicked; waiting for main-menu memory readiness.");
                }
                else if (state != GameMemoryState.MainMenu)
                {
                    throw new InvalidOperationException("The game is not in a safe reset state: " + state + ".");
                }

                SetActivity("Waiting for Main Menu");
                await WaitForMainMenuReadyAsync(token).ConfigureAwait(false);
                SetActivity("Starting " + FormatMode(mode));

                await NativeInput.ClickAsync(process, profile.Play, 10).ConfigureAwait(false);
                await Task.Delay(50, token).ConfigureAwait(false);
                await NativeInput.ClickAsync(process, profile.NewGame, 30).ConfigureAwait(false);
                await Task.Delay(profile.ModernMenu ? 150 : 50, token).ConfigureAwait(false);
                await NativeInput.ClickAsync(process, profile.GameMode, 20).ConfigureAwait(false);

                SetActivity("Reset Complete");
                Log("Reset completed.");
            }
        }

        private async Task RunExplosionResetAsync(MacroConfiguration configuration, CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                await Task.Delay(250, token).ConfigureAwait(false);
                await RunSingleResetAsync(configuration.GameMode, token).ConfigureAwait(false);

                await Task.Delay(50, token).ConfigureAwait(false);

                SetActivity("Waiting for Intro");
                await WaitForIntroReadyAsync(token).ConfigureAwait(false);

                float explosionTime = await WaitForExplosionTimeAsync(token).ConfigureAwait(false);
                MacroConfiguration currentConfiguration;
                lock (sync)
                    currentConfiguration = Copy(latestConfiguration ?? configuration);

                float minimum;
                float maximum;
                GetExplosionRange(currentConfiguration, out minimum, out maximum);

                SetActivity("Explosion " + FormatTime(explosionTime));
                Log("Explosion time=" + explosionTime.ToString("F3")
                    + ", target=" + minimum.ToString("F3") + ".." + maximum.ToString("F3") + ".");

                if (explosionTime >= minimum && explosionTime <= maximum)
                {
                    SetActivity("Good Time " + FormatTime(explosionTime));
                    Log("Good explosion time found after " + explosionResets + " reset(s).");
                    return;
                }

                Process process;
                SubnauticaVersion version;
                if (!memory.TryGetProcess(out process, out version))
                    throw new InvalidOperationException("The Subnautica process closed during the explosion reset.");

                using (process)
                {
                    SetActivity("Bad Time - Resetting");
                    int hold = version == SubnauticaVersion.Build2018
                        || version == SubnauticaVersion.Build2021 ? 1050 : 1550;
                    await NativeInput.HoldEscapeAsync(process, hold, token).ConfigureAwait(false);
                }

                explosionResets++;
            }
        }

        private async Task WaitForMainMenuReadyAsync(CancellationToken token)
        {
            var timeout = Stopwatch.StartNew();
            int stableSamples = 0;
            while (timeout.ElapsedMilliseconds < 15000)
            {
                token.ThrowIfCancellationRequested();
                memory.Update();
                bool ready;
                bool valid = memory.TryReadMainMenuReady(out ready) && ready;
                stableSamples = valid ? stableSamples + 1 : 0;
                if (stableSamples >= 2)
                    return;
                await Task.Delay(25, token).ConfigureAwait(false);
            }

            throw new TimeoutException("The main menu did not become ready in memory.");
        }

        private async Task WaitForIntroReadyAsync(CancellationToken token)
        {
            var timeout = Stopwatch.StartNew();
            bool sawLoading = false;
            while (timeout.ElapsedMilliseconds < 45000)
            {
                token.ThrowIfCancellationRequested();
                memory.Update();

                bool loading;
                if (memory.TryReadLoading(out loading) && loading)
                    sawLoading = true;

                bool ready;
                if (memory.TryReadIntroReady(out ready) && ready)
                {
                    Log("Intro continue prompt ready after "
                        + timeout.ElapsedMilliseconds + "ms. LoadingObserved=" + sawLoading + ".");
                    return;
                }
                await Task.Delay(25, token).ConfigureAwait(false);
            }

            throw new TimeoutException(
                "The intro continue prompt did not become ready in memory. LoadingObserved="
                + sawLoading + ".");
        }

        private async Task<float> WaitForExplosionTimeAsync(CancellationToken token)
        {
            var timeout = Stopwatch.StartNew();
            while (timeout.ElapsedMilliseconds < 10000)
            {
                token.ThrowIfCancellationRequested();
                float seconds;
                if (memory.TryReadExplosionTime(out seconds))
                    return seconds;
                await Task.Delay(25, token).ConfigureAwait(false);
            }

            throw new TimeoutException("The Aurora explosion timer could not be read from memory.");
        }

        private bool IsRunning()
        {
            lock (sync)
                return running;
        }

        private void SetActivity(string value)
        {
            lock (sync)
                activity = value + (explosionResets > 0 ? " (" + explosionResets + ")" : string.Empty);
        }

        private static MacroConfiguration Copy(MacroConfiguration value)
        {
            return new MacroConfiguration
            {
                Enabled = value.Enabled,
                Hotkey = value.Hotkey,
                GameMode = value.GameMode,
                ExplosionReset = value.ExplosionReset,
                ExplosionPreset = value.ExplosionPreset,
                CustomMinimumSeconds = value.CustomMinimumSeconds,
                CustomMaximumSeconds = value.CustomMaximumSeconds
            };
        }

        private static void GetExplosionRange(MacroConfiguration configuration, out float minimum, out float maximum)
        {
            switch (configuration.ExplosionPreset)
            {
                case ExplosionPreset.Minutes46To46_30: minimum = 2760f; maximum = 2790f; return;
                case ExplosionPreset.Minutes46To47: minimum = 2760f; maximum = 2820f; return;
                case ExplosionPreset.Minutes46To48: minimum = 2760f; maximum = 2880f; return;
                case ExplosionPreset.Minutes46To50: minimum = 2760f; maximum = 3000f; return;
                case ExplosionPreset.UnderOneHour: minimum = 0f; maximum = 3600f; return;
                case ExplosionPreset.OverOneHour: minimum = 3600f; maximum = float.MaxValue; return;
                default:
                    minimum = Math.Max(0, configuration.CustomMinimumSeconds);
                    maximum = Math.Max(minimum, configuration.CustomMaximumSeconds);
                    return;
            }
        }

        private static string FormatVersion(string version, string status)
        {
            return string.IsNullOrEmpty(version) ? status : "Subnautica " + version + " - " + status;
        }

        private static string FormatMode(ResetGameMode mode)
        {
            return mode.ToString().Replace("Rng", "RNG ");
        }

        private static string FormatTime(float seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return ((int)time.TotalMinutes).ToString("00") + ":" + time.Seconds.ToString("00")
                + "." + time.Milliseconds.ToString("000");
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SubnauticaMarco.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " | " + message + Environment.NewLine);
            }
            catch
            {
            }
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        private void ConfigureHotkey(MacroConfiguration configuration)
        {
            if (hotkeyCaptureActive || !configuration.Enabled || configuration.Hotkey <= 0)
            {
                if (registeredHotkey != 0)
                {
                    hotkeyWindow.Unregister();
                    registeredHotkey = 0;
                }
                return;
            }

            if (waitForHotkeyRelease)
            {
                if (IsHotkeyDown(configuration.Hotkey))
                    return;
                waitForHotkeyRelease = false;
            }

            if (registeredHotkey == configuration.Hotkey)
                return;

            hotkeyWindow.Unregister();
            registeredHotkey = hotkeyWindow.Register(configuration.Hotkey)
                ? configuration.Hotkey
                : 0;
        }

        private static bool IsHotkeyDown(int hotkey)
        {
            Keys keys = (Keys)hotkey;
            int keyCode = (int)(keys & Keys.KeyCode);
            if (keyCode != 0 && (GetAsyncKeyState(keyCode) & 0x8000) != 0)
                return true;

            Keys modifiers = keys & Keys.Modifiers;
            return (modifiers.HasFlag(Keys.Control) && (GetAsyncKeyState((int)Keys.ControlKey) & 0x8000) != 0)
                || (modifiers.HasFlag(Keys.Shift) && (GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0)
                || (modifiers.HasFlag(Keys.Alt) && (GetAsyncKeyState((int)Keys.Menu) & 0x8000) != 0);
        }

        private sealed class HotkeyWindow : System.Windows.Forms.NativeWindow, IDisposable
        {
            private const int HotkeyId = 0x534D;
            private const int HotkeyMessage = 0x0312;
            private readonly Action pressed;
            private bool registered;

            public HotkeyWindow(IntPtr window, Action pressed)
            {
                this.pressed = pressed;
                AssignHandle(window);
            }

            public bool Register(int virtualKey)
            {
                Unregister();
                Keys keys = (Keys)virtualKey;
                uint modifiers = 0;
                if (keys.HasFlag(Keys.Alt))
                    modifiers |= 0x0001;
                if (keys.HasFlag(Keys.Control))
                    modifiers |= 0x0002;
                if (keys.HasFlag(Keys.Shift))
                    modifiers |= 0x0004;

                registered = RegisterHotKey(
                    Handle,
                    HotkeyId,
                    modifiers,
                    (uint)(keys & Keys.KeyCode));
                return registered;
            }

            public void Unregister()
            {
                if (!registered)
                    return;
                UnregisterHotKey(Handle, HotkeyId);
                registered = false;
            }

            public void Dispose()
            {
                Unregister();
                ReleaseHandle();
            }

            protected override void WndProc(ref System.Windows.Forms.Message message)
            {
                if (message.Msg == HotkeyMessage && message.WParam.ToInt32() == HotkeyId)
                    pressed();
                base.WndProc(ref message);
            }

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool RegisterHotKey(
                IntPtr window,
                int id,
                uint modifiers,
                uint virtualKey);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool UnregisterHotKey(IntPtr window, int id);
        }
    }
}
