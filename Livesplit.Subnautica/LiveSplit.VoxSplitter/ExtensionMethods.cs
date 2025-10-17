using LiveSplit.ComponentUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LiveSplit.VoxSplitter {
    public static class ExtensionMethods {

        //
        // PROCESS
        //
        public static ProcessModuleWow64Safe[] Modules(this Process process) {
            try {
                return process.ModulesWow64Safe();
            } catch {
                return null;
            }
        }

        public static bool HasModule(this Process process, string module) {
            return process.Modules().Any(m => m.ModuleName.Equals(module, StringComparison.OrdinalIgnoreCase));
        }

        public static T Read<T>(this Process process, IntPtr address) where T : unmanaged {
            return process.Read<T>(address, EDerefType.Auto);
        }
        public static T Read<T>(this Process process, IntPtr address, EDerefType derefType) where T : unmanaged {
            return process.Read<T>(address, derefType == EDerefType.Auto ? process.Is64Bit() : derefType == EDerefType.Bit64);
        }
        public unsafe static T Read<T>(this Process process, IntPtr address, bool is64Bit) where T : unmanaged {
            if(address == default) { return default; }

            int size = typeof(T) == typeof(IntPtr) ? (is64Bit ? 8 : 4) : sizeof(T);
            byte[] buffer = process.ReadBytes(address, size);
            return buffer?.To<T>() ?? default;
        }

        public static IntPtr DerefOffsets(this Process process, IntPtr ptr, params int[] offsets) {
            return DerefOffsets(process, EDerefType.Auto, ptr, offsets);
        }
        public static IntPtr DerefOffsets(this Process process, EDerefType derefType, IntPtr ptr, params int[] offsets) {
            if(ptr == default) { return default; }
            
            if(offsets.Length == 0) { return ptr; }
            
            bool is64Bit = derefType == EDerefType.Auto ? process.Is64Bit() : derefType == EDerefType.Bit64;
            for(int i = 0; i < offsets.Length - 1; i++) {
                ptr = process.Read<IntPtr>(ptr + offsets[i], is64Bit);
                if(ptr == default) {
                    return default;
                }
            }
            return ptr += offsets[offsets.Length - 1];
        }

        public static string ReadString(this Process process, IntPtr ptr, EStringType type = EStringType.Auto) {
            const string empty = "";

            if(ptr == default) { return empty; }

            const int maxSize = 1024;

            bool isUnicode = type == EStringType.UTF16 || type == EStringType.UTF16Sized;
            Encoding encoding = isUnicode ? Encoding.Unicode : Encoding.UTF8;
            int charSize = isUnicode ? 2 : 1;

            if(type == EStringType.UTF8Sized || type == EStringType.UTF16Sized) {
                int size = process.ReadValue<int>(ptr - 0x4);
                if(size >= maxSize) {
                    return empty;
                }
                byte[] stringBytes = new byte[size * charSize];
                if(!NativeMethods.ReadProcessMemory(process.Handle, ptr, stringBytes, stringBytes.Length, out int readLength) || readLength != stringBytes.Length) {
                    return empty;
                }
                return encoding.GetString(stringBytes);
            } else {
                byte[] buffer = new byte[64];
                List<byte> stringBytes = new List<byte>(buffer.Length);
                int offset = 0;
                while(NativeMethods.ReadProcessMemory(process.Handle, ptr + offset, buffer, buffer.Length, out int readLength) && readLength == buffer.Length) {
                    if(offset >= maxSize) {
                        return empty;
                    }

                    if(type == EStringType.Auto && offset == 0 && buffer[1] == 0) {
                        isUnicode = true;
                        encoding = Encoding.Unicode;
                        charSize = 2;
                    }

                    for(int c = 0; c < readLength; c += charSize) {
                        if(buffer[c] == 0) {
                            return encoding.GetString(stringBytes.ToArray());
                        } else {
                            stringBytes.Add(buffer[c]);
                            if(isUnicode) {
                                stringBytes.Add(buffer[c + 1]);
                            }
                        }
                    }

                    offset += readLength;
                }
            }
            return empty;
        }

        public static IntPtr GetSymbolAddress(this Process process, string moduleName, string symbol) {
            try {
                ProcessModuleWow64Safe module = process.Modules().FirstOrDefault(m => m.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase));
                if(module == null) {
                    return default;
                }
                SymbolInfo[] symbols = process.EnumerateSymbols(module, symbol);
                return symbols.Length > 0 ? (IntPtr)symbols[0].address : default;
            } catch(Exception e) {
                Options.Log.Error(e.ToString());
                return default;
            }
        }

        public static SymbolInfo[] EnumerateSymbols(this Process process, ProcessModuleWow64Safe module, string symbol = "*") {
            IntPtr hProcess = process.Handle;

            if(!NativeMethods.SymInitialize(hProcess, null, false)) {
                throw new Exception("Failed to initialize symbols");
            }

            List<SymbolInfo> symbolList = new List<SymbolInfo>();
            try {
                if(NativeMethods.SymLoadModuleEx(hProcess, default, module.ModuleName, null, (long)module.BaseAddress, module.ModuleMemorySize, default, 0) == 0) {
                    throw new Exception("Failed to load module's symbols");
                }

                if(!NativeMethods.SymEnumSymbols(hProcess, (ulong)module.BaseAddress, symbol, EnumSyms, default)) {
                    throw new Exception("Failed to enumerate symbols");
                }
            } finally {
                NativeMethods.SymCleanup(hProcess);
            }

            return symbolList.ToArray();

            bool EnumSyms(ref SymbolInfo pSymInfo, uint SymbolSize, IntPtr UserContext) {
                symbolList.Add(pSymInfo);
                return true;
            }
        }

        //
        // UNMANAGED
        //
        public unsafe static T To<T>(this byte[] bytes) where T : unmanaged {
            fixed(byte* p = bytes) {
                return *(T*)p;
            }
        }
        public unsafe static T To<T>(this byte[] bytes, int offset) where T : unmanaged {
            fixed(byte* p = bytes) {
                return *(T*)(p + offset);
            }
        }

        public unsafe static byte[] ToBytes<T>(this T value) where T : unmanaged {
            int size = sizeof(T);
            byte[] bytes = new byte[size];
            return value.ToBytes(bytes, size);
        }
        public unsafe static byte[] ToBytes<T>(this T value, byte[] bytes) where T : unmanaged {
            int size = sizeof(T);
            return value.ToBytes(bytes, size);
        }
        public unsafe static byte[] ToBytes<T>(this T value, byte[] bytes, int size) where T : unmanaged {
            fixed(byte* p = bytes) {
                Buffer.MemoryCopy(&value, p, bytes.Length, size);
            }
            return bytes;
        }

        //
        // ENUM
        //
        public static string GetDescription(this Enum enumVal) {
            return enumVal.GetAttributeOfType<DescriptionAttribute>()?.Description;
        }

        public static T GetAttributeOfType<T>(this Enum enumVal) where T : Attribute {
            Type type = enumVal.GetType();
            object[] attributes = type.GetMember(Enum.GetName(type, enumVal))[0].GetCustomAttributes(typeof(T), false);
            return (attributes.Length > 0) ? (T)attributes[0] : null;
        }

        //
        // ASSEMBLY
        //
        public static string FullComponentName(this Assembly asm) {
            string name = asm.GetName().Name.Substring(10);
            StringBuilder sb = new StringBuilder(name.Length * 2);
            sb.Append(name[0]);
            for(int i = 1; i < name.Length; i++) {
                if(Char.IsUpper(name[i]) && name[i - 1] != ' ') {
                    sb.Append(' ');
                }
                sb.Append(name[i]);
            }
            sb.Append(" Autosplitter v").Append(asm.GetName().Version.ToString(3));
            return sb.ToString();
        }
        public static string GitMainURL(this Assembly asm) => Path.Combine("https://raw.githubusercontent.com/Sprinter31", asm.GetName().Name, "main/");
        public static string ResourcesURL(this Assembly asm) => Path.Combine(asm.GitMainURL(), "Resources");
        public static string ResourcesPath(this Assembly asm) => Path.Combine(Path.GetDirectoryName(asm.Location), asm.GetName().Name);
        public static string Description(this Assembly asm) => ((AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(asm, typeof(AssemblyDescriptionAttribute))).Description;


        //
        // ARRAY
        //
        // Linq.Prepend replacement function for .net framework 4.6.1
        public static T[] Prepend<T>(this T[] array, T value) {
            T[] newArray = new T[array.Length + 1];
            newArray[0] = value;
            Array.Copy(array, 0, newArray, 1, array.Length);
            return newArray;
        }
    }
}