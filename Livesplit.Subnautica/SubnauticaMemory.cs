using LiveSplit.Model;
using LiveSplit.VoxSplitter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Livesplit.Subnautica
{
    public class SubnauticaMemory : Memory
    {
        private Pointer<bool> cinematic;

        private bool isReady = true;

        private readonly RemainingDictionary remainingSplits;
        private readonly MonoHelper mono;

        public SubnauticaMemory(LiveSplitState state, Logger logger) : base(state, logger)
        {
            SetProcessNames("Subnautica");
            remainingSplits = new RemainingDictionary(logger);
            mono = new MonoHelper(this);
        }

        public override bool IsReady() => base.IsReady() && mono.IsCompleted;

        protected override void OnHook()
        {
            mono.Run(() => {
                var ptrFactory = new MonoNestedPointerFactory(this, mono);

                cinematic = ptrFactory.Make<bool>("Player", "main", "_cinematicModeActive");

                Logger.Log("ptrFactory: " + ptrFactory.ToString());
            });
        }

        public override bool Update()
        {
            return isReady;
        }

        public override bool Start(int start)
        {
            bool edge = cinematic.Changed && !cinematic.New;
            Logger.Log($"Start? edge={edge}  Old={cinematic.Old} New={cinematic.New} startSetting={start}");
            return edge;
        }

        public override void OnStart(HashSet<string> splits)
        {
            remainingSplits.Setup(splits);
        }

        public override bool Split()
        {
            return false;
        }

        public override bool Loading()
        {
            return false;
        }

        public override void OnExit()
        {
            isReady = false;
        }

        public override void Dispose() => mono.Dispose();
    }
}
