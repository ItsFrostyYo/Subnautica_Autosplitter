using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiveSplit.SubnauticaMarco.Input
{
    internal static class NativeInput
    {
        private const ushort EscapeKey = 0x1B;
        private const uint MouseLeftDown = 0x0002;
        private const uint MouseLeftUp = 0x0004;
        private static readonly object releaseSync = new object();
        private static int intentionalInputDepth;
        private static int mouseLeftHeld;
        private static bool escapeHeld;

        public static bool IsSendingIntentionalInput => Volatile.Read(ref intentionalInputDepth) > 0;

        // Only the Escape key and the left mouse button are ever synthesized.
        // Wait for the game to be foreground AND for the user to release ALL
        // physical keys/buttons. Do not manipulate the user's held modifiers.
        public static bool IsUserInputIdle()
        {
            for (int vk = 1; vk <= 0xFE; vk++)
            {
                // VK_PACKET (0xE7) is not a physical held key.
                if (vk == 0xE7) continue;
                // Ignore the states this macro owns while it is holding them.
                // All modifier keys, right mouse, and every other key remain blockers.
                if (vk == EscapeKey && escapeHeld) continue;
                if (vk == 0x01 && Volatile.Read(ref mouseLeftHeld) != 0) continue;
                if ((GetAsyncKeyState(vk) & 0x8000) != 0)
                    return false;
            }
            return true;
        }

        public static bool IsSafeToSend(Process process)
        {
            return IsGameFocused(GetWindowHandle(process)) && IsUserInputIdle();
        }

        private static async Task WaitForSafeInputAsync(Process process, CancellationToken token)
        {
            // A normally running macro must not pay a fresh 50 ms wait before
            // every click/Escape. Stabilize only if input was actually unsafe.
            // The final focus/keyboard check is still performed under the input lock.
            if (IsSafeToSend(process))
            {
                token.ThrowIfCancellationRequested();
                return;
            }

            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (process == null || process.HasExited)
                    throw new InvalidOperationException("Subnautica closed while waiting to send input.");

                if (IsSafeToSend(process))
                {
                    // On recovery from focus loss / a held physical key, require
                    // 50 ms of stable idle before allowing any synthetic input.
                    await Task.Delay(50, token).ConfigureAwait(false);
                    if (IsSafeToSend(process)) return;
                }
                await Task.Delay(10, token).ConfigureAwait(false);
            }
        }

        public static async Task ClickAsync(Process process, Point logicalPoint, int delayMilliseconds,
            CancellationToken token)
        {
            BeginIntentionalInput();
            try
            {
                await WaitForSafeInputAsync(process, token).ConfigureAwait(false);
                Point physical;
                if (!WindowCoordinateMapper.TryMap(process, logicalPoint, out physical))
                    physical = MapToMonitor(process, logicalPoint);
                lock (releaseSync)
                {
                    EnsureSafeInput(process);
                    if (!SetCursorPos(physical.X, physical.Y))
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Could not position cursor.");
                    EnsureSafeInput(process);
                    mouse_event(MouseLeftDown, 0, 0, 0, UIntPtr.Zero);
                    mouseLeftHeld = 1;
                }
                try { await Task.Delay(5, token).ConfigureAwait(false); }
                finally { ReleaseMouseLeft(); }
                await Task.Delay(delayMilliseconds, token).ConfigureAwait(false);
            }
            finally { EndIntentionalInput(); }
        }

        public static async Task PressEscapeAsync(Process process, CancellationToken token)
        {
            BeginIntentionalInput();
            try
            {
                await WaitForSafeInputAsync(process, token).ConfigureAwait(false);
                HoldEscapeStart(process);
                try { await Task.Delay(25, token).ConfigureAwait(false); }
                finally { ReleaseEscape(); }
            }
            finally { EndIntentionalInput(); }
        }

        public static async Task HoldEscapeAsync(Process process, int durationMilliseconds,
            CancellationToken token)
        {
            BeginIntentionalInput();
            try
            {
                int remaining = Math.Max(0, durationMilliseconds);
                while (remaining > 0)
                {
                    await WaitForSafeInputAsync(process, token).ConfigureAwait(false);
                    HoldEscapeStart(process);
                    var heldTime = Stopwatch.StartNew();
                    try
                    {
                        while (heldTime.ElapsedMilliseconds < remaining)
                        {
                            token.ThrowIfCancellationRequested();
                            if (!IsSafeToSend(process)) break;
                            int slice = (int)Math.Min(10L,
                                Math.Max(1L, remaining - heldTime.ElapsedMilliseconds));
                            await Task.Delay(slice, token).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        heldTime.Stop();
                        ReleaseEscape();
                    }
                    // Count real elapsed milliseconds while actively holding Esc,
                    // rather than summing Task.Delay requests (which can oversleep).
                    remaining -= (int)Math.Min(remaining, heldTime.ElapsedMilliseconds);
                }
            }
            finally { EndIntentionalInput(); }
        }

        public static bool IsGameFocused(IntPtr gameWindow)
        {
            return gameWindow != IntPtr.Zero && GetForegroundWindow() == gameWindow;
        }

        public static void ReleaseAllInputs()
        {
            lock (releaseSync)
            {
                // Only release inputs this macro itself has pressed. Never release
                // Ctrl, Alt, Shift, right-click, or any user-owned input state.
                try { ReleaseEscapeLocked(); }
                finally { ReleaseMouseLeftLocked(); }
            }
        }

        private static void HoldEscapeStart(Process process)
        {
            lock (releaseSync)
            {
                EnsureSafeInput(process);
                if (escapeHeld)
                    throw new InvalidOperationException("Escape is already held by the macro.");
                // Set before sending so cleanup still attempts key-up on errors.
                escapeHeld = true;
                SendEscape(false);
            }
        }

        private static void ReleaseEscape()
        {
            lock (releaseSync)
                ReleaseEscapeLocked();
        }

        private static void ReleaseEscapeLocked()
        {
            if (!escapeHeld) return;
            // Clear only after successful release, to allow a later cleanup retry.
            SendEscape(true);
            escapeHeld = false;
        }

        private static void ReleaseMouseLeft()
        {
            lock (releaseSync)
                ReleaseMouseLeftLocked();
        }

        private static void ReleaseMouseLeftLocked()
        {
            if (mouseLeftHeld == 0) return;
            mouse_event(MouseLeftUp, 0, 0, 0, UIntPtr.Zero);
            mouseLeftHeld = 0;
        }

        private static void SendEscape(bool release)
        {
            // The only keyboard scancode this plugin is allowed to synthesize is Escape.
            const uint InputKeyboard = 1;
            const uint ScanCodeFlag = 0x0008;
            const uint KeyUpFlag = 0x0002;
            ushort scan = (ushort)MapVirtualKey(EscapeKey, 0);
            if (scan == 0)
                throw new InvalidOperationException("Windows could not map the Escape key.");
            var input = new NativeInputRecord
            {
                Type = InputKeyboard,
                Union = new InputUnion
                {
                    Keyboard = new KeyboardInput
                    {
                        ScanCode = scan,
                        Flags = ScanCodeFlag | (release ? KeyUpFlag : 0)
                    }
                }
            };
            if (SendInput(1, new[] { input }, Marshal.SizeOf(typeof(NativeInputRecord))) != 1)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),
                    "Failed to send Escape " + (release ? "release" : "press"));
        }

        private static void EnsureSafeInput(Process process)
        {
            if (!IsSafeToSend(process))
                throw new InvalidOperationException("Game focus changed or a user key is down; input blocked.");
        }

        private static IntPtr GetWindowHandle(Process process)
        {
            try
            {
                return process == null ? IntPtr.Zero : process.MainWindowHandle;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private static void BeginIntentionalInput()
        {
            Interlocked.Increment(ref intentionalInputDepth);
        }

        private static void EndIntentionalInput()
        {
            Interlocked.Decrement(ref intentionalInputDepth);
        }

        public static void Focus(Process process)
        {
            try
            {
                IntPtr gameWindow = GetWindowHandle(process);
                if (gameWindow != IntPtr.Zero)
                    SetForegroundWindow(gameWindow);
            }
            catch
            {
            }
        }

        private static Point MapToMonitor(Process process, Point logicalPoint)
        {
            Screen screen = process != null && process.MainWindowHandle != IntPtr.Zero
                ? Screen.FromHandle(process.MainWindowHandle)
                : Screen.PrimaryScreen;
            if (screen == null)
                throw new InvalidOperationException("A display for the Subnautica game window could not be located.");

            Rectangle bounds = screen.Bounds;
            return new Point(
                bounds.Left + (int)Math.Round(logicalPoint.X * (bounds.Width / 1920f)),
                bounds.Top + (int)Math.Round(logicalPoint.Y * (bounds.Height / 1080f)));
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int key);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint count, NativeInputRecord[] inputs, int size);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint code, uint mapType);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeInputRecord
        {
            public uint Type;
            public InputUnion Union;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public KeyboardInput Keyboard;
            [FieldOffset(0)] public MouseInput Mouse;
            [FieldOffset(0)] public HardwareInput Hardware;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardInput
        {
            public ushort VirtualKey;
            public ushort ScanCode;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            public int X;
            public int Y;
            public int Data;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HardwareInput
        {
            public uint Message;
            public ushort ParameterLow;
            public ushort ParameterHigh;
        }
    }
}
