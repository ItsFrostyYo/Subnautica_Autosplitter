using LiveSplit.ComponentUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiveSplit.VoxSplitter {
    //WIP Mono helper
    public class MonoHelper : IDisposable {

        public class MonoInformation {
            public bool il2cpp;
            public string assembly;
            public ushort pointer_size;
            public ushort assembly_image;
            public ushort image_name;
            public ushort image_class_offset;
            public ushort image_class_count;
            public ushort image_cache_table;
            public ushort class_name;
            public ushort class_parent;
            public ushort class_fields;
            public ushort class_fields_count;
            public ushort class_basetype;
            public ushort class_generic_class;
            public ushort class_next_class_cache;
            public ushort class_vtable;
            public ushort vtable_static_data;
            public ushort vtable_static_data_offset;
            public ushort field_struct_size;
            public ushort field_name;
            public ushort field_type;
            public ushort field_offset;
            public ushort type_attrs;
        }

        // TODO mono v1 32bit
        //private readonly MonoInformation monoV1_64 = new MonoInformation { il2cpp = false, pointer_size = 0x8, assembly = monoV1Assembly, assembly_image = 0x58, image_name = 0x28, image_class_count = 0x18, image_cache_table = 0x20, class_next_class_cache = 0x100, image_class_offset = 0x3D0, class_name = 0x48, class_vtable = 0xF8, vtable_static_data = 0x10, class_fields_count = 0x094, class_fields = 0xA8, field_struct_size = 0x20, field_name = 0x8, field_offset = 0x18, field_type = 0x0, class_parent = 0x30, type_attrs = 0x8 };
        //2017.4.3f1
        private readonly MonoInformation monoV1_64 = new MonoInformation { il2cpp = false, pointer_size = 0x8, assembly = monoV1Assembly, assembly_image = 0x58, image_name = 0x28, image_class_count = 0x18, image_cache_table = 0x20, class_next_class_cache = 0x108, image_class_offset = 0x3D0, class_name = 0x50, class_vtable = 0x100, vtable_static_data = 0x18, class_fields_count = 0x09C, class_fields = 0xB0, field_struct_size = 0x20, field_name = 0x8, field_offset = 0x18, field_type = 0x0, class_parent = 0x30, type_attrs = 0x8 };

        private readonly MonoInformation monoV2_32 = new MonoInformation { il2cpp = false, pointer_size = 0x4, assembly = monoV2Assembly, assembly_image = 0x44, image_name = 0x18, image_class_count = 0x0C, image_cache_table = 0x14, class_next_class_cache = 0x0A8, image_class_offset = 0x354, class_name = 0x2C, class_vtable = 0x84, vtable_static_data = 0x28, vtable_static_data_offset = 0x38, class_fields_count = 0x0A4, class_fields = 0x60, class_basetype = 0x1E, class_generic_class = 0x94, field_struct_size = 0x10, field_name = 0x4, field_offset = 0x0C, field_type = 0x0, class_parent = 0x20, type_attrs = 0x4 };
        //2018.4.24f1
        private readonly MonoInformation monoV2_64 = new MonoInformation { il2cpp = false, pointer_size = 0x8, assembly = monoV2Assembly, assembly_image = 0x60, image_name = 0x28, image_class_count = 0x18, image_cache_table = 0x20, class_next_class_cache = 0x108, image_class_offset = 0x4C0, class_name = 0x48, class_vtable = 0xD0, vtable_static_data = 0x40, vtable_static_data_offset = 0x5C, class_fields_count = 0x100, class_fields = 0x98, class_basetype = 0x2A, class_generic_class = 0xF0, field_struct_size = 0x20, field_name = 0x8, field_offset = 0x18, field_type = 0x0, class_parent = 0x30, type_attrs = 0x8 };

        // TODO il2cpp 32bit
        //2018.3.11f1
        private readonly MonoInformation il2cpp_64 = new MonoInformation { il2cpp = true, pointer_size = 0x8, assembly = il2cppAssembly, assembly_image = 0x00, image_name = 0x18, image_class_count = 0x1C, image_class_offset = 0x18, class_name = 0x10, class_fields_count = 0x114, class_fields = 0x80, field_struct_size = 0x20, field_name = 0x0, field_offset = 0x18, field_type = 0x8, class_parent = 0x58, type_attrs = 0x8 };

        public MonoInformation MonoInfo { get; protected set; }

        public const string monoV1Assembly = "mono.dll";
        public const string monoV2Assembly = "mono-2.0-bdwgc.dll";
        public const string il2cppAssembly = "GameAssembly.dll";

        private const int FIELD_ATTRIBUTE_STATIC = 0x10;
        private const int TYPE_ATTRIBUTE_VISIBILITY_MASK = 0x07;
        private const int MONO_CLASS_GINST = 0x03;

        protected Memory memory;
        protected Task task;
        protected CancellationTokenSource tokenSource;
        protected CancellationToken token;

        public IntPtr mainImage;
        private IntPtr il2cppTypeInfo;

        public bool IsCompleted => task?.IsCompleted ?? true;

        public MonoHelper(Memory memory) {
            this.memory = memory;
        }

        public void Run(Action action) {
            if(!IsCompleted) {
                tokenSource.Cancel();
                task.Wait();
            }
            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;
            task = Task.Factory.StartNew(() => {
                try {
                    InitMono();
                    action();
                    Log("Task terminated");
                } catch {
                    Log("Task aborted");
                } finally {
                    ExitMono();
                }
            }, token);
        }

        protected void InitMono() {
            while(!token.IsCancellationRequested) {
                string monoName = memory.Game.Modules().FirstOrDefault(
                    m => m.ModuleName.Equals(monoV1Assembly)
                      || m.ModuleName.Equals(monoV2Assembly)
                      || m.ModuleName.Equals(il2cppAssembly))?.ModuleName;

                if(monoName == null) {
                    Sleep();
                    continue;
                }

                if(monoName.StartsWith("mono")) {
                    // TODO                                      Add v1 32bit
                    MonoInfo = monoName.Equals(monoV1Assembly) ? monoV1_64
                                                               : (memory.Game.Is64Bit() ? monoV2_64 : monoV2_32);
                } else {
                    // TODO    Add 32bit
                    MonoInfo = il2cpp_64;
                    SigScanTarget signature = new SigScanTarget(0xB, "48 8D 1C FD ???????? 48 8B 05 ???????? 48 83 3C 03");
                    ProcessModuleWow64Safe module = memory.Game.Modules().First(m => m.ModuleName == il2cppAssembly);
                    SignatureScanner scanner = new SignatureScanner(memory.Game, module.BaseAddress, module.ModuleMemorySize);
                    il2cppTypeInfo = scanner.Scan(signature);
                }

                mainImage = GetModuleImage("Assembly-CSharp");

                break;
            }
        }

        public IntPtr GetModuleImage(string moduleName) {
            Log("Looking for module: " + moduleName);
            while(!token.IsCancellationRequested) {
                IntPtr assemblies;
                IntPtr relativeAsm = MonoInfo.il2cpp ? memory.Game.GetSymbolAddress(MonoInfo.assembly, "il2cpp_domain_get_assemblies") + 0xA
                                                     : memory.Game.GetSymbolAddress(MonoInfo.assembly, "mono_assembly_foreach") + 0x25;
                if(relativeAsm == default) {
                    continue;
                }
                assemblies = memory.Game.Read<IntPtr>(memory.FromRelativeAddress(relativeAsm));
                while(true) {
                    IntPtr image = GetImage(assemblies);
                    string assemblyName = GetImageName(image);
                    if(assemblyName.Equals(moduleName)) {
                        Log("Module " + image.ToString("X") + " " + assemblyName);
                        return MonoInfo.il2cpp ? memory.Game.Read<IntPtr>(image) : image;
                    }
                    if(MonoInfo.il2cpp) {
                        assemblies += MonoInfo.pointer_size;
                        if(memory.Game.Read<IntPtr>(assemblies) == default) {
                            Sleep();
                            break;
                        }
                    } else {
                        assemblies = memory.Game.Read<IntPtr>(assemblies + MonoInfo.pointer_size);
                        if(assemblies == default) {
                            Sleep();
                            break;
                        }
                    }
                }
            }
            return default;
        }

        public IntPtr GetClass(IntPtr image, string classToFind) {
            Log("Looking for class: " + classToFind);
            while(!token.IsCancellationRequested) {
                if(MonoInfo.il2cpp) {
                    int offset = GetClassOffsetCpp(image);
                    int count = GetClassCount(image);
                    IntPtr table = memory.Game.Read<IntPtr>(memory.FromRelativeAddress(il2cppTypeInfo));
                    for(int i = 0; i < count; i++) {
                        IntPtr klass = GetClass(table, offset + i);
                        if(ClassFound(klass, classToFind)) {
                            return klass;
                        }
                    }
                } else {
                    IntPtr offset = GetClassOffsetMono(image);
                    int count = GetClassCount(offset);
                    IntPtr table = GetCacheTable(offset);
                    for(int i = 0; i < count; i++) {
                        IntPtr klass = GetClass(table, i);
                        while(klass != default) {
                            if(ClassFound(klass, classToFind)) {
                                return klass;
                            }
                            klass = GetNextClassCache(klass);
                        }
                    }
                }
                Sleep();
            }
            return default;
        }

        protected bool ClassFound(IntPtr klass, string classToFind) {
            string className = GetClassName(klass);
            if(!classToFind.Equals(className)) {
                return false;
            }
            Log("Class " + klass.ToString("X") + " " + className);
            return true;
        }

        public IntPtr GetStaticAddress(IntPtr klass) {
            return GetStaticField(klass, null, out _);
        }

        public IntPtr GetStaticField(IntPtr image, string klassName, string fieldName, out IntPtr klass, out int staticOffset, bool includeParents = true) {
            klass = GetClass(image, klassName);
            return GetStaticField(klass, fieldName, out staticOffset, includeParents);
        }

        public IntPtr GetStaticField(IntPtr klass, string fieldName, out int staticOffset, bool includeParents = true) {
            Log("Looking for static field: " + fieldName);
            staticOffset = 0;
            while(!token.IsCancellationRequested) {
                foreach(IntPtr field in FieldSequence(klass, includeParents)) {
                    if(!FieldIsStatic(field)) {
                        continue;
                    }
                    if(!String.IsNullOrEmpty(fieldName)) {
                        string name = GetFieldName(field);
                        if(!name.Equals(fieldName)) {
                            continue;
                        }
                        staticOffset = GetFieldOffset(field);
                    }
                    IntPtr staticKlass = MonoInfo.il2cpp ? field : klass; ;
                    Log("Static " + staticKlass.ToString("X") + " " + staticOffset.ToString("X"));
                    return staticKlass;
                }
                Sleep();
            }
            return default;
        }

        public int GetFieldOffset(IntPtr klass, string fieldName, bool includeParents = true) {
            foreach(IntPtr field in FieldSequence(klass, includeParents)) {
                string name = GetFieldName(field);
                if(!name.Equals(fieldName)) {
                    continue;
                }
                int offset = GetFieldOffset(field);
                Log("Field " + offset.ToString("X") + " " + name);
                return offset;
            }
            return default;
        }

        public IEnumerable<IntPtr> FieldSequence(IntPtr klass, bool includeParents) {
            while(klass != default) {
                if(HasFields(klass, out IntPtr fieldsPtr)) {
                    int fieldCount = GetFieldCount(klass);
                    for(int i = 0; i < fieldCount; i++) {
                        yield return fieldsPtr + MonoInfo.field_struct_size * i;
                    }
                }
                if(includeParents) {
                    klass = GetParent(klass);
                } else {
                    break;
                }
            }
        }

        public IntPtr GetImage(IntPtr module) {
            if(!MonoInfo.il2cpp) { module = memory.Game.Read<IntPtr>(module); }
            return memory.Game.Read<IntPtr>(module + MonoInfo.assembly_image);
        }
        public string GetImageName(IntPtr image) => GetName(image + MonoInfo.image_name);
        public string GetClassName(IntPtr klass) => GetName(klass + MonoInfo.class_name);
        public string GetFieldName(IntPtr field) => GetName(field + MonoInfo.field_name);
        public string GetName(IntPtr ptr) => memory.Game.ReadString(memory.Game.Read<IntPtr>(ptr), EStringType.Auto);
        public IntPtr GetClass(IntPtr table, int offset) => memory.Game.Read<IntPtr>(table + offset * MonoInfo.pointer_size);
        public IntPtr GetClassOffsetMono(IntPtr image) => image + MonoInfo.image_class_offset;
        public int GetClassOffsetCpp(IntPtr image) => memory.Game.ReadValue<int>(image + MonoInfo.image_class_offset);
        public int GetClassCount(IntPtr image) => memory.Game.ReadValue<int>(image + MonoInfo.image_class_count);
        public IntPtr GetCacheTable(IntPtr cache) => memory.Game.Read<IntPtr>(cache + MonoInfo.image_cache_table);
        public IntPtr GetParent(IntPtr klass) => memory.Game.Read<IntPtr>(klass + MonoInfo.class_parent);
        public bool HasParent(IntPtr klass, out IntPtr parent) => memory.Game.ReadPointer(klass + MonoInfo.class_parent, out parent);
        public int GetFieldCount(IntPtr klass) {
            IntPtr baseKlass = klass;
            if(MonoInfo.assembly == monoV2Assembly) {
                while((memory.Game.ReadValue<byte>(baseKlass + MonoInfo.class_basetype) & TYPE_ATTRIBUTE_VISIBILITY_MASK) == MONO_CLASS_GINST) {
                    baseKlass = GetGenericClass(klass);
                }
            }
            return memory.Game.ReadValue<int>(baseKlass + MonoInfo.class_fields_count);
        }
        public IntPtr GetGenericClass(IntPtr klass) => memory.Game.Read<IntPtr>(memory.Game.Read<IntPtr>(klass + MonoInfo.class_generic_class));
        public bool HasFields(IntPtr klass, out IntPtr field) => memory.Game.ReadPointer(klass + MonoInfo.class_fields, out field);
        public bool FieldIsStatic(IntPtr field) => (GetTypeAttributes(GetType(field)) & FIELD_ATTRIBUTE_STATIC) != 0;
        public int GetFieldOffset(IntPtr field) => memory.Game.ReadValue<int>(field + MonoInfo.field_offset);
        public ushort GetTypeAttributes(IntPtr type) => memory.Game.ReadValue<ushort>(type + MonoInfo.type_attrs);
        public IntPtr GetNextClassCache(IntPtr klass) => memory.Game.Read<IntPtr>(klass + MonoInfo.class_next_class_cache);
        public IntPtr GetVTable(IntPtr klass) => memory.Game.Read<IntPtr>(memory.Game.Read<IntPtr>(klass + MonoInfo.class_vtable) + MonoInfo.pointer_size);
        public IntPtr GetType(IntPtr field) => memory.Game.Read<IntPtr>(field + MonoInfo.field_type);
        public IntPtr GetStaticData(IntPtr data) {
            if(MonoInfo.il2cpp) {
                return memory.Game.Read<IntPtr>(memory.Game.Read<IntPtr>(data + 0x10) + 0xB8);
            } else {
                return MonoInfo.assembly == monoV1Assembly
                    ? memory.Game.Read<IntPtr>(GetVTable(data) + MonoInfo.vtable_static_data)
                    : memory.Game.Read<IntPtr>(GetVTable(data) + MonoInfo.vtable_static_data + memory.Game.ReadValue<int>(data + MonoInfo.vtable_static_data_offset) * MonoInfo.pointer_size);
            }
        }

        protected void Sleep(int msTimeout = 50) => Thread.Sleep(msTimeout);

        protected void Log(string msg) => memory.Logger.Log("[Mono] " + msg);

        protected void ExitMono() {
            Log("Exit");
            tokenSource?.Cancel();
        }

        public void Dispose() => ExitMono();
    }

    public class MonoNestedPointerFactory : NestedPointerFactory {

        protected MonoHelper mono;

        public MonoNestedPointerFactory(Memory memory, MonoHelper monoHelper) : this(memory, null, monoHelper, EDerefType.Auto) { }
        public MonoNestedPointerFactory(Memory memory, string moduleName, MonoHelper monoHelper) : this(memory, moduleName, monoHelper, EDerefType.Auto) { }
        public MonoNestedPointerFactory(Memory memory, string moduleName, MonoHelper monoHelper, EDerefType derefType) : base(memory, moduleName, derefType) {
            mono = monoHelper;
        }


        public Pointer<T> Make<T>(string klassName, string instanceName, params int[] offsets) where T : unmanaged {
            return Make<T>(mono.mainImage, klassName, instanceName, out _, offsets);
        }
        public Pointer<T> Make<T>(string klassName, string instanceName, out IntPtr klass, params int[] offsets) where T : unmanaged {
            return Make<T>(mono.mainImage, klassName, instanceName, out klass, offsets);
        }
        public Pointer<T> Make<T>(IntPtr image, string klassName, string instanceName, params int[] offsets) where T : unmanaged {
            return Make<T>(image, klassName, instanceName, out _, offsets);
        }
        public Pointer<T> Make<T>(IntPtr image, string klassName, string instanceName, out IntPtr klass, params int[] offsets) where T : unmanaged {
            return (Pointer<T>)Make(typeof(T), image, klassName, instanceName, out klass, offsets);
        }

        public Pointer<T> Make<T>(string klassName, string instanceName, string fieldName, params int[] offsets) where T : unmanaged {
            return Make<T>(mono.mainImage, klassName, instanceName, fieldName, offsets);
        }
        public Pointer<T> Make<T>(IntPtr image, string klassName, string instanceName, string fieldName, params int[] offsets) where T : unmanaged {
            return (Pointer<T>)Make(typeof(T), image, klassName, instanceName, fieldName, offsets);
        }


        public StringPointer MakeString(string klassName, string instanceName, params int[] offsets) {
            return MakeString(mono.mainImage, klassName, instanceName, out _, offsets);
        }
        public StringPointer MakeString(string klassName, string instanceName, out IntPtr klass, params int[] offsets) {
            return MakeString(mono.mainImage, klassName, instanceName, out klass, offsets);
        }
        public StringPointer MakeString(IntPtr image, string klassName, string instanceName, params int[] offsets) {
            return MakeString(image, klassName, instanceName, out _, offsets);
        }
        public StringPointer MakeString(IntPtr image, string klassName, string instanceName, out IntPtr klass, params int[] offsets) {
            return (StringPointer)Make(typeof(string), image, klassName, instanceName, out klass, offsets);
        }

        public StringPointer MakeString(string klassName, string instanceName, string fieldName, params int[] offsets) {
            return MakeString(mono.mainImage, klassName, instanceName, fieldName, offsets);
        }
        public StringPointer MakeString(IntPtr image, string klassName, string instanceName, string fieldName, params int[] offsets) {
            return (StringPointer)Make(typeof(string), image, klassName, instanceName, fieldName, offsets);
        }


        public Pointer Make(Type type, IntPtr image, string klassName, string instanceName, out IntPtr klass, params int[] offsets) {
            IntPtr staticBase = mono.GetStaticField(image, klassName, instanceName, out klass, out int instanceOffset);
            return CreateBase(type, staticBase, offsets.Prepend(instanceOffset).ToArray());
        }
        public Pointer Make(Type type, IntPtr image, string klassName, string instanceName, string fieldName, params int[] offsets) {
            IntPtr staticBase = mono.GetStaticField(image, klassName, instanceName, out IntPtr klass, out int instanceOffset);
            return CreateBase(type, staticBase, offsets.Prepend(mono.GetFieldOffset(klass, fieldName)).Prepend(instanceOffset).ToArray());
        }
        protected Pointer CreateBase(Type type, IntPtr ptr, params int[] offsets) {
            MonoBasePointer monoBase = new MonoBasePointer(memory, mono, ptr);
            monoBase.ForceUpdate();
            Pointer pointer = (Pointer)CreateNodeStructOrString(type, monoBase, derefType, offsets);
            pointer.ForceUpdate();
            nodeLink.Add(monoBase, new HashSet<IPointer> { pointer });
            return pointer;
        }
    }

    public class MonoBasePointer : Pointer<IntPtr>, IBasePointer {
        public IntPtr Base { get; protected set; }

        protected MonoHelper mono;

        public MonoBasePointer(Memory memory, MonoHelper mono, IntPtr basePtr) : this(memory, mono, basePtr, EDerefType.Auto) { }
        public MonoBasePointer(Memory memory, MonoHelper mono, IntPtr basePtr, EDerefType derefType) : base(memory, derefType) {
            Base = basePtr;
            this.mono = mono;
        }

        protected override void Update() {
            Old = (IntPtr)(newValue ?? default(IntPtr));
            New = mono.GetStaticData(Base);
        }

        protected override IntPtr DerefOffsets() => throw new NotImplementedException();
    }
}