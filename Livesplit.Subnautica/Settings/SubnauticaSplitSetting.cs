using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiveSplit.Subnautica
{
    public class SubnauticaSplitSetting : UserControl
    {
        public Func<bool> IsLoadingGetter { get; set; }
        public bool IsLoading => IsLoadingGetter?.Invoke() ?? false;
        public bool IsSubCondition { get; set; } = false;
        public virtual ComboBox ComboBox { get; }
        public virtual ComboBox ComboBox2 { get; }
        public virtual Button BtnEdit { get; }
        public virtual Button BtnRemove { get; }
        public virtual SplitName SplitName { get; }
        public virtual SubnauticaSplit Split { get; }

        public static SplitName GetSplitName(string text)
        {
            foreach (SplitName split in Enum.GetValues(typeof(SplitName)))
            {
                string name = split.ToString();
                MemberInfo info = typeof(SplitName).GetMember(name)[0];
                DescriptionAttribute description = (DescriptionAttribute)info.GetCustomAttributes(typeof(DescriptionAttribute), false)[0];

                if (name.Equals(text, StringComparison.OrdinalIgnoreCase) || description.Description.Equals(text, StringComparison.OrdinalIgnoreCase))
                {
                    return split;
                }
            }
            return SplitName.None;
        }

        public static TechType GetTechType(string text)
        {
            foreach (TechType techType in Enum.GetValues(typeof(TechType)))
            {
                string name = techType.ToString();
                string displayName = Localization.GetDisplayName(name);

                if (name.Equals(text, StringComparison.OrdinalIgnoreCase) || displayName.Equals(text, StringComparison.OrdinalIgnoreCase))
                {
                    return techType;
                }
            }
            return TechType.None;
        }

        public static EncyEntry GetEncyEntry(string text)
        {
            foreach (EncyEntry encyEntry in Enum.GetValues(typeof(EncyEntry)))
            {
                string name = encyEntry.ToString();
                string displayName = Localization.GetDisplayName(name);

                if (name.Equals(text, StringComparison.OrdinalIgnoreCase) || displayName.Equals(text, StringComparison.OrdinalIgnoreCase))
                {
                    return encyEntry;
                }
            }
            return EncyEntry.None;
        }

        public static Biome GetBiome(string text)
        {
            foreach (Biome biome in Enum.GetValues(typeof(Biome)))
            {
                string name = biome.ToString();
                string displayName = Localization.GetDisplayName(name);

                if (name.Equals(text, StringComparison.OrdinalIgnoreCase) || displayName.Equals(text, StringComparison.OrdinalIgnoreCase))
                {
                    return biome;
                }
            }
            return Biome.None;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
                DoDragDrop(this, DragDropEffects.Move);
        }
    }    
    
    public class SubnauticaSplit
    {
        public SplitName SplitName { get; set; }
        public bool OnlySplitOnce { get; set; }
        public bool IsSubCondition { get; set; }
        public List<SubnauticaSplit> Conditions { get; set; } = new List<SubnauticaSplit>();
        public virtual string GetDescription() => "Split";
        public virtual SubnauticaSplit DeepCopy()
        {
            var copy = (SubnauticaSplit)this.MemberwiseClone();
            copy.Conditions = Conditions?.Select(c => c.DeepCopy()).ToList() ?? new List<SubnauticaSplit>();

            return copy;
        }
    }

    public enum SplitName
    {
        [Description("None"), ToolTip("None")]
        None,
        [Description("Inventory"), ToolTip("Splits when you pickup/drop a certain item")]
        Inventory,
        [Description("Blueprint"), ToolTip("Splits when you have a certain blueprint unlocked")]
        Blueprint,
        [Description("Encyclopedia"), ToolTip("Splits when you have a certain entry in the encyclopedia unlocked")]
        Encyclopedia,
        [Description("Biome"), ToolTip("Splits when you have enter a certain biome from a certain biome")]
        Biome,
        [Description("Craft"), ToolTip("Splits when you start crafting a certain item from a crafting menu")]
        Craft,
        [Description("Aurora Biome Change"), ToolTip("Splits when the biome changes from Aurora to Shallows or Kelp Forest")]
        AuroraBiomeSplit,
        [Description("Aurora Death"), ToolTip("Splits when you die in the Aurora")]
        AuroraDeathSplit,
        [Description("Exit Aurora at Patch Hole"), ToolTip("Splits when you exit the Aurora through the old patch hole after unlocking the rocket")]
        AuroraExitSplit,
        [Description("Base Death Split (includes Clip A and Clip C)"), ToolTip("Splits when you die next to the main base (includes Clip A and Clip C)")]
        BaseDeathSplit,
        [Description("Build Boosters Section"), ToolTip("Splits when you build the boosters section of the Neptune Rocket")]
        BoostersSplit,
        [Description("Builder Loop Lifepod Return"), ToolTip("Splits when you return to the Lifepod with 3 or 4 Table Coral Samples and no Builder Tool in your inventory")]
        BuilderLoopLifepodReturnSplit,
        /*[Description("Chair%"), ToolTip("Splits when you sit")]
        ChairSplit,*/
        [Description("Cure"), ToolTip("Splits when you cure yourself")]
        CureSplit,
        [Description("Death"), ToolTip("Splits when you die")]
        DeathSplit,
        [Description("Enter Base"), ToolTip("Splits when you enter a constructed base")]
        EnterBaseSplit,
        [Description("Exit Base"), ToolTip("Splits when you exit a constructed base")]
        ExitBaseSplit,
        [Description("Collect 4 Stalker Teeth"), ToolTip("Splits when you collect four Stalker teeth")]
        FourToothSplit,
        [Description("Build Fuel Reserves Section"), ToolTip("Splits when you build the fuel reserves section of the Neptune Rocket")]
        FuelReservesSplit,
        [Description("Full Inventory"), ToolTip("Splits when you have a full inventory")]
        FullInventorySplit,
        [Description("Build Gantry Section"), ToolTip("Splits when you build the gantry section of the Neptune Rocket")]
        GantrySplit,
        [Description("Deactivate Gun"), ToolTip("Splits when you deactivate the gun")]
        GunDeactivationSplit,
        [Description("Gun Death"), ToolTip("Splits when you die in the gun room")]
        GunDeathSplit,
        [Description("Hatch Eggs"), ToolTip("Splits when you hatch the eggs in the prison aquarium")]
        HatchSplit,
        [Description("HCG Sparse Split (includes clip A and C)"), ToolTip("Splits when you enter the main base with a ruby (includes clip A and C)")]
        HCGSparseSplit,
        [Description("Alien Thermal Plant Death"), ToolTip("Splits when you die in the Alien Thermal Plant")]
        IonDeathSplit,
        [Description("Unlock Ion Blueprints"), ToolTip("Splits when you unlock the Ion BP")]
        IonUnlockSplit,
        [Description("Alien Thermal Plant Unstuck"), ToolTip("Splits when you unstuck in the Ion BP room")]
        IonUnstuckSplit,
        [Description("Leave Kelp Forest (Alt. Tooth Split)"), ToolTip("Splits when you leave the Kelp Forest with one or more Creepvine samples")]
        LeaveKelpForestSplit,
        [Description("Descend at Mountain"), ToolTip("Splits when you descend under the arch after getting out of bounds")]
        MountainDescendSplit,
        [Description("Build Neptune Launch Platform"), ToolTip("Splits when you build the Neptune Launch Platform")]
        NeptuneLaunchPlatform,
        [Description("Enter PCF Pool"), ToolTip("Splits when you enter the prison aquarium the normal way")]
        PCFPoolSplit,
        [Description("Insert PCF Tablet"), ToolTip("Splits when you insert the tablet at the PCF entrance")]
        PCFTabletSplit,
        [Description("Prison Portal"), ToolTip("Splits when you enter the prison aquarium portal for the first time")]
        PortalSplit,
        [Description("Repair Reactor Core"), ToolTip("Splits when you repair the aurora reactor core")]
        ReactorCoreRepairSplit,
        [Description("Rocket Launch"), ToolTip("Splits when you start the Neptune Rocket")]
        RocketSplit,
        [Description("Unlock Rocket"), ToolTip("Splits when you unlock the Neptune Rocket")]
        RocketUnlockSplit,
        [Description("Glitchless Shallows Split"), ToolTip("Splits when you exit a constructed base with 1 Table Coral Sample, 1 Cave Sulfur, 1 Battery, and 2 Fiber Mesh or 2 First Aid Kits")]
        SGLShallowsSplit,
        [Description("Sparse Biome Change"), ToolTip("Splits when the biome changes from Sparse to Shallows or Kelp Forest")]
        SparseBiomeSplit,
        [Description("Sparse Death"), ToolTip("Splits when you die in the biomes: Sea Treader Path or Sparse Reef")]
        SparseDeathSplit,
        [Description("Throw Flare"), ToolTip("Splits when you throw a flare")]
        ThrowFlareSplit,
        [Description("Collect Upper Tablet"), ToolTip("Splits when you grab the purple tablet above the Alien Gun Entrence")]
        UpperTabletSplit,
        [Description("Repair Secondary Systems"), ToolTip("Splits when you Repair the Lifepod Secondary Systems")]
        RepairSecondarySystemsSplit,
        [Description("Repair Radio"), ToolTip("Splits when you Repair the Lifepod Radio")]
        RepairRadioSplit,
    }
    public class ToolTipAttribute : Attribute
    {
        public string ToolTip { get; set; }
        public ToolTipAttribute(string text)
        {
            ToolTip = text;
        }
    }
}
