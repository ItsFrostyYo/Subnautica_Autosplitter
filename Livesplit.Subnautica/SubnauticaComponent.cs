using LiveSplit.Model;
using LiveSplit.Options;
using LiveSplit.Subnautica;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Voxif.AutoSplitter;
using Voxif.IO;

namespace LiveSplit.Subnautica
{
    public class SubnauticaComponent : Voxif.AutoSplitter.Component
    {
        private readonly SubnauticaMemory memory;
        private readonly LiveSplitState _state;
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
            UpdateExploTime();

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
            if (settings.IntroStart && (GameModeOption)memory.GameMode.New != GameModeOption.Creative)
            {
                if (memory.DamageEffectsShowing.New && !memory.DamageEffectsShowing.Old) { logger.Log("Start of damageEffectsShowing"); memory.startedTimerBefore = true; return true; }
            }
            if (settings.CreativeStart && !memory.IsLoadingScreenShowing.New && !memory.IsIntroCinematicActive.New && !memory.isInMainMenu)
            {
                // Start of Move
                if ((memory.walkDir.Current != 0 && memory.walkDir.Old == 0) || (memory.strafeDir.Current != 0 && memory.strafeDir.Old == 0)) { logger.Log("Start of Move"); memory.startedTimerBefore = true; return true; }

                // Start of Jump
                if (memory.IsPlayerJumping.New && memory.IsPlayerJumping.Changed) { logger.Log("Start of Jump"); memory.startedTimerBefore = true; return true; }

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
                if ((SubnauticaSettings.OrderedAutoSplits && i != alreadySplit.Count) || (SubnauticaSettings.OrderedLiveSplit && i != _state.CurrentSplitIndex))
                    continue;

                var split = splits[i];

                memory.Checks = Checks.CreateDefault();

                IEnumerable<SubnauticaSplit> conditionsSplits = GetAllConditions(split);
                bool allConditionsMet = true;

                foreach (var conditionSplit in conditionsSplits)
                {
                    SetCheckObjects(conditionSplit);
                    if (memory.subConditions.TryGetValue(conditionSplit.SplitName, out var subCondition) && !subCondition())
                    {
                        allConditionsMet = false;
                        break;
                    }
                }

                SetCheckObjects(split);
                if (allConditionsMet 
                    && memory.splitConditions.TryGetValue(split.SplitName, out var condition) 
                    && condition()
                    && !(split.OnlySplitOnce && !SubnauticaSettings.OrderedAutoSplits && !SubnauticaSettings.OrderedLiveSplit && alreadySplit.Contains(split)))
                {
                    alreadySplit.Add(split);
                    logger.Log($"{split.GetDescription()} triggered");
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<SubnauticaSplit> GetAllConditions(SubnauticaSplit split)
        {
            if (split?.Conditions == null)
                yield break;

            foreach (var c in split.Conditions.Where(c => c.IsSubCondition))
            {
                yield return c;

                foreach (var nested in GetAllConditions(c))
                    yield return nested;
            }
        }

        private void SetCheckObjects(SubnauticaSplit split)
        {
            switch (split)
            {
                case ItemSplit itemSplit:
                    memory.Checks.InvChecks.Item = itemSplit.Item;
                    memory.Checks.InvChecks.Count = itemSplit.Count;
                    memory.Checks.InvChecks.IsCount = itemSplit.IsCount;
                    memory.Checks.InvChecks.Pickup = itemSplit.PickUp;
                    break;
                case BlueprintSplit bpSplit: memory.Checks.Blueprint = bpSplit.Blueprint; break;
                case EncySplit encySplit: memory.Checks.EncyEntry = encySplit.Entry; break;
                case BiomeSplit biomeSplit: memory.Checks.Biomes = biomeSplit.Biomes; break;
                case CraftSplit craftSplit: memory.Checks.Craftable = craftSplit.Craftable; break;
                default: break;
            }
        }

        public override bool Loading() => memory.ShouldPause();

        private void TryResetOnMainMenu()
        {
            if (!settings.Reset)
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
                if (settings.AskForGoldSave && GoldSegment)
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

        private void UpdateExploTime()
        {
            string text = TimeSpan.FromSeconds(0).ToString(@"h\:mm\:ss");

            if (memory.pointersInitialized)
            {
                float explosionTimeFloat = memory.TimeToStartCountdown.New - memory.TimeToStartWarning.New;
                TimeSpan explosionTime = TimeSpan.FromSeconds(explosionTimeFloat);
                text = explosionTime.ToString(@"h\:mm\:ss");
            }

            settings.UpdateTextComponent("Explosion Time", text);
            settings.UpdateExploBtnContent();
        }
    }

    public struct Checks
    {
        public InvChecks InvChecks;
        public Unlockable Blueprint;
        public EncyEntry EncyEntry;
        public (Biome Biome1, Biome Biome2) Biomes;
        public Craftable Craftable;

        public static Checks CreateDefault() =>
            new Checks()
            {
                InvChecks = new InvChecks
                {
                    Item = InventoryItem.None,
                    Pickup = false,
                    Count = 1,
                    IsCount = false,
                },
                Blueprint = Unlockable.None,
                EncyEntry = EncyEntry.None,
                Biomes = (Biome.None, Biome.None)
            };
    }

    public struct InvChecks
    {
        public InventoryItem Item;
        public bool Pickup;
        public int Count;
        public bool IsCount;
    }
}
