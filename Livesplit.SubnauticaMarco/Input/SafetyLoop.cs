using System;
using System.Threading;

namespace LiveSplit.SubnauticaMarco.Input
{
    internal sealed class SafetyLoop : IDisposable
    {
        private readonly Func<bool> isMacroRunning;
        private readonly Func<IntPtr> getGameWindow;
        private readonly Timer timer;
        private int checking;
        private bool disposed;

        public SafetyLoop(Func<bool> isMacroRunning, Func<IntPtr> getGameWindow)
        {
            this.isMacroRunning = isMacroRunning;
            this.getGameWindow = getGameWindow;
            timer = new Timer(Check, null, 50, 50);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            timer.Dispose();
            NativeInput.ReleaseAllInputs();
        }

        private void Check(object state)
        {
            if (disposed) return;
            if (!isMacroRunning()) return;
            if (Interlocked.Exchange(ref checking, 1) != 0)
                return;

            try
            {
                IntPtr gameWindow = getGameWindow();
                // Release only our own Escape/left button immediately when focus
                // changes or the user presses a physical key. Never cancel the loop:
                // the operation waits for safe focus and input, then resumes.
                if (!NativeInput.IsGameFocused(gameWindow) || !NativeInput.IsUserInputIdle()
                    || !NativeInput.IsSendingIntentionalInput)
                    NativeInput.ReleaseAllInputs();
            }
            catch
            {
                NativeInput.ReleaseAllInputs();
            }
            finally
            {
                Interlocked.Exchange(ref checking, 0);
            }
        }
    }
}
