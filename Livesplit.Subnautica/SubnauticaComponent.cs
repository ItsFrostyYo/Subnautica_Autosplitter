using LiveSplit.Model;
using LiveSplit.Subnautica;
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
        public readonly HashSet<SubnauticaSplit> alreadySplit = new HashSet<SubnauticaSplit>();

        public SubnauticaComponent(LiveSplitState state) : base(state)
        {
#if DEBUG
            logger = new ConsoleLogger();
#else
            logger = new  FileLogger("_" + Factory.ExAssembly.GetName().Name.Substring(10) + ".log");
#endif
            logger.StartLogger();

            Localization.Load();
            _state = state;
            settings = new SubnauticaSettings(state);
            memory = new SubnauticaMemory(state, this, logger, settings);
            timerModel = new TimerModel() { CurrentState = state };
        }

        public override bool Update()
        {
            settings.UpdateExploBtnContent();
            if (!memory.Update() || !memory.pointersInitialized)
                return false;
            TryResetOnMainMenu();
            return true;
        }

        public override bool Start()
        {
            if (memory.startedTimerBefore || !memory.pointersInitialized)
                return false;

            // options: 100 -> 80 health
            if (settings.introStart && (GameModeOption)memory.GameMode.New != GameModeOption.Creative)
            {
                if (memory.DamageEffectsShowing.New && !memory.DamageEffectsShowing.Old) { logger.Log("Start of damageEffectsShowing"); memory.startedTimerBefore = true; return true; }
            }
            if (settings.creativeStart && !memory.isLoadingScreen.Current && !memory.isInMainMenu)
            {
                // Start of Move
                if ((memory.walkDir.Current != 0 && memory.walkDir.Old == 0) || (memory.strafeDir.Current != 0 && memory.strafeDir.Old == 0)) { logger.Log("Start of Move"); memory.startedTimerBefore = true; return true; }

                // Start of Fabricator
                if (memory.isFabiOpen.Current == 1 && memory.isFabiOpen.Old == 0) { logger.Log("Start of Fabricator"); memory.startedTimerBefore = true; return true; }

                // Start of PDA
                if ((PDATab)memory.PDATab.New != PDATab.None && memory.PDATab.Changed) { logger.Log("Start of PDA"); memory.startedTimerBefore = true; return true; }
            }
            return false;
        }

        public override bool Split()
        {
            if (!memory.pointersInitialized)
                return false;

            var splits = settings.Splits;

            for (int i = 0; i < splits.Count; i++)
            {
                if (settings.Ordered && i != alreadySplit.Count)
                    continue;

                var split = splits[i];

                memory.CurrentItemToCheck = InventoryItem.None;
                memory.CurrentBlueprintToCheck = Unlockable.None;
                memory.CurrentEncyEntryToCheck = EncyEntry.None;

                switch (split)
                {
                    case ItemSplit itemSplit:    memory.CurrentItemToCheck      = itemSplit.Item;    break;
                    case BlueprintSplit bpSplit: memory.CurrentBlueprintToCheck = bpSplit.Blueprint; break;
                    case EncySplit encySplit:    memory.CurrentEncyEntryToCheck = encySplit.Entry;   break;
                    default: break;
                }

                if (memory.splitConditions.TryGetValue(split.SplitName, out var condition) && condition() && !(split.OnlySplitOnce && alreadySplit.Contains(split)))
                {
                    alreadySplit.Add(split);
                    logger.Log($"{split.SplitName.GetDescription()} triggered");
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
            if (memory.MainMenu?.New == memory.MainMenu?.Old && memory.MainMenu?.New != IntPtr.Zero)
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

        public override void OnReset()
        {
            alreadySplit.Clear();
        }
    }
}
