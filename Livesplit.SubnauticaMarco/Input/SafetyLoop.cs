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
            timer = new Timer(Check, null, 1000, 1000);
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
            if (disposed || !isMacroRunning() || Interlocked.Exchange(ref checking, 1) != 0)
                return;

            try
            {
                IntPtr gameWindow = getGameWindow();
                if (!NativeInput.IsSendingIntentionalInput || !NativeInput.IsGameFocused(gameWindow))
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
