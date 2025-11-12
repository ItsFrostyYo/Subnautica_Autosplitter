using Livesplit.Subnautica;
using Livesplit.Subnautica.Settings;
using LiveSplit.Model;
using LiveSplit.VoxSplitter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Voxif.AutoSplitter;

namespace Livesplit.Subnautica
{
    public partial class SubnauticaSettings : UserControl
    {
        public List<SubnauticaSplit> Splits { get; set; }

        private static List<ComboItem<TEnum>> BuildEnumList<TEnum>(int skip, Func<TEnum, string> display) where TEnum : struct
        {
            return Enum.GetValues(typeof(TEnum))
                       .Cast<TEnum>()
                       .Skip(skip)
                       .Select(x => new ComboItem<TEnum> { Value = x, Display = display(x) })
                       .ToList();
        }

        private static readonly StringComparer AlphaComparer = StringComparer.OrdinalIgnoreCase;

        private static readonly Lazy<IReadOnlyList<ComboItem<SplitName>>> prefabs =
            new Lazy<IReadOnlyList<ComboItem<SplitName>>>(() => BuildEnumList<SplitName>(5, e => e.GetDescription()));

        private static readonly Lazy<IReadOnlyList<ComboItem<InventoryItem>>> items =
            new Lazy<IReadOnlyList<ComboItem<InventoryItem>>>(() => BuildEnumList<InventoryItem>(1, e => Localization.GetDisplayName(e)));

        private static readonly Lazy<IReadOnlyList<ComboItem<Unlockable>>> blueprints =
            new Lazy<IReadOnlyList<ComboItem<Unlockable>>>(() => BuildEnumList<Unlockable>(1, e => Localization.GetDisplayName(e)));

        private static readonly Lazy<IReadOnlyList<ComboItem<EncyEntry>>> encyEntries =
            new Lazy<IReadOnlyList<ComboItem<EncyEntry>>>(() => BuildEnumList<EncyEntry>(1, e => Localization.GetDisplayName(e)));

        private static readonly Lazy<IReadOnlyList<ComboItem<Biome>>> biomes =
            new Lazy<IReadOnlyList<ComboItem<Biome>>>(() => BuildEnumList<Biome>(1, e => Localization.GetDisplayName(e)));

        private static readonly Lazy<IReadOnlyList<ComboItem<SplitName>>> prefabAlpha =
            new Lazy<IReadOnlyList<ComboItem<SplitName>>>(() => prefabs.Value.OrderBy(x => x.Display ?? string.Empty, AlphaComparer).ToList());

        private static readonly Lazy<IReadOnlyList<ComboItem<InventoryItem>>> itemsAlpha =
            new Lazy<IReadOnlyList<ComboItem<InventoryItem>>>(() => items.Value.OrderBy(x => x.Display ?? string.Empty, AlphaComparer).ToList());

        private static readonly Lazy<IReadOnlyList<ComboItem<Unlockable>>> blueprintsAlpha =
            new Lazy<IReadOnlyList<ComboItem<Unlockable>>>(() => blueprints.Value.OrderBy(x => x.Display ?? string.Empty, AlphaComparer).ToList());

        private static readonly Lazy<IReadOnlyList<ComboItem<EncyEntry>>> encyEntriesAlpha =
            new Lazy<IReadOnlyList<ComboItem<EncyEntry>>>(() => encyEntries.Value.OrderBy(x => x.Display ?? string.Empty, AlphaComparer).ToList());

        private static readonly Lazy<IReadOnlyList<ComboItem<Biome>>>  biomesAlpha =
            new Lazy<IReadOnlyList<ComboItem<Biome>>>(() => biomes.Value.OrderBy(x => x.Display ?? string.Empty, AlphaComparer).ToList());

        public bool IntroStart { get; set; }
        public bool CreativeStart { get; set; }
        public bool Reset { get; set; }
        public bool AskForGoldSave { get; set; }
        public bool SRCLoadtimes { get; set; }
        public bool Ordered { get; set; }

        private LiveSplitState _state;

        private bool _isLoading = false;

        public SubnauticaSettings(LiveSplitState state)
        {
            InitializeComponent();
            Splits = new List<SubnauticaSplit>();
            _state = state;            
        }

        #region Buttons
        private void btnAddSplit_Click(object sender, EventArgs e)
        {
            var dialog = new SelectSplitType(this);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var setting = dialog.Func();
                flowMain.Controls.Add(setting);
                UpdateSplits();
            }
        }

        public void btnRemove_Click(object sender, EventArgs e)
        {
            for (int i = flowMain.Controls.Count - 1; i > 0; i--)
            {
                if (flowMain.Controls[i].Contains((Control)sender))
                {
                    RemoveHandlers((SubnauticaSplitSetting)((Button)sender).Parent);

                    flowMain.Controls.RemoveAt(i);
                    break;
                }
            }
            UpdateSplits();
        }

        public void btnEdit_Click(object sender, EventArgs e)
        {
            foreach (var setting in flowMain.Controls.OfType<SubnauticaSplitSetting>())
            {
                if (ReferenceEquals(setting.BtnEdit, sender))
                {
                    bool anyEnabled = setting.ComboBox.Enabled || (setting.ComboBox2?.Enabled ?? false);
                    if (anyEnabled) disableEdit(setting);
                    else enableEdit(setting);
                    break;
                }
            }
        }

        private void btnAddExplo_Click(object sender, EventArgs e)
        {
            if (_state == null)
                return;

            var componentPath = @"Components\\SubnauticaShipExplosionInfo.dll";
            var exploTimeComponent = _state.Layout.LayoutComponents.Where(x => x.Component.GetType().FullName == "LiveSplit.UI.Components.Component").FirstOrDefault();

            if (!File.Exists(componentPath)) { MessageBox.Show($"Could not find file: {componentPath}"); return; }

            if (exploTimeComponent == null)
            {
                var asm = Assembly.LoadFrom(componentPath);
                var componentType = asm.GetType("LiveSplit.UI.Components.Component");
                var component = Activator.CreateInstance(componentType, _state);
                _state.Layout.LayoutComponents.Add(new LiveSplit.UI.Components.LayoutComponent("SubnauticaShipExplosionInfo.dll", component as LiveSplit.UI.Components.IComponent));
                UpdateExploBtnContent();
            }
            else
            {
                _state.Layout.LayoutComponents.Remove(exploTimeComponent);
                UpdateExploBtnContent();
            }
        }

        private void ButtonSplitGenerator_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Generating the splits will overwrite the existing splits and times, do you want to overwrite them?",
                "Generate Splits?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            using (SplitsGenerator splitGen = new SplitsGenerator())
            {
                int maxWidth = 0;
                foreach (SubnauticaSplit split in Splits)
                {
                    string splitName = "Split name";
                    splitName = split.GetDescription();
                    splitGen.ListView.Items.Add(splitName);
                    int width = TextRenderer.MeasureText(splitName, splitGen.ListView.Font).Width;
                    if (width > maxWidth)
                    {
                        maxWidth = width;
                    }
                }
                splitGen.ListView.Columns[0].Width = maxWidth + 10;
                splitGen.ListView.Size = new Size(maxWidth + 30, (int)Math.Min(splitGen.ListView.Items[0].Bounds.Height * (splitGen.ListView.Items.Count + 1), Screen.PrimaryScreen.Bounds.Height * .75f));
                if (splitGen.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                //Doesn't work with subsplits + show last split
                _state.Run.Clear();
                foreach (ListViewItem item in splitGen.ListView.Items)
                {
                    _state.Run.AddSegment(item.Text);
                }
                _state.Form.Refresh();
            }
        }
        #endregion

        public void UpdateExploBtnContent()
        {
            bool hasExplosionInfo = _state?.Layout?.LayoutComponents?.Any(x => x?.Component?.ComponentName == "Subnautica Ship Explosion Info") ?? false;

            if (hasExplosionInfo)
                btnAddExplo.Text = "Remove Explosion Time";
            else
                btnAddExplo.Text = "Add Explosion Time";
        }

        private void enableEdit(SubnauticaSplitSetting setting)
        {
            setting.BtnEdit.Text = "✔";
            ApplyDataSources(setting, rdAlpha.Checked);
            setting.ComboBox.Enabled = true;
            if (setting.ComboBox2 != null)
                setting.ComboBox2.Enabled = true;
        }

        private void disableEdit(SubnauticaSplitSetting setting)
        {
            setting.BtnEdit.Text = "✏";
            setting.ComboBox.Enabled = false;
            if (setting.ComboBox2 != null)
                setting.ComboBox2.Enabled = false;
        }

        public void ControlChanged(object sender, EventArgs e)
        {
            if (_isLoading)
                return;

            UpdateSplits();
        }

        private void ApplyDataSources(SubnauticaSplitSetting setting, bool alpha)
        {
            switch (setting)
            {
                case SubnauticaItemSplit s:
                    BindCombo(setting.ComboBox, alpha ? itemsAlpha.Value : items.Value, setting.ComboBox.SelectedValue);
                    break;
                case SubnauticaBlueprintSplit s:
                    BindCombo(setting.ComboBox, alpha ? blueprintsAlpha.Value : blueprints.Value, setting.ComboBox.SelectedValue);
                    break;
                case SubnauticaEncySplit s:
                    BindCombo(setting.ComboBox, alpha ? encyEntriesAlpha.Value : encyEntries.Value, setting.ComboBox.SelectedValue);
                    break;
                case SubnauticaBiomeSplit s:
                    BindCombo(setting.ComboBox, alpha ? biomesAlpha.Value : biomes.Value, setting.ComboBox.SelectedValue);
                    BindCombo(setting.ComboBox2, alpha ? biomesAlpha.Value : biomes.Value, setting.ComboBox2.SelectedValue ?? setting.ComboBox.SelectedValue);
                    break;
                default:
                    BindCombo(setting.ComboBox, alpha ? prefabAlpha.Value : prefabs.Value, setting.ComboBox.SelectedValue);
                    break;
            }
        }

        private void UpdateSplits()
        {
            IntroStart = chkIntroStart.Checked;
            CreativeStart = chkCreativeStart.Checked;
            Reset = chkReset.Checked;
            AskForGoldSave = chkAskForGoldSave.Checked;
            SRCLoadtimes = chkSRCLoadtimes.Checked;
            Ordered = cbOrdered.Checked;

            Splits.Clear();
            foreach (var setting in flowMain.Controls.OfType<SubnauticaSplitSetting>())
                if (!string.IsNullOrEmpty(setting.ComboBox.Text))
                    Splits.Add(setting.Split);
        }

        private void AddHandlers(SubnauticaSplitSetting setting)
        {
            setting.ComboBox.SelectedIndexChanged += new EventHandler(ControlChanged);
            setting.CbSplitOnce.CheckedChanged += new EventHandler(ControlChanged);
            setting.BtnRemove.Click += new EventHandler(btnRemove_Click);
            setting.BtnEdit.Click += new EventHandler(btnEdit_Click);
        }

        private void RemoveHandlers(SubnauticaSplitSetting setting)
        {
            setting.ComboBox.SelectedIndexChanged -= ControlChanged;
            setting.CbSplitOnce.CheckedChanged -= ControlChanged;
            setting.BtnRemove.Click -= btnRemove_Click;
            setting.BtnEdit.Click -= btnEdit_Click;
        }

        public void LoadSettings()
        {
            SubnauticaSplitSetting[] settings = new SubnauticaSplitSetting[Splits.Count];

            _isLoading = true;
            try
            {
                flowMain.SuspendLayout();

                for (int i = flowMain.Controls.Count - 1; i > 0; i--)
                    flowMain.Controls.RemoveAt(i);

                chkIntroStart.Checked = IntroStart;
                chkCreativeStart.Checked = CreativeStart;
                chkReset.Checked = Reset;
                chkAskForGoldSave.Checked = AskForGoldSave;
                chkSRCLoadtimes.Checked = SRCLoadtimes;
                cbOrdered.Checked = Ordered;

                for (int i = 0; i < Splits.Count; i++)
                {
                    var split = Splits[i];
                    SubnauticaSplitSetting setting;
                    switch (split)
                    {
                        case ItemSplit s:
                            setting = new SubnauticaItemSplit();
                            ApplyDataSources(setting, rdAlpha.Checked);
                            setting.ComboBox.SelectedValue = s.Item;
                            setting.CbSplitOnce.Checked = s.OnlySplitOnce;
                            break;

                        case BlueprintSplit s:
                            setting = new SubnauticaBlueprintSplit();
                            ApplyDataSources(setting, rdAlpha.Checked);
                            setting.ComboBox.SelectedValue = s.Blueprint;
                            setting.CbSplitOnce.Checked = s.OnlySplitOnce;
                            break;

                        case EncySplit s:
                            setting = new SubnauticaEncySplit();
                            ApplyDataSources(setting, rdAlpha.Checked);
                            setting.ComboBox.SelectedValue = s.Entry;
                            setting.CbSplitOnce.Checked = s.OnlySplitOnce;
                            break;

                        case BiomeSplit s:
                            setting = new SubnauticaBiomeSplit();
                            ApplyDataSources(setting, rdAlpha.Checked);
                            setting.ComboBox.SelectedValue = s.Biomes.Biome1;
                            setting.ComboBox2.SelectedValue = s.Biomes.Biome2;
                            setting.CbSplitOnce.Checked = s.OnlySplitOnce;
                            break;

                        default:
                            setting = new SubnauticaPrefabSplit();
                            ApplyDataSources(setting, rdAlpha.Checked);
                            setting.ComboBox.SelectedValue = split.SplitName;
                            setting.CbSplitOnce.Checked = split.OnlySplitOnce;
                            break;
                    }

                    setting.ComboBox.Enabled = false;
                    if (setting.ComboBox2 != null) setting.ComboBox2.Enabled = false;

                    AddHandlers(setting);
                    settings[i] = setting;
                }
            }
            finally
            {
                flowMain.Controls.AddRange(settings);
                _isLoading = false;
                flowMain.ResumeLayout();
            }
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private T CreateSplit<T, TEnum>(IEnumerable<ComboItem<TEnum>> data, Func<T, ComboBox> getCombo) where T : SubnauticaSplitSetting, new()
        {
            var setting = new T();
            var combo = getCombo(setting);
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.MouseWheel += (o, e) => ((HandledMouseEventArgs)e).Handled = true;

            combo.DisplayMember = "Display";
            combo.ValueMember = "Value";
            combo.DataSource = data.ToList();

            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;

            setting.BtnEdit.Text = "✔";
            AddHandlers(setting);
            return setting;
        }

        public SubnauticaPrefabSplit CreatePrefabSplit() => CreateSplit<SubnauticaPrefabSplit, SplitName>(rdAlpha.Checked ? prefabAlpha.Value : prefabs.Value, s => s.cboName);
        public SubnauticaItemSplit CreateItemSplit() => CreateSplit<SubnauticaItemSplit, InventoryItem>(rdAlpha.Checked ? itemsAlpha.Value : items.Value, s => s.cboItem);
        public SubnauticaBlueprintSplit CreateBlueprintSplit() => CreateSplit<SubnauticaBlueprintSplit, Unlockable>(rdAlpha.Checked ? blueprintsAlpha.Value : blueprints.Value, s => s.cboBlueprint);
        public SubnauticaEncySplit CreateEncySplit() => CreateSplit<SubnauticaEncySplit, EncyEntry>(rdAlpha.Checked ? encyEntriesAlpha.Value : encyEntries.Value, s => s.cboEncy);
        public SubnauticaBiomeSplit CreateBiomeSplit()
        {
            var setting = new SubnauticaBiomeSplit();
            var data = rdAlpha.Checked ? biomesAlpha : biomes;
            BindCombo(setting.cboBiome1, data.Value, null);
            BindCombo(setting.cboBiome2, data.Value, null);
            setting.btnEdit.Text = "✔";
            AddHandlers(setting);
            return setting;
        }

        private void radio_CheckedChanged(object sender, EventArgs e)
        {
            flowMain.SuspendLayout();

            foreach (var setting in flowMain.Controls.OfType<SubnauticaSplitSetting>())
                ApplyDataSources(setting, rdAlpha.Checked);

            flowMain.ResumeLayout();
        }
        private void flowMain_DragDrop(object sender, DragEventArgs e) => UpdateSplits();
        private void flowMain_DragEnter(object sender, DragEventArgs e) => e.Effect = DragDropEffects.Move;
        private void flowMain_DragOver(object sender, DragEventArgs e)
        {
            SubnauticaSplitSetting data = null;

            if (e.Data.GetDataPresent(typeof(SubnauticaSplitSetting)))
                data = (SubnauticaSplitSetting)e.Data.GetData(typeof(SubnauticaSplitSetting));
            else if (e.Data.GetDataPresent(typeof(SubnauticaBlueprintSplit)))
                data = (SubnauticaSplitSetting)e.Data.GetData(typeof(SubnauticaBlueprintSplit));
            else if (e.Data.GetDataPresent(typeof(SubnauticaItemSplit)))
                data = (SubnauticaSplitSetting)e.Data.GetData(typeof(SubnauticaItemSplit));
            else if (e.Data.GetDataPresent(typeof(SubnauticaPrefabSplit)))
                data = (SubnauticaSplitSetting)e.Data.GetData(typeof(SubnauticaPrefabSplit));
            else if (e.Data.GetDataPresent(typeof(SubnauticaEncySplit)))
                data = (SubnauticaSplitSetting)e.Data.GetData(typeof(SubnauticaEncySplit));
            else if (e.Data.GetDataPresent(typeof(SubnauticaBiomeSplit)))
                data = (SubnauticaSplitSetting)e.Data.GetData(typeof(SubnauticaBiomeSplit));

            if (data == null)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            FlowLayoutPanel destination = (FlowLayoutPanel)sender;
            Point p = destination.PointToClient(new Point(e.X, e.Y));
            var item = destination.GetChildAtPoint(p);
            int index = destination.Controls.GetChildIndex(item, false);

            if (index == 0)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            e.Effect = DragDropEffects.Move;

            int oldIndex = destination.Controls.GetChildIndex(data);
            if (oldIndex != index)
            {
                enableEdit(data);
                destination.Controls.SetChildIndex(data, index);
                destination.Invalidate();
            }
        }


        public XmlNode UpdateSettings(XmlDocument document)
        {
            XmlElement xmlSettings = document.CreateElement("Settings");

            AddBool(document, xmlSettings, "IntroStart", IntroStart);
            AddBool(document, xmlSettings, "CreativeStart", CreativeStart);
            AddBool(document, xmlSettings, "Reset", Reset);
            AddBool(document, xmlSettings, "AskForGoldSave", AskForGoldSave);
            AddBool(document, xmlSettings, "SRCLoadtimes", SRCLoadtimes);
            AddBool(document, xmlSettings, "Ordered", Ordered);

            XmlElement xmlSplits = document.CreateElement("Splits");
            xmlSettings.AppendChild(xmlSplits);

            foreach (var split in Splits)
            {
                XmlElement xmlSplit = document.CreateElement("Split");
                XmlElement xmlName = document.CreateElement("Name");
                XmlElement xmlOnlySplitOnce = document.CreateElement("OnlySplitOnce");
                XmlElement xmlValue = document.CreateElement("Value");

                xmlName.InnerText = split.SplitName.ToString();
                xmlOnlySplitOnce.InnerText = split.OnlySplitOnce.ToString();

                switch (split)
                {
                    case ItemSplit itemSplit:
                        xmlValue.InnerText = itemSplit.Item.ToString();
                        break;
                    case BlueprintSplit bpSplit:
                        xmlValue.InnerText = bpSplit.Blueprint.ToString();
                        break;
                    case EncySplit encySplit:
                        xmlValue.InnerText = encySplit.Entry.ToString();
                        break;
                    case BiomeSplit biomeSplit:
                        xmlValue.InnerText = $"{biomeSplit.Biomes.Biome1}:{biomeSplit.Biomes.Biome2}";
                        break;
                    default:
                        xmlValue.InnerText = split.SplitName.ToString();
                        break;
                }

                xmlSplit.AppendChild(xmlOnlySplitOnce);
                xmlSplit.AppendChild(xmlName);
                xmlSplit.AppendChild(xmlValue);
                xmlSplits.AppendChild(xmlSplit);
            }

            return xmlSettings;
        }

        public void SetSettings(XmlNode settings)
        {
            try
            {
                XmlNode splitsNode = settings.SelectSingleNode(".//Splits");
                if (splitsNode != null)
                {
                    IntroStart = ReadBool(settings, "IntroStart");
                    CreativeStart = ReadBool(settings, "CreativeStart");
                    Reset = ReadBool(settings, "Reset");
                    AskForGoldSave = ReadBool(settings, "AskForGoldSave");
                    SRCLoadtimes = ReadBool(settings, "SRCLoadtimes");
                    Ordered = ReadBool(settings, "Ordered");

                    Splits.Clear();
                    foreach (XmlNode splitNode in settings.SelectNodes(".//Splits/Split"))
                    {
                        var name = splitNode.SelectSingleNode("Name")?.InnerText;
                        var value = splitNode.SelectSingleNode("Value")?.InnerText;
                        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value)) continue;

                        bool onlySplitOnce = true;
                        bool.TryParse(splitNode.SelectSingleNode("OnlySplitOnce")?.InnerText, out onlySplitOnce);

                        var splitName = SubnauticaSplitSetting.GetSplitName(name);
                        switch (splitName)
                        {
                            case SplitName.Inventory:
                                var item = SubnauticaSplitSetting.GetTechType(value);
                                Splits.Add(new ItemSplit(item.ConvertTo<InventoryItem>(), onlySplitOnce));
                                break;
                            case SplitName.Blueprint:
                                var blueprint = SubnauticaSplitSetting.GetTechType(value);
                                Splits.Add(new BlueprintSplit(blueprint.ConvertTo<Unlockable>(), onlySplitOnce));
                                break;
                            case SplitName.Encyclopedia:
                                var encyEntry = SubnauticaSplitSetting.GetEncyEntry(value);
                                Splits.Add(new EncySplit(encyEntry, onlySplitOnce));
                                break;
                            case SplitName.Biome:
                                var parts = value.Split(':');
                                if (parts.Length == 2)
                                    Splits.Add(new BiomeSplit(
                                        (SubnauticaSplitSetting.GetBiome(parts[0]),
                                         SubnauticaSplitSetting.GetBiome(parts[1])), onlySplitOnce));
                                break;
                            default:
                                Splits.Add(new PrefabSplit(splitName, onlySplitOnce));
                                break;
                        }
                    }
                }
                else
                {
                    IntroStart = true;
                    CreativeStart = false;
                    Reset = true;
                    AskForGoldSave = true;
                    SRCLoadtimes = true;
                    Ordered = false;
                    Splits.Clear();
                }
            }
            catch (Exception ex) { }
        }    

        private static XmlElement AddBool(XmlDocument doc, XmlElement root, string name, bool value)
        {
            var e = doc.CreateElement(name); e.InnerText = value.ToString(); root.AppendChild(e); return e;
        }
        private static bool ReadBool(XmlNode root, string name, bool def = false)
        {
            var n = root.SelectSingleNode($".//{name}");
            return n != null && bool.TryParse(n.InnerText, out var b) ? b : def;
        }
        public static void BindCombo<T>(ComboBox combo, IEnumerable<ComboItem<T>> data, object previousSelected)
        {
            combo.BeginUpdate();
            try
            {
                combo.BindingContext = new BindingContext();
                var list = (data as IList<ComboItem<T>>) ?? data.ToList();
                combo.DisplayMember = "Display";
                combo.ValueMember = "Value";
                combo.DataSource = new List<ComboItem<T>>(list);
                if (previousSelected is T) combo.SelectedValue = (T)previousSelected;
            }
            finally
            {
                combo.EndUpdate();
            }
        }
    }
    public sealed class ComboItem<T>
    {
        public T Value { get; set; }
        public string Display { get; set; }
    }
}
