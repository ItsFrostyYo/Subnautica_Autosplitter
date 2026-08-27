using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LiveSplit.SubnauticaTracker.Memory
{
    internal sealed class ProcessMemory : IDisposable
    {
        private readonly Process process;

        public ProcessMemory(Process process)
        {
            this.process = process ?? throw new ArgumentNullException(nameof(process));
        }

        public Process Process => process;
        public int PointerSize => 8;
        public string LastError { get; private set; } = string.Empty;

        public bool IsAlive
        {
            get
            {
                try { return !process.HasExited; }
                catch { return false; }
            }
        }

        public bool TryReadBytes(IntPtr address, int count, out byte[] bytes)
        {
            bytes = null;
            if (address == IntPtr.Zero || count <= 0)
            {
                LastError = "Invalid memory read request at 0x"
                    + address.ToInt64().ToString("X") + " for " + count + " byte(s).";
                return false;
            }

            var buffer = new byte[count];
            IntPtr bytesRead;
            bool success;
            try
            {
                success = ReadProcessMemory(process.Handle, address, buffer, count, out bytesRead);
            }
            catch (Exception exception)
            {
                LastError = "ReadProcessMemory could not obtain the game handle: " + exception.Message;
                return false;
            }

            if (!success || bytesRead.ToInt64() != count)
            {
                int error = Marshal.GetLastWin32Error();
                LastError = "ReadProcessMemory failed at 0x"
                    + address.ToInt64().ToString("X")
                    + " (requested " + count + ", read " + bytesRead.ToInt64()
                    + ", Win32 " + error + ").";
                return false;
            }

            LastError = string.Empty;
            bytes = buffer;
            return true;
        }

        public bool TryReadPointer(IntPtr address, out IntPtr value)
        {
            value = IntPtr.Zero;
            byte[] bytes;
            if (!TryReadBytes(address, PointerSize, out bytes))
                return false;

            value = new IntPtr(BitConverter.ToInt64(bytes, 0));
            return true;
        }

        public bool TryReadInt32(IntPtr address, out int value)
        {
            value = 0;
            byte[] bytes;
            if (!TryReadBytes(address, 4, out bytes))
                return false;

            value = BitConverter.ToInt32(bytes, 0);
            return true;
        }

        public bool TryReadUInt16(IntPtr address, out ushort value)
        {
            value = 0;
            byte[] bytes;
            if (!TryReadBytes(address, 2, out bytes))
                return false;

            value = BitConverter.ToUInt16(bytes, 0);
            return true;
        }

        public bool TryReadByte(IntPtr address, out byte value)
        {
            value = 0;
            byte[] bytes;
            if (!TryReadBytes(address, 1, out bytes))
                return false;

            value = bytes[0];
            return true;
        }

        public string ReadUtf8String(IntPtr address, int maximumBytes)
        {
            byte[] bytes;
            if (!TryReadBytes(address, maximumBytes, out bytes))
                return string.Empty;

            int length = Array.IndexOf(bytes, (byte)0);
            if (length < 0)
                length = bytes.Length;

            try { return Encoding.UTF8.GetString(bytes, 0, length); }
            catch { return string.Empty; }
        }

        public string ReadMonoString(IntPtr stringObject, int maximumCharacters)
        {
            if (stringObject == IntPtr.Zero)
                return string.Empty;

            int characterCount;
            if (!TryReadInt32(Add(stringObject, 0x10), out characterCount)
                || characterCount <= 0
                || characterCount > maximumCharacters)
            {
                return string.Empty;
            }

            byte[] bytes;
            if (!TryReadBytes(Add(stringObject, 0x14), characterCount * 2, out bytes))
                return string.Empty;

            try
            {
                string value = Encoding.Unicode.GetString(bytes);
                foreach (char character in value)
                {
                    if (char.IsControl(character) && character != '\t')
                        return string.Empty;
                }
                return value;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static IntPtr Add(IntPtr address, long offset)
        {
            return new IntPtr(address.ToInt64() + offset);
        }

        public void Dispose()
        {
            process.Dispose();
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            IntPtr processHandle,
            IntPtr baseAddress,
            [Out] byte[] buffer,
            int size,
            out IntPtr bytesRead);
    }
}
