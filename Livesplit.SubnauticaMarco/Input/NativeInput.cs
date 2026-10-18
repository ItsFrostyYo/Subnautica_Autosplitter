using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private const uint MouseRightUp = 0x0010;
        private const uint MouseMiddleUp = 0x0040;
        private const uint MouseXUp = 0x0100;
        private const uint MouseXButton1 = 0x0001;
        private const uint MouseXButton2 = 0x0002;
        private static readonly object releaseSync = new object();
        private static int intentionalInputDepth;
        private static int mouseLeftHeld;

        public static bool IsSendingIntentionalInput => Volatile.Read(ref intentionalInputDepth) > 0;

        public static async Task ClickAsync(Process process, Point logicalPoint, int delayMilliseconds)
        {
            BeginIntentionalInput();
            try
            {
                Point physical;
                if (!WindowCoordinateMapper.TryMap(process, logicalPoint, out physical))
                    physical = MapToMonitor(process, logicalPoint);

                Focus(process);
                await Task.Delay(10).ConfigureAwait(false);
                SetCursorPos(physical.X, physical.Y);
                mouse_event(MouseLeftDown, 0, 0, 0, UIntPtr.Zero);
                Interlocked.Exchange(ref mouseLeftHeld, 1);
                try
                {
                    await Task.Delay(5).ConfigureAwait(false);
                }
                finally
                {
                    mouse_event(MouseLeftUp, 0, 0, 0, UIntPtr.Zero);
                    Interlocked.Exchange(ref mouseLeftHeld, 0);
                }
                await Task.Delay(delayMilliseconds).ConfigureAwait(false);
            }
            finally
            {
                EndIntentionalInput();
            }
        }

        public static void PressEscape()
        {
            BeginIntentionalInput();
            try
            {
                KeyController.HoldStart(EscapeKey);
                Thread.Sleep(25);
            }
            finally
            {
                KeyController.HoldStop(EscapeKey);
                EndIntentionalInput();
            }
        }

        public static async Task HoldEscapeAsync(
            Process process,
            int durationMilliseconds,
            CancellationToken cancellationToken)
        {
            BeginIntentionalInput();
            try
            {
                Focus(process);
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                KeyController.HoldStart(EscapeKey);
                await Task.Delay(durationMilliseconds, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                KeyController.HoldStop(EscapeKey);
                EndIntentionalInput();
            }
        }

        public static bool IsGameFocused(IntPtr gameWindow)
        {
            return gameWindow != IntPtr.Zero && GetForegroundWindow() == gameWindow;
        }

        public static void ReleaseAllInputs()
        {
            lock (releaseSync)
            {
                KeyController.ReleaseAll();

                if (Interlocked.Exchange(ref mouseLeftHeld, 0) != 0)
                    mouse_event(MouseLeftUp, 0, 0, 0, UIntPtr.Zero);

                // This is only called while the macro is active but is not
                // intentionally sending input, or immediately when it stops.
                KeyController.ReleaseEveryKey();
                mouse_event(MouseLeftUp | MouseRightUp | MouseMiddleUp, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MouseXUp, 0, 0, MouseXButton1, UIntPtr.Zero);
                mouse_event(MouseXUp, 0, 0, MouseXButton2, UIntPtr.Zero);
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
                if (process != null && process.MainWindowHandle != IntPtr.Zero)
                    SetForegroundWindow(process.MainWindowHandle);
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
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private static class KeyController
        {
            private const uint InputKeyboard = 1;
            private const uint KeyEventExtended = 0x0001;
            private const uint KeyEventUp = 0x0002;
            private const uint KeyEventScanCode = 0x0008;
            private const uint MapVirtualKeyToScanCode = 0;
            private const uint GetKeyboardDelay = 0x0016;
            private const uint GetKeyboardSpeed = 0x000A;

            private static readonly HashSet<ushort> VirtualKeyOnly = new HashSet<ushort>
            {
                0xAE, 0xAF, 0xAD,
                0xB0, 0xB1, 0xB2, 0xB3,
                0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC,
                0xB4, 0xB5, 0xB6, 0xB7
            };

            private static readonly ConcurrentDictionary<ushort, HoldState> Holds =
                new ConcurrentDictionary<ushort, HoldState>();

            public static void HoldStart(ushort virtualKey)
            {
                if (Holds.ContainsKey(virtualKey))
                    return;

                int initialDelay;
                double repeatHz;
                GetSystemRepeatSettings(out initialDelay, out repeatHz);

                var state = new HoldState
                {
                    VirtualKey = virtualKey,
                    TapMode = VirtualKeyOnly.Contains(virtualKey),
                    InitialDelayMilliseconds = initialDelay,
                    RepeatHz = repeatHz,
                    Cancellation = new CancellationTokenSource()
                };
                if (!Holds.TryAdd(virtualKey, state))
                {
                    state.Cancellation.Dispose();
                    return;
                }

                CancellationToken token = state.Cancellation.Token;
                if (state.TapMode)
                {
                    Send(MakeKey(virtualKey, false), MakeKey(virtualKey, true));
                }
                else
                {
                    Send(MakeKey(virtualKey, false));
                    state.LeftDown = true;
                }

                state.Worker = Task.Run(delegate
                {
                    if (token.WaitHandle.WaitOne(state.InitialDelayMilliseconds))
                        return;

                    int period = Math.Max(5, (int)Math.Round(1000.0 / state.RepeatHz));
                    while (!token.IsCancellationRequested)
                    {
                        if (token.WaitHandle.WaitOne(period))
                            break;

                        if (state.TapMode)
                            Send(MakeKey(virtualKey, false), MakeKey(virtualKey, true));
                        else
                            Send(MakeKey(virtualKey, false));
                    }
                }, token);
            }

            public static void HoldStop(ushort virtualKey)
            {
                HoldState state;
                if (!Holds.TryRemove(virtualKey, out state))
                    return;

                try
                {
                    state.Cancellation.Cancel();
                    if (state.Worker != null)
                        state.Worker.Wait(300);
                }
                catch
                {
                }

                if (!state.TapMode && state.LeftDown)
                    Send(MakeKey(virtualKey, true));

                state.Cancellation.Dispose();
            }

            public static void ReleaseAll()
            {
                foreach (ushort virtualKey in Holds.Keys)
                    HoldStop(virtualKey);
            }

            public static void ReleaseEveryKey()
            {
                var releases = new List<NativeInputRecord>();
                for (ushort virtualKey = 0x08; virtualKey < 0xFF; virtualKey++)
                {
                    releases.Add(new NativeInputRecord
                    {
                        Type = InputKeyboard,
                        Union = new InputUnion
                        {
                            Keyboard = new KeyboardInput
                            {
                                VirtualKey = virtualKey,
                                Flags = KeyEventUp | (IsExtended(virtualKey) ? KeyEventExtended : 0)
                            }
                        }
                    });
                }
                Send(releases.ToArray());
            }

            private static NativeInputRecord MakeKey(ushort virtualKey, bool keyUp)
            {
                if (VirtualKeyOnly.Contains(virtualKey))
                {
                    return new NativeInputRecord
                    {
                        Type = InputKeyboard,
                        Union = new InputUnion
                        {
                            Keyboard = new KeyboardInput
                            {
                                VirtualKey = virtualKey,
                                Flags = (keyUp ? KeyEventUp : 0) | KeyEventExtended
                            }
                        }
                    };
                }

                ushort scanCode = (ushort)MapVirtualKey(virtualKey, MapVirtualKeyToScanCode);
                return new NativeInputRecord
                {
                    Type = InputKeyboard,
                    Union = new InputUnion
                    {
                        Keyboard = new KeyboardInput
                        {
                            ScanCode = scanCode,
                            Flags = KeyEventScanCode
                                | (IsExtended(virtualKey) ? KeyEventExtended : 0)
                                | (keyUp ? KeyEventUp : 0)
                        }
                    }
                };
            }

            private static bool IsExtended(ushort virtualKey)
            {
                switch (virtualKey)
                {
                    case 0x25:
                    case 0x26:
                    case 0x27:
                    case 0x28:
                    case 0x2D:
                    case 0x2E:
                    case 0x24:
                    case 0x23:
                    case 0x21:
                    case 0x22:
                    case 0x6F:
                    case 0xA3:
                    case 0xA5:
                        return true;
                    default:
                        return false;
                }
            }

            private static void Send(params NativeInputRecord[] inputs)
            {
                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(NativeInputRecord)));
            }

            private static void GetSystemRepeatSettings(out int initialDelay, out double repeatHz)
            {
                uint delayIndex;
                initialDelay = SystemParametersInfo(GetKeyboardDelay, 0, out delayIndex, 0)
                    ? (int)(delayIndex + 1) * 250
                    : 300;

                uint speedIndex;
                repeatHz = SystemParametersInfo(GetKeyboardSpeed, 0, out speedIndex, 0)
                    ? 2.5 + speedIndex * (31.0 - 2.5) / 31.0
                    : 25.0;
            }

            [DllImport("user32.dll", SetLastError = true)]
            private static extern uint SendInput(uint count, NativeInputRecord[] inputs, int size);

            [DllImport("user32.dll")]
            private static extern uint MapVirtualKey(uint code, uint mapType);

            [DllImport("user32.dll")]
            private static extern bool SystemParametersInfo(
                uint action,
                uint parameter,
                out uint value,
                uint flags);

            private sealed class HoldState
            {
                public ushort VirtualKey;
                public bool TapMode;
                public bool LeftDown;
                public int InitialDelayMilliseconds;
                public double RepeatHz;
                public CancellationTokenSource Cancellation;
                public Task Worker;
            }

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
}
