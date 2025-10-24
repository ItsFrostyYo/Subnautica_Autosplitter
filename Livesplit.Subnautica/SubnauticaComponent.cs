using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Voxif.AutoSplitter;
using Voxif.IO;

namespace Livesplit.Subnautica
{
    public class SubnauticaComponent : Voxif.AutoSplitter.Component
    {
        private SubnauticaMemory memory;
        private LiveSplitState _state;
        private readonly TimerModel timerModel;
        public SubnauticaComponent(LiveSplitState state) : base(state)
        {
#if DEBUG
            logger = new ConsoleLogger();
#else
            logger = new  FileLogger("_" + Factory.ExAssembly.GetName().Name.Substring(10) + ".log");
#endif
            logger.StartLogger();

            _state = state;
            settings = new SubnauticaSettings(state);
            memory = new SubnauticaMemory(state, logger, settings);
            timerModel = new TimerModel() { CurrentState = state };
        }

        public override bool Update()
        {
            if (!memory.Update())
                return false;
            TryResetOnMainMenu();
            return false;
        }

        public override bool Start()
        {
            if (memory.startedTimerBefore)
                return false;

            if (settings.introStart)
            {
                if (memory.gameVersion == GameVersion.Sept2018 && memory.oxygen.Current == 45 && memory.oxygen.Old < 45) { logger.Log("Start of oxygen"); memory.startedTimerBefore = true; return true; }
                if (!memory.isIntroCinematicActive.New && memory.isIntroCinematicActive.Old) { logger.Log("Start of introCinematic"); memory.startedTimerBefore = true; return true; }
            }
            if (settings.creativeStart && !memory.isLoadingScreen.Current && !memory.isInMainMenu)
            {
                // Start of Move
                if ((memory.walkDir.Current != 0 && memory.walkDir.Old == 0) || (memory.strafeDir.Current != 0 && memory.strafeDir.Old == 0)) { logger.Log("Start of Move"); memory.startedTimerBefore = true; return true; }

                // Start of Fabricator
                if (memory.isFabiOpen.Current == 1 && memory.isFabiOpen.Old == 0) { logger.Log("Start of Fabricator"); memory.startedTimerBefore = true; return true; }

                // Start of PDA
                if (memory.isPDAOpen.Current == 1051931443 && memory.isPDAOpen.Current != memory.isPDAOpen.Old) { logger.Log("Start of PDA"); memory.startedTimerBefore = true; return true; }
            }
            return false;
        }

        public override bool Split()
        {
            if (!memory.pointersInitialized)
                return false;

            foreach (var split in settings.Splits)
            {
                if (memory.splitConditions.TryGetValue(split, out var condition) && condition())
                {
                    memory.alreadySplit.Add(split);
                    logger.Log($"{split} triggered");
                    return true;
                }
            }
            return false;
        }

        public override bool Loading() => memory.ShouldPause();

        private void TryResetOnMainMenu()
        {
            if (!settings.reset)
                return;
            if (memory.mainMenu.New == memory.mainMenu.Old && memory.mainMenu.New != IntPtr.Zero)
                return;
            if (_state.CurrentPhase == TimerPhase.NotRunning)
                return;

            Form ui = _state.Form;
            Action doReset = () =>
            {
                bool GoldSegment = false;
                for (int index = 0; index < _state.Run.Count; index++)
                {
                    if (LiveSplitStateHelper.CheckBestSegment(_state, index, _state.CurrentTimingMethod))
                    {
                        GoldSegment = true;
                        break;
                    }
                }

                bool save = true;
                if (settings.askForGoldSave && GoldSegment)
                {
                    DialogResult r = MessageBox.Show(
                        ui,
                        "Save splits before resetting?",
                        "Reset",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (r == DialogResult.Cancel)
                        return;

                    save = (r == DialogResult.Yes);
                }

                timerModel.Reset(save);
            };

            if (ui.InvokeRequired)
                ui.BeginInvoke(doReset);
            else
                doReset();
        }
    }
}
