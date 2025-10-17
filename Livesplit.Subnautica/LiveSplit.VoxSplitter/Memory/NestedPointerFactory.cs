using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiveSplit.VoxSplitter {
    public class NestedPointerFactory {

        protected Memory memory;
        protected EDerefType derefType;
        protected IntPtr moduleBase;

        protected Dictionary<IPointer, HashSet<IPointer>> nodeLink = new Dictionary<IPointer, HashSet<IPointer>>();

        public NestedPointerFactory(Memory memory) : this(memory, null, EDerefType.Auto) { }
        public NestedPointerFactory(Memory memory, string moduleName) : this(memory, moduleName, EDerefType.Auto) { }
        public NestedPointerFactory(Memory memory, string moduleName, EDerefType derefType) {
            this.memory = memory;
            this.derefType = derefType;
            if(moduleName == null) {
                moduleBase = memory.Game.Modules()[0].BaseAddress;
            } else {
                moduleBase = memory.Game.Modules().FirstOrDefault(
                    m => m.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))?.BaseAddress ?? default;
            }
        }

        protected HashSet<IPointer> BasePointers() => new HashSet<IPointer>(nodeLink.Keys.Where(n => n is IBasePointer));

        public Pointer<T> Make<T>(int moduleOffset, params int[] offsets) where T : unmanaged {
            return Make<T>(derefType, moduleBase + moduleOffset, offsets);
        }
        public Pointer<T> Make<T>(IntPtr basePtr, params int[] offsets) where T : unmanaged {
            return Make<T>(derefType, basePtr, offsets);
        }
        public Pointer<T> Make<T>(EDerefType derefType, IntPtr basePtr, params int[] offsets) where T : unmanaged {
            Pointer<T> pointer = (Pointer<T>)Make(typeof(T), derefType, basePtr, offsets);
            pointer.ForceUpdate();
            return pointer;
        }

        public StringPointer MakeString(int moduleOffset, params int[] offsets) {
            return MakeString(derefType, moduleBase + moduleOffset, offsets);
        }
        public StringPointer MakeString(IntPtr basePtr, params int[] offsets) {
            return MakeString(derefType, basePtr, offsets);
        }
        public StringPointer MakeString(EDerefType derefType, IntPtr basePtr, params int[] offsets) {
            StringPointer pointer = (StringPointer)Make(typeof(string), derefType, basePtr, offsets);
            pointer.ForceUpdate();
            return pointer;
        }

        public Pointer<T> Make<T>(Pointer parent, params int[] offsets) where T : unmanaged {
            return Make<T>(derefType, parent, offsets);
        }
        public Pointer<T> Make<T>(EDerefType derefType, Pointer parent, params int[] offsets) where T : unmanaged {
            Pointer<T> pointer = (Pointer<T>)Make(typeof(T), derefType, parent, offsets);
            pointer.ForceUpdate();
            return pointer;
        }

        public StringPointer MakeString(Pointer parent, params int[] offsets) {
            return MakeString(derefType, parent, offsets);
        }
        public StringPointer MakeString(EDerefType derefType, Pointer parent, params int[] offsets) {
            StringPointer pointer = (StringPointer)Make(typeof(string), derefType, parent, offsets);
            pointer.ForceUpdate();
            return pointer;
        }

        protected IPointer Make(Type type, EDerefType derefType, IntPtr basePtr, params int[] offsets) {
            IPointer pointer;
            foreach(IBasePointer basePointer in BasePointers()) {
                if(basePointer.Base != basePtr) {
                    continue;
                }

                if(offsets.Length == 0 || basePointer.Offsets.SequenceEqual(offsets)) {
                    return basePointer;
                }

                for(int i = 0; i < basePointer.Offsets.Length; i++) {
                    if(i < offsets.Length && basePointer.Offsets[i] == offsets[i]) {
                        continue;
                    }

                    BasePointer<IntPtr> newBase = new BasePointer<IntPtr>(memory, basePointer.Base, derefType, basePointer.Offsets.Take(i).ToArray());
                    Type baseType = basePointer.GetType().IsGenericType ? basePointer.GetType().GenericTypeArguments[0] : typeof(string);
                    INodePointer nodeBase = CreateNodeStructOrString(baseType, newBase, derefType, basePointer.Offsets.Skip(i).ToArray());
                    nodeLink.Add(nodeBase, new HashSet<IPointer>());
                    foreach(INodePointer node in nodeLink[basePointer]) {
                        node.Parent = nodeBase;
                        nodeLink[nodeBase].Add(node);
                    }
                    nodeLink.Remove(basePointer);
                    if(i != offsets.Length) {
                        INodePointer newNode = CreateNodeStructOrString(type, newBase, derefType, offsets.Skip(i).ToArray());
                        nodeLink.Add(newBase, new HashSet<IPointer> { nodeBase, newNode });
                        return newNode;
                    } else {
                        nodeLink.Add(newBase, new HashSet<IPointer> { nodeBase });
                        return newBase;
                    }
                }

                pointer = AddPointer(type, nodeLink[basePointer], derefType, offsets.Skip(basePointer.Offsets.Length).ToArray());
                if(pointer == null) {
                    pointer = CreateNodeStructOrString(type, basePointer, derefType, offsets.Skip(basePointer.Offsets.Length).ToArray());
                    nodeLink[basePointer].Add(pointer);
                }
                return pointer;
            }
            IBasePointer newBasePointer = CreateBaseStructOrString(type, basePtr, derefType, offsets);
            nodeLink.Add(newBasePointer, new HashSet<IPointer>());
            return newBasePointer;
        }

        protected IPointer Make(Type type, EDerefType derefType, IPointer parent, params int[] offsets) {
            IPointer pointer = null;
            if(nodeLink.TryGetValue(parent, out HashSet<IPointer> value)) {
                pointer = AddPointer(type, value, derefType, offsets);
            } else {
                nodeLink.Add(parent, new HashSet<IPointer>());
            }
            if(pointer == null) {
                pointer = CreateNodeStructOrString(type, parent, derefType, offsets);
                nodeLink[parent].Add(pointer);
            }
            return pointer;
        }

        protected IPointer AddPointer(Type type, HashSet<IPointer> pointers, EDerefType derefType, int[] offsets) {
            IPointer newNode = null;
            foreach(INodePointer ptr in pointers) {
                if(ptr.Offsets[0] != offsets[0]) {
                    continue;
                }
                if(ptr.Offsets.SequenceEqual(offsets)) {
                    return ptr;
                }
                for(int i = 1; i < ptr.Offsets.Length; i++) {
                    if(i < offsets.Length && ptr.Offsets[i] == offsets[i]) {
                        continue;
                    }
                    NodePointer<IntPtr> newParent = new NodePointer<IntPtr>(memory, ptr.Parent, derefType, ptr.Offsets.Take(i).ToArray());
                    pointers.Remove(ptr);
                    pointers.Add(newParent);
                    ptr.Parent = newParent;
                    ptr.Offsets = ptr.Offsets.Skip(i).ToArray();
                    if(i != offsets.Length) {
                        newNode = CreateNodeStructOrString(type, newParent, derefType, offsets.Skip(i).ToArray());
                        nodeLink.Add(newParent, new HashSet<IPointer> { ptr, newNode });
                        return newNode;
                    } else {
                        nodeLink.Add(newParent, new HashSet<IPointer> { ptr });
                        return newParent;
                    }
                }
                if(nodeLink.ContainsKey(ptr) && (newNode = AddPointer(type, nodeLink[ptr], derefType, offsets.Skip(ptr.Offsets.Length).ToArray())) != null) {
                    return newNode;
                }
                newNode = CreateNodeStructOrString(type, ptr, derefType, offsets.Skip(ptr.Offsets.Length).ToArray());
                if(nodeLink.ContainsKey(ptr)) {
                    nodeLink[ptr].Add(newNode);
                } else {
                    nodeLink[ptr] = new HashSet<IPointer> { newNode };
                }
            }
            return newNode;
        }

        protected IBasePointer CreateBaseStructOrString(Type type, IntPtr basePointer, EDerefType derefType, params int[] offsets) {
            return type == typeof(string)
                 ? new BaseStringPointer(memory, basePointer, derefType, offsets)
                 : (IBasePointer)Activator.CreateInstance(typeof(BasePointer<>).MakeGenericType(type), new object[] { memory, basePointer, derefType, offsets });
        }

        protected INodePointer CreateNodeStructOrString(Type type, IPointer basePointer, EDerefType derefType, params int[] offsets) {
            return type == typeof(string)
                 ? new NodeStringPointer(memory, basePointer, derefType, offsets)
                 : (INodePointer)Activator.CreateInstance(typeof(NodePointer<>).MakeGenericType(type), new object[] { memory, basePointer, derefType, offsets });
        }

        protected string NodeToString(Pointer pointer) {
            string str = Environment.NewLine + new string(' ', 19 + GetTreeDepth(pointer) * 4) + pointer.OffsetsToString();

            if(nodeLink.ContainsKey(pointer)) {
                foreach(Pointer subNode in nodeLink[pointer]) {
                    str += NodeToString(subNode);
                }
            }
            return str;
        }

        public int GetTreeDepth(IPointer pointer) => pointer is INodePointer node ? 1 + GetTreeDepth(node.Parent) : 0;

        public override string ToString() {
            string str = "";
            foreach(IBasePointer baseNode in BasePointers()) {
                str += Environment.NewLine + "0x" + baseNode.Base.ToString("X16");
                str += " " + ((Pointer)baseNode).OffsetsToString();
                foreach(Pointer node in nodeLink[baseNode].OrderBy(n => n.Offsets[0])) {
                    str += NodeToString(node);
                }
            }
            return str;
        }
    }

    public interface IPointer {
        int[] Offsets { get; set; }
        object Old { get; set; }
        object New { get; set; }
    }

    public enum EDerefType { Auto, Bit32, Bit64 };

    public abstract class Pointer : IPointer {

        protected Memory memory;

        protected uint lastTickUpdate = 0;

        protected object oldValue;
        public object Old {
            get {
                if(lastTickUpdate != memory.Tick) {
                    lastTickUpdate = memory.Tick;
                    Update();
                }
                return oldValue;
            }
            set => oldValue = value;
        }

        protected object newValue;
        public object New {
            get {
                if(lastTickUpdate != memory.Tick) {
                    lastTickUpdate = memory.Tick;
                    Update();
                }
                return newValue;
            }
            set => newValue = value;
        }

        public bool Changed => !Old.Equals(newValue);

        public int[] Offsets { get; set; }

        public EDerefType derefType;

        public Pointer(Memory memory, EDerefType derefType, params int[] offsets) {
            this.memory = memory;
            this.derefType = derefType;
            Offsets = offsets;
        }

        protected abstract IntPtr DerefOffsets();

        protected abstract void Update();

        public string OffsetsToString() {
            if(Offsets.Length == 0) {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("[0x").Append(Offsets[0].ToString("X"));
            for(int i = 1; i < Offsets.Length; i++) {
                sb.Append(", 0x").Append(Offsets[i].ToString("X"));
            }
            return sb.Append(']').ToString();
        }

        public void ForceUpdate() {
            Update();
        }
    }

    public abstract class Pointer<T> : Pointer where T : unmanaged {

        public new T Old {
            get => (T)base.Old;
            set => base.Old = value;
        }
        public new T New {
            get => (T)base.New;
            set => base.New = value;
        }

        public Pointer(Memory memory, EDerefType derefType, params int[] offsets) : base(memory, derefType, offsets) { }

        protected override void Update() {
            Old = (T)(newValue ?? default(T));
            New = memory.Game.Read<T>(DerefOffsets(), derefType);
        }
    }

    public abstract class StringPointer : Pointer {

        public EStringType StringType { get; set; }

        public new string Old {
            get => (string)base.Old;
            set => base.Old = value;
        }
        public new string New {
            get => (string)base.New;
            set => base.New = value;
        }

        public StringPointer(Memory memory, EDerefType derefType, params int[] offsets) : base(memory, derefType, offsets) { }

        protected override void Update() {
            Old = (string)(newValue ?? default(string));
            New = memory.Game.ReadString(DerefOffsets(), StringType);
        }
    }

    public interface IBasePointer : IPointer {
        IntPtr Base { get; }
    }

    public class BasePointer<T> : Pointer<T>, IBasePointer where T : unmanaged {
        public IntPtr Base { get; protected set; }

        public BasePointer(Memory memory, IntPtr basePtr, params int[] offsets) : this(memory, basePtr, EDerefType.Auto, offsets) { }
        public BasePointer(Memory memory, IntPtr basePtr, EDerefType derefType, params int[] offsets) : base(memory, derefType, offsets) {
            Base = basePtr;
        }

        protected override IntPtr DerefOffsets() {
            return memory.Game.DerefOffsets(derefType, Offsets.Length > 0 ? memory.Game.Read<IntPtr>(Base) : Base, Offsets);
        }
    }

    public class BaseStringPointer : StringPointer, IBasePointer {
        public IntPtr Base { get; protected set; }

        public BaseStringPointer(Memory memory, IntPtr basePtr, params int[] offsets) : this(memory, basePtr, EDerefType.Auto, offsets) { }
        public BaseStringPointer(Memory memory, IntPtr basePtr, EDerefType derefType, params int[] offsets) : base(memory, derefType, offsets) {
            Base = basePtr;
        }

        protected override IntPtr DerefOffsets() {
            return memory.Game.DerefOffsets(derefType, Offsets.Length > 0 ? memory.Game.Read<IntPtr>(Base) : Base, Offsets);
        }
    }

    public interface INodePointer : IPointer {
        IPointer Parent { get; set; }
    }

    public class NodePointer<T> : Pointer<T>, INodePointer where T : unmanaged {

        public IPointer Parent { get; set; }

        public NodePointer(Memory memory, IPointer parent, params int[] offsets) : this(memory, parent, EDerefType.Auto, offsets) { }
        public NodePointer(Memory memory, IPointer parent, EDerefType derefType, params int[] offsets) : base(memory, derefType, offsets) {
            Parent = parent;
        }

        protected override IntPtr DerefOffsets() {
            return memory.Game.DerefOffsets(derefType, (IntPtr)Parent.New, Offsets);
        }
    }

    public enum EStringType { Auto, UTF8, UTF8Sized, UTF16, UTF16Sized }

    public class NodeStringPointer : StringPointer, INodePointer {

        public IPointer Parent { get; set; }

        public NodeStringPointer(Memory memory, IPointer parent, params int[] offsets) : this(memory, parent, EDerefType.Auto, offsets) { }
        public NodeStringPointer(Memory memory, IPointer parent, EDerefType derefType, params int[] offsets) : base(memory, derefType, offsets) {
            Parent = parent;
        }

        protected override IntPtr DerefOffsets() {
            return memory.Game.DerefOffsets(derefType, (IntPtr)Parent.New, Offsets);
        }
    }
}