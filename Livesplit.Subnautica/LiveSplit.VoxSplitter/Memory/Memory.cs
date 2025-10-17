using LiveSplit.ComponentUtil;
using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiveSplit.VoxSplitter {
    public abstract class Memory : IDisposable {

        protected LiveSplitState state;

        public Process Game { get; protected set; }
        protected DateTime hookTime;
        
        protected string[] processNames;
        protected void SetProcessNames(params string[] names) => processNames = names;

        public uint Tick { get; private set; } = 1;
        public void IncreaseTick() => ++Tick;

        public delegate void VersionEventHandler(object sender, string version);
        public VersionEventHandler SetVersion { get; set; }

        public Logger Logger { get; }

        protected Memory(LiveSplitState state, Logger logger) {
            this.state = state;
            Logger = logger;
        }

        public virtual bool TryGetGameProcess() {
            if(Game != null) {
                Game = null;
                OnExit();
            }

            if(DateTime.Now < hookTime) { return false; }

            hookTime = DateTime.Now.AddSeconds(1d);

            Process process = Process.GetProcesses()
                .Where(p => processNames.Any(n => p.ProcessName.StartsWith(n, StringComparison.OrdinalIgnoreCase)) && !p.HasExited)
                .FirstOrDefault();

            if(process == null || process.Modules() == null) {
                return false;
            }
            Logger.Log($"Process Found. PID: {process.Id}, 64bit: {process.Is64Bit()}");
            Game = process;
            return true;
        }

        public virtual bool IsReady() {
            if(!Game?.HasExited ?? false) {
                return true;
            }
            if(TryGetGameProcess()) {
                OnHook();
            } else {
                return false;
            }
            return true;
        }

        protected virtual void OnHook() { }
        public virtual bool Update() => true;
        public virtual bool Start(int start) => false;
        public virtual bool Split() => false;
        public virtual bool Reset(int reset) => false;
        public virtual bool Loading() => false;
        public virtual TimeSpan? GameTime() => null;
        public virtual void OnStart(HashSet<string> splits) { }
        public virtual void OnSplit() { }
        public virtual void OnReset() { }
        public virtual void OnExit() { }
        public virtual void Dispose() { }

        public IntPtr FromRelativeAddress(IntPtr asmAddress) => asmAddress + 0x4 + Game.Read<int>(asmAddress);
        public IntPtr FromAbsoluteAddress(IntPtr asmAddress) => Game.Read<IntPtr>(asmAddress);

        public class RemainingHashSet : HashSet<string> {
            protected Logger logger;

            public RemainingHashSet(Logger logger = null) {
                this.logger = logger;
            }

            public bool Split(string split) {
                logger?.Log("Try to split: " + split);
                return Remove(split);
            }
        }

        public class RemainingDictionary : Dictionary<string, HashSet<string>> {
            protected Logger logger;

            public RemainingDictionary(Logger logger = null) {
                this.logger = logger;
            }

            public void Setup(HashSet<string> splits) {
                Clear();

                foreach(string split in splits) {
                    int typeSeparator = split.IndexOf('_');

                    if(typeSeparator != -1) {
                        string type = split.Substring(0, typeSeparator);
                        if(!ContainsKey(type)) {
                            Add(type, new HashSet<string>());
                        }
                        string setting = split.Substring(typeSeparator + 1);
                        this[type].Add(setting);
                    } else {
                        Add(split, null);
                    }
                }
            }

            public bool Split(string type, string setting) {
                logger?.Log("Try to split setting: " + setting);
                if(this[type].Remove(setting)) {
                    if(this[type].Count == 0) {
                        Remove(type);
                    }
                    return true;
                }
                return false;
            }

            public bool Split(string type) {
                logger?.Log("Try to split type: " + type);
                return Remove(type);
            }
        }
    }
}