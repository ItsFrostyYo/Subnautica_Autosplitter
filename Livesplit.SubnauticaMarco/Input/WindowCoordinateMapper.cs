using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace LiveSplit.SubnauticaMarco.Input
{
    internal static class WindowCoordinateMapper
    {
        private const float BaseWidth = 1920f;
        private const float BaseHeight = 1080f;

        public static bool TryMap(Process process, Point logicalPoint, out Point screenPoint)
        {
            screenPoint = logicalPoint;
            if (process == null)
                return false;

            try
            {
                IntPtr window = process.MainWindowHandle;
                if (process.HasExited || window == IntPtr.Zero)
                    return false;

                NativeRect client;
                if (!GetClientRect(window, out client))
                    return false;

                var origin = new NativePoint();
                if (!ClientToScreen(window, ref origin))
                    return false;

                int width = client.Right - client.Left;
                int height = client.Bottom - client.Top;
                if (width <= 0 || height <= 0)
                    return false;

                screenPoint = new Point(
                    origin.X + (int)Math.Round(logicalPoint.X / BaseWidth * width),
                    origin.Y + (int)Math.Round(logicalPoint.Y / BaseHeight * height));
                return true;
            }
            catch
            {
                return false;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr window, out NativeRect rectangle);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr window, ref NativePoint point);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }
    }
}
