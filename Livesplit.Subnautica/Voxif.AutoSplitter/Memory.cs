using System;
using System.Diagnostics;
using Voxif.IO;
using Voxif.Memory;

namespace Voxif.AutoSplitter {
    public abstract class Memory: IDisposable {

        protected abstract string[] ProcessNames { get; }

        protected TickableProcessWrapper game;

        protected Logger logger;
        
        protected DateTime hookTime;

        public Memory(Logger logger) {
            this.logger = logger;
        }

        public virtual bool Update() {
            if(!IsHooked) {
                return false;
            }
            game.IncreaseTick();
            return true;
        }

        protected virtual bool IsHooked => (!game?.Process?.HasExited ?? false) || TryHookProcess();

        protected virtual bool TryHookProcess() {
            if(game != null) {
                game = null;
                OnExit?.Invoke();
            }

            if(DateTime.Now < hookTime) {
                return false;
            }

            hookTime = DateTime.Now.AddSeconds(1d);

            Process process = null;
            foreach(Process p in Process.GetProcesses()) {
                if(process == null) {
                    foreach(string processName in ProcessNames) {
                        if(p.ProcessName.StartsWith(processName, StringComparison.OrdinalIgnoreCase) && !p.HasExited) {
                            process = p;
                        }
                    }
                } else {
                    p.Dispose();
                }
            }

            if(process == null || process.Modules().Length == 0) {
                return false;
            }
            game = new TickableProcessWrapper(process);
            logger?.Log($"Process Found. PID: {game.Process.Id}, {(game.Is64Bit ? "64" : "32")}bit");
            OnHook?.Invoke();
            return true;
        }

        public virtual void Dispose() {
            OnExit?.Invoke();
            game?.Process.Dispose();
        }

        public virtual Action OnHook { get; set; }
        public virtual Action OnExit { get; set; }

        public delegate void OnVersionDetectedCallback(string version);
        public OnVersionDetectedCallback OnVersionDetected { get; set; }
    }
}