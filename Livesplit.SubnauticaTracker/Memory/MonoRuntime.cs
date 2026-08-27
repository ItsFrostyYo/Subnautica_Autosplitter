using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace LiveSplit.SubnauticaTracker.Memory
{
    internal enum MonoRuntimeFlavor
    {
        Legacy,
        Modern
    }

    internal sealed class ManagedField
    {
        public ManagedField(IntPtr declaringClass, int offset, bool isStatic)
        {
            DeclaringClass = declaringClass;
            Offset = offset;
            IsStatic = isStatic;
        }

        public IntPtr DeclaringClass { get; }
        public int Offset { get; }
        public bool IsStatic { get; }
    }

    internal sealed class ManagedStaticField
    {
        private readonly MonoRuntime runtime;

        public ManagedStaticField(MonoRuntime runtime, ManagedField field)
        {
            this.runtime = runtime;
            Field = field;
        }

        public ManagedField Field { get; }

        public bool TryReadPointer(out IntPtr value)
        {
            value = IntPtr.Zero;
            IntPtr staticData;
            return runtime.TryGetStaticData(Field.DeclaringClass, out staticData)
                && runtime.Memory.TryReadPointer(ProcessMemory.Add(staticData, Field.Offset), out value);
        }
    }

    internal sealed class MonoRuntime
    {
        private const ushort StaticFieldAttribute = 0x10;
        private const uint DontResolveDllReferences = 0x00000001;

        private static readonly RuntimeLayout LegacyLayout = new RuntimeLayout(
            0x58, 0x28, 0x3D0, 0x18, 0x20, 0x48, 0xA8, 0x94, 0x30,
            0xF8, 0x5C, 0x100, 0x94, 0x100, 0x08, 0x10, 0x18, 0x00,
            0x08, 0x08, 0x18, -1);

        private static readonly RuntimeLayout LegacyCustomAttributesLayout = new RuntimeLayout(
            0x58, 0x28, 0x3D0, 0x18, 0x20, 0x50, 0xB0, 0x9C, 0x30,
            0x100, 0x64, 0x108, 0x9C, 0x108, 0x08, 0x10, 0x18, 0x00,
            0x08, 0x08, 0x18, -1);

        private static readonly RuntimeLayout ModernLayout = new RuntimeLayout(
            0x60, 0x28, 0x4C0, 0x18, 0x20, 0x48, 0x98, -1, 0x30,
            0xD0, 0x5C, -1, 0x100, 0x108, 0x08, 0x10, 0x18, 0x00,
            0x08, 0x08, -1, 0x40);

        private readonly IDictionary<string, IntPtr> classCache =
            new Dictionary<string, IntPtr>(StringComparer.Ordinal);
        private readonly IDictionary<string, ManagedField> fieldCache =
            new Dictionary<string, ManagedField>(StringComparer.Ordinal);

        private RuntimeLayout layout;
        private int classFieldSize;
        private IntPtr mainImage;

        public MonoRuntime(ProcessMemory memory)
        {
            Memory = memory ?? throw new ArgumentNullException(nameof(memory));
        }

        public ProcessMemory Memory { get; }
        public MonoRuntimeFlavor Flavor { get; private set; }
        public bool IsInitialized => mainImage != IntPtr.Zero;
        public string LastError { get; private set; } = string.Empty;
        public string LayoutName { get; private set; } = string.Empty;

        public bool TryInitialize()
        {
            LastError = string.Empty;
            ProcessModule monoModule = FindMonoModule();
            if (monoModule == null)
            {
                LastError = "Neither mono.dll nor mono-2.0-bdwgc.dll is loaded.";
                return false;
            }

            Flavor = monoModule.ModuleName.Equals("mono.dll", StringComparison.OrdinalIgnoreCase)
                ? MonoRuntimeFlavor.Legacy
                : MonoRuntimeFlavor.Modern;
            layout = Flavor == MonoRuntimeFlavor.Legacy ? LegacyLayout : ModernLayout;
            LayoutName = Flavor == MonoRuntimeFlavor.Legacy ? "Mono v1" : "Mono v2";
            classFieldSize = Align(Memory.PointerSize * 3 + 4, Memory.PointerSize);

            IntPtr assemblyForeach = GetRemoteExportAddress(monoModule, "mono_assembly_foreach");
            if (assemblyForeach == IntPtr.Zero)
            {
                LastError = "Could not resolve the remote mono_assembly_foreach export from "
                    + monoModule.ModuleName + ".";
                return false;
            }

            int candidateCount = 0;
            foreach (IntPtr assemblies in FindAssemblyListCandidates(assemblyForeach))
            {
                candidateCount++;
                IntPtr image = FindAssemblyImage(assemblies, "Assembly-CSharp");
                if (image == IntPtr.Zero)
                    image = FindAssemblyImage(assemblies, "Assembly-CSharp.dll");

                if (image != IntPtr.Zero)
                {
                    if (Flavor == MonoRuntimeFlavor.Legacy && UsesLegacyCustomAttributesLayout(image))
                    {
                        layout = LegacyCustomAttributesLayout;
                        LayoutName = "Mono v1 cattrs";
                    }

                    mainImage = image;
                    classCache.Clear();
                    fieldCache.Clear();
                    return true;
                }
            }

            LastError = candidateCount == 0
                ? "No Mono assembly-list pointer candidates were found in mono_assembly_foreach."
                : "Found " + candidateCount
                    + " Mono assembly-list candidate(s), but none contained Assembly-CSharp.";
            return false;
        }

        private bool UsesLegacyCustomAttributesLayout(IntPtr image)
        {
            IntPtr classCacheAddress = ProcessMemory.Add(image, LegacyLayout.ImageClassCache);
            int bucketCount;
            IntPtr buckets;
            if (!Memory.TryReadInt32(ProcessMemory.Add(classCacheAddress, LegacyLayout.HashSize), out bucketCount)
                || bucketCount <= 0
                || bucketCount > 200000
                || !Memory.TryReadPointer(ProcessMemory.Add(classCacheAddress, LegacyLayout.HashTable), out buckets))
            {
                return false;
            }

            for (int i = 0; i < bucketCount; i++)
            {
                IntPtr managedClass;
                if (!Memory.TryReadPointer(ProcessMemory.Add(buckets, (long)i * Memory.PointerSize), out managedClass)
                    || managedClass == IntPtr.Zero)
                {
                    continue;
                }

                IntPtr normalImage;
                IntPtr customAttributesImage;
                Memory.TryReadPointer(ProcessMemory.Add(managedClass, 0x40), out normalImage);
                Memory.TryReadPointer(ProcessMemory.Add(managedClass, 0x48), out customAttributesImage);
                return customAttributesImage == image && normalImage != image;
            }

            return false;
        }

        public IntPtr FindClass(string className)
        {
            IntPtr cached;
            if (classCache.TryGetValue(className, out cached))
                return cached;

            IntPtr found = FindClass(mainImage, className);
            if (found != IntPtr.Zero)
                classCache[className] = found;
            return found;
        }

        public bool TryGetObjectClass(IntPtr objectAddress, out IntPtr managedClass)
        {
            managedClass = IntPtr.Zero;
            IntPtr vtable;
            return objectAddress != IntPtr.Zero
                && Memory.TryReadPointer(objectAddress, out vtable)
                && vtable != IntPtr.Zero
                && Memory.TryReadPointer(vtable, out managedClass)
                && managedClass != IntPtr.Zero;
        }

        public bool TryFindFieldAny(IntPtr managedClass, IEnumerable<string> names, out ManagedField field)
        {
            foreach (string name in names)
            {
                if (TryFindField(managedClass, name, out field))
                    return true;
            }

            field = null;
            return false;
        }

        public bool TryResolveStaticField(
            string className,
            IEnumerable<string> names,
            out ManagedStaticField staticField)
        {
            staticField = null;
            IntPtr managedClass = FindClass(className);
            if (managedClass == IntPtr.Zero)
                return false;

            ManagedField field;
            if (!TryFindFieldAny(managedClass, names, out field) || !field.IsStatic)
                return false;

            staticField = new ManagedStaticField(this, field);
            return true;
        }

        public bool TryGetStaticData(IntPtr managedClass, out IntPtr staticData)
        {
            staticData = IntPtr.Zero;
            IntPtr runtimeInfo;
            IntPtr classVtable;
            if (!Memory.TryReadPointer(ProcessMemory.Add(managedClass, layout.ClassRuntimeInfo), out runtimeInfo)
                || runtimeInfo == IntPtr.Zero
                || !Memory.TryReadPointer(ProcessMemory.Add(runtimeInfo, layout.RuntimeInfoDomainVtables), out classVtable)
                || classVtable == IntPtr.Zero)
            {
                return false;
            }

            if (Flavor == MonoRuntimeFlavor.Legacy)
            {
                return layout.VtableData >= 0
                    && Memory.TryReadPointer(ProcessMemory.Add(classVtable, layout.VtableData), out staticData)
                    && staticData != IntPtr.Zero;
            }

            int vtableSize;
            if (layout.VtableVtable < 0
                || !Memory.TryReadInt32(ProcessMemory.Add(managedClass, layout.ClassVtableSize), out vtableSize)
                || vtableSize < 0
                || vtableSize > 100000)
            {
                return false;
            }

            IntPtr staticPointer = ProcessMemory.Add(
                classVtable,
                layout.VtableVtable + (long)vtableSize * Memory.PointerSize);
            return Memory.TryReadPointer(staticPointer, out staticData) && staticData != IntPtr.Zero;
        }

        private ProcessModule FindMonoModule()
        {
            try
            {
                // LiveSplit can attach during the first seconds of startup,
                // before Unity loads Mono. Process.Modules is cached on the
                // Process instance, so refresh it on every initialization retry.
                Memory.Process.Refresh();
                ProcessModule modern = Memory.Process.Modules.Cast<ProcessModule>().FirstOrDefault(
                    module => module.ModuleName.Equals("mono-2.0-bdwgc.dll", StringComparison.OrdinalIgnoreCase));
                if (modern != null)
                    return modern;

                return Memory.Process.Modules.Cast<ProcessModule>().FirstOrDefault(
                    module => module.ModuleName.Equals("mono.dll", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        private IEnumerable<IntPtr> FindAssemblyListCandidates(IntPtr assemblyForeach)
        {
            byte[] bytes;
            if (!Memory.TryReadBytes(assemblyForeach, 0x180, out bytes))
                yield break;

            // Different compiler builds choose different registers for the same
            // RIP-relative global. Every candidate is validated by locating the
            // Assembly-CSharp image before it is accepted.
            byte[] registerOpcodes = { 0x0D, 0x15, 0x1D, 0x25, 0x2D, 0x35, 0x3D };
            for (int i = 0; i <= bytes.Length - 7; i++)
            {
                if (bytes[i] != 0x48 || bytes[i + 1] != 0x8B || !registerOpcodes.Contains(bytes[i + 2]))
                    continue;

                int relative = BitConverter.ToInt32(bytes, i + 3);
                IntPtr globalAddress = ProcessMemory.Add(assemblyForeach, i + 7L + relative);
                IntPtr list;
                if (Memory.TryReadPointer(globalAddress, out list) && list != IntPtr.Zero)
                    yield return list;
            }
        }

        private IntPtr FindAssemblyImage(IntPtr assemblies, string wantedName)
        {
            IntPtr node = assemblies;
            for (int guard = 0; node != IntPtr.Zero && guard < 4096; guard++)
            {
                IntPtr assembly;
                IntPtr image;
                IntPtr namePointer;
                if (!Memory.TryReadPointer(ProcessMemory.Add(node, 0x00), out assembly)
                    || assembly == IntPtr.Zero
                    || !Memory.TryReadPointer(ProcessMemory.Add(assembly, layout.AssemblyImage), out image)
                    || image == IntPtr.Zero)
                {
                    break;
                }

                if (Memory.TryReadPointer(ProcessMemory.Add(image, layout.ImageAssemblyName), out namePointer))
                {
                    string currentName = Memory.ReadUtf8String(namePointer, 128);
                    if (currentName.Equals(wantedName, StringComparison.OrdinalIgnoreCase))
                        return image;
                }

                if (!Memory.TryReadPointer(ProcessMemory.Add(node, 0x08), out node))
                    break;
            }

            return IntPtr.Zero;
        }

        private IntPtr FindClass(IntPtr image, string className)
        {
            if (image == IntPtr.Zero)
                return IntPtr.Zero;

            IntPtr classCacheAddress = ProcessMemory.Add(image, layout.ImageClassCache);
            int bucketCount;
            IntPtr buckets;
            if (!Memory.TryReadInt32(ProcessMemory.Add(classCacheAddress, layout.HashSize), out bucketCount)
                || bucketCount <= 0
                || bucketCount > 200000
                || !Memory.TryReadPointer(ProcessMemory.Add(classCacheAddress, layout.HashTable), out buckets)
                || buckets == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            int nextOffset = Flavor == MonoRuntimeFlavor.Modern
                ? layout.ClassDefNextClassCache
                : layout.ClassNextClassCache;

            for (int i = 0; i < bucketCount; i++)
            {
                IntPtr managedClass;
                if (!Memory.TryReadPointer(ProcessMemory.Add(buckets, (long)i * Memory.PointerSize), out managedClass))
                    continue;

                for (int chain = 0; managedClass != IntPtr.Zero && chain < 10000; chain++)
                {
                    IntPtr namePointer;
                    if (Memory.TryReadPointer(ProcessMemory.Add(managedClass, layout.ClassName), out namePointer))
                    {
                        string currentName = Memory.ReadUtf8String(namePointer, 128);
                        if (currentName.Equals(className, StringComparison.Ordinal))
                            return managedClass;
                    }

                    if (nextOffset < 0
                        || !Memory.TryReadPointer(ProcessMemory.Add(managedClass, nextOffset), out managedClass))
                    {
                        break;
                    }
                }
            }

            return IntPtr.Zero;
        }

        private bool TryFindField(IntPtr managedClass, string wantedName, out ManagedField field)
        {
            string cacheKey = managedClass.ToInt64().ToString("X") + ":" + NormalizeName(wantedName);
            if (fieldCache.TryGetValue(cacheKey, out field))
                return field != null;

            IntPtr currentClass = managedClass;
            for (int parent = 0; currentClass != IntPtr.Zero && parent < 32; parent++)
            {
                IntPtr fields;
                int fieldCount;

                if (Memory.TryReadPointer(ProcessMemory.Add(currentClass, layout.ClassFields), out fields)
                    && fields != IntPtr.Zero
                    && TryGetClassFieldCount(currentClass, out fieldCount)
                    && fieldCount > 0
                    && fieldCount < 4000)
                {
                    for (int i = 0; i < fieldCount; i++)
                    {
                        IntPtr fieldAddress = ProcessMemory.Add(fields, (long)i * classFieldSize);
                        IntPtr namePointer;
                        if (!Memory.TryReadPointer(ProcessMemory.Add(fieldAddress, layout.ClassFieldName), out namePointer))
                            continue;

                        string actualName = Memory.ReadUtf8String(namePointer, 128);
                        if (!NamesMatch(actualName, wantedName))
                            continue;

                        int offset;
                        IntPtr declaringClass;
                        IntPtr type;
                        ushort attributes;
                        if (!Memory.TryReadInt32(ProcessMemory.Add(fieldAddress, layout.ClassFieldOffset), out offset)
                            || !Memory.TryReadPointer(ProcessMemory.Add(fieldAddress, layout.ClassFieldParent), out declaringClass)
                            || !Memory.TryReadPointer(ProcessMemory.Add(fieldAddress, layout.ClassFieldType), out type)
                            || !Memory.TryReadUInt16(ProcessMemory.Add(type, layout.MonoTypeAttributes), out attributes))
                        {
                            break;
                        }

                        field = new ManagedField(
                            declaringClass == IntPtr.Zero ? currentClass : declaringClass,
                            offset,
                            (attributes & StaticFieldAttribute) != 0);
                        fieldCache[cacheKey] = field;
                        return true;
                    }
                }

                if (!Memory.TryReadPointer(ProcessMemory.Add(currentClass, layout.ClassParent), out currentClass))
                    break;
            }

            // A MonoClass can enter the image cache before all of its field
            // metadata is inflated. Never cache a miss: classes such as
            // KnownTech and PDAEncyclopedia become readable later while a save
            // is loading, and initialization must recover without reattaching.
            field = null;
            return false;
        }

        private bool TryGetClassFieldCount(IntPtr managedClass, out int fieldCount)
        {
            fieldCount = 0;
            if (Flavor == MonoRuntimeFlavor.Legacy)
            {
                return layout.ClassFieldCount >= 0
                    && Memory.TryReadInt32(
                        ProcessMemory.Add(managedClass, layout.ClassFieldCount),
                        out fieldCount);
            }

            byte classKindValue;
            if (!Memory.TryReadByte(ProcessMemory.Add(managedClass, 0x2A), out classKindValue))
                return false;

            int classKind = classKindValue & 0x07;
            if (classKind == 1 || classKind == 2)
            {
                return Memory.TryReadInt32(
                    ProcessMemory.Add(managedClass, layout.ClassDefFieldCount),
                    out fieldCount);
            }

            if (classKind != 3)
                return false;

            IntPtr genericClass;
            IntPtr containerClass;
            return Memory.TryReadPointer(ProcessMemory.Add(managedClass, 0xF0), out genericClass)
                && genericClass != IntPtr.Zero
                && Memory.TryReadPointer(genericClass, out containerClass)
                && containerClass != IntPtr.Zero
                && TryGetClassFieldCount(containerClass, out fieldCount);
        }

        private static bool NamesMatch(string actual, string expected)
        {
            return string.Equals(actual, expected, StringComparison.Ordinal)
                || string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeName(actual), NormalizeName(expected), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            string normalized = name.Trim();
            if (normalized.StartsWith("m_", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(2);
            else if (normalized.StartsWith("_", StringComparison.Ordinal))
                normalized = normalized.Substring(1);

            return normalized.Replace("_", string.Empty);
        }

        private static int Align(int value, int alignment)
        {
            int remainder = value % alignment;
            return remainder == 0 ? value : value + alignment - remainder;
        }

        private static IntPtr GetRemoteExportAddress(ProcessModule module, string exportName)
        {
            IntPtr localModule = LoadLibraryEx(module.FileName, IntPtr.Zero, DontResolveDllReferences);
            if (localModule == IntPtr.Zero)
                return IntPtr.Zero;

            try
            {
                IntPtr localExport = GetProcAddress(localModule, exportName);
                if (localExport == IntPtr.Zero)
                    return IntPtr.Zero;

                long relative = localExport.ToInt64() - localModule.ToInt64();
                return ProcessMemory.Add(module.BaseAddress, relative);
            }
            finally
            {
                FreeLibrary(localModule);
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string fileName, IntPtr file, uint flags);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string procedureName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr module);

        private sealed class RuntimeLayout
        {
            public RuntimeLayout(
                int assemblyImage,
                int imageAssemblyName,
                int imageClassCache,
                int hashSize,
                int hashTable,
                int className,
                int classFields,
                int classFieldCount,
                int classParent,
                int classRuntimeInfo,
                int classVtableSize,
                int classNextClassCache,
                int classDefFieldCount,
                int classDefNextClassCache,
                int classFieldName,
                int classFieldParent,
                int classFieldOffset,
                int classFieldType,
                int monoTypeAttributes,
                int runtimeInfoDomainVtables,
                int vtableData,
                int vtableVtable)
            {
                AssemblyImage = assemblyImage;
                ImageAssemblyName = imageAssemblyName;
                ImageClassCache = imageClassCache;
                HashSize = hashSize;
                HashTable = hashTable;
                ClassName = className;
                ClassFields = classFields;
                ClassFieldCount = classFieldCount;
                ClassParent = classParent;
                ClassRuntimeInfo = classRuntimeInfo;
                ClassVtableSize = classVtableSize;
                ClassNextClassCache = classNextClassCache;
                ClassDefFieldCount = classDefFieldCount;
                ClassDefNextClassCache = classDefNextClassCache;
                ClassFieldName = classFieldName;
                ClassFieldParent = classFieldParent;
                ClassFieldOffset = classFieldOffset;
                ClassFieldType = classFieldType;
                MonoTypeAttributes = monoTypeAttributes;
                RuntimeInfoDomainVtables = runtimeInfoDomainVtables;
                VtableData = vtableData;
                VtableVtable = vtableVtable;
            }

            public int AssemblyImage { get; }
            public int ImageAssemblyName { get; }
            public int ImageClassCache { get; }
            public int HashSize { get; }
            public int HashTable { get; }
            public int ClassName { get; }
            public int ClassFields { get; }
            public int ClassFieldCount { get; }
            public int ClassParent { get; }
            public int ClassRuntimeInfo { get; }
            public int ClassVtableSize { get; }
            public int ClassNextClassCache { get; }
            public int ClassDefFieldCount { get; }
            public int ClassDefNextClassCache { get; }
            public int ClassFieldName { get; }
            public int ClassFieldParent { get; }
            public int ClassFieldOffset { get; }
            public int ClassFieldType { get; }
            public int MonoTypeAttributes { get; }
            public int RuntimeInfoDomainVtables { get; }
            public int VtableData { get; }
            public int VtableVtable { get; }
        }
    }
}
