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
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Voxif.AutoSplitter;

namespace Livesplit.Subnautica
{
    public partial class SubnauticaSettings : UserControl
    {
        public List<SubnauticaSplit> Splits { get; private set; }
        
        public List<ComboItem<SplitName>> PrefabSplits;
        public List<ComboItem<SplitName>> PrefabSplitsAlphaSorted;
        public List<ComboItem<InventoryItem>> Items;
        public List<ComboItem<InventoryItem>> ItemsAlphaSorted;
        public List<ComboItem<Unlockable>> Blueprints;
        public List<ComboItem<Unlockable>> BlueprintsAlphaSorted;
        public List<ComboItem<EncyEntry>> EncyEntries;
        public List<ComboItem<EncyEntry>> EncyEntriesAlphaSorted;

        public bool introStart { get; set; }
        public bool creativeStart { get; set; }
        public bool reset {  get; set; }
        public bool askForGoldSave { get; set; }
        public bool SRCLoadtimes { get; set; }
        public bool Ordered { get; set; }

        private LiveSplitState _state;
        private static ReaderWriterLockSlim isLoading = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        
        public SubnauticaSettings(LiveSplitState state)
        {
            InitializeComponent();            
            Splits = new List<SubnauticaSplit>();
            _state = state;
            PrefabSplits = Enum.GetValues(typeof(SplitName))
                               .Cast<SplitName>()
                               .Skip(4)
                               .Select(s => new ComboItem<SplitName> { Value = s, Display = s.GetDescription() })
                               .ToList();
            PrefabSplitsAlphaSorted = PrefabSplits.OrderBy(x => x.Display).ToList();

            Items = Enum.GetValues(typeof(InventoryItem))
                        .Cast<InventoryItem>()
                        .Skip(1)
                        .Select(t => new ComboItem<InventoryItem> { Value = t, Display = Localization.GetDisplayName(t) })
                        .ToList();
            ItemsAlphaSorted = Items.OrderBy(x => x.Display).ToList();

            Blueprints = Enum.GetValues(typeof(Unlockable))
                        .Cast<Unlockable>()
                        .Skip(1)
                        .Select(t => new ComboItem<Unlockable> { Value = t, Display = Localization.GetDisplayName(t) })
                        .ToList();
            BlueprintsAlphaSorted = Blueprints.OrderBy(x => x.Display).ToList();

            EncyEntries = Enum.GetValues(typeof(EncyEntry))
                               .Cast<EncyEntry>()
                               .Skip(1)
                               .Select(e => new ComboItem<EncyEntry> { Value = e, Display = Localization.GetDisplayName(e) })
                               .ToList();
            EncyEntriesAlphaSorted = EncyEntries.OrderBy(x => x.Display).ToList();
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
                    if (setting.ComboBox.Enabled) 
                        disableEdit(setting);
                    else  
                        enableEdit(setting);
                    break;
                }
            }
        }

        private void btnAddExplo_Click(object sender, EventArgs e)
        {
            if(_state == null)
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
            var combo = setting.ComboBox;
            combo.DisplayMember = "Display";
            combo.ValueMember = "Value";
            var prev = combo.SelectedValue;
            switch (setting)
            {
                case SubnauticaItemSplit itemSplit:
                    combo.DataSource = rdAlpha.Checked ? ItemsAlphaSorted : Items;
                    if (prev is InventoryItem prevItem)
                        combo.SelectedValue = prevItem;
                    break;
                case SubnauticaBlueprintSplit bpSplit:
                    combo.DataSource = rdAlpha.Checked ? BlueprintsAlphaSorted : Blueprints;
                    if (prev is Unlockable prevBP)
                        combo.SelectedValue = prevBP;
                    break;
                case SubnauticaEncySplit encySplit:
                    combo.DataSource = rdAlpha.Checked ? EncyEntriesAlphaSorted : EncyEntries;
                    if (prev is EncyEntry prevEntry)
                        combo.SelectedValue = prevEntry;
                    break;
                default:
                    combo.DataSource = rdAlpha.Checked ? PrefabSplitsAlphaSorted : PrefabSplits;
                    if (prev is SplitName prevSplit)
                        combo.SelectedValue = prevSplit;
                    break;
            }
            combo.Enabled = true;
        }
        private void disableEdit(SubnauticaSplitSetting setting)
        {
            setting.BtnEdit.Text = "✏";
            setting.ComboBox.Enabled = false;
        }
        public void ControlChanged(object sender, EventArgs e)
        {
            UpdateSplits();
        }

        public void UpdateSplits()
        {
            try
            {
                // NO retry, lower priority than SetSettings and LoadSettings
                if (!isLoading.TryEnterWriteLock(0))
                {
                    return;
                }
            }
            catch (LockRecursionException)
            {
                return;
            }

            introStart = chkIntroStart.Checked;
            creativeStart = chkCreativeStart.Checked;
            reset = chkReset.Checked;
            askForGoldSave = chkAskForGoldSave.Checked;
            SRCLoadtimes = chkSRCLoadtimes.Checked;
            Ordered = cbOrdered.Checked;

            Splits.Clear();
            foreach (Control c in flowMain.Controls)
            {
                if (c is SubnauticaSplitSetting setting)
                {
                    if (!string.IsNullOrEmpty(setting.ComboBox.Text))
                    {
                        Splits.Add(setting.Split);
                    }
                }
            }

            isLoading.ExitWriteLock();
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
            try
            {
                // 5 seconds, higher priority than UpdateSplits
                if (!isLoading.TryEnterReadLock(5000))
                {
                    return;
                }
            }
            catch (LockRecursionException)
            {
                return;
            }

            this.flowMain.SuspendLayout();

            for (int i = flowMain.Controls.Count - 1; i > 0; i--)
            {
                flowMain.Controls.RemoveAt(i);
            }

            chkIntroStart.Checked = introStart;
            chkCreativeStart.Checked = creativeStart;
            chkReset.Checked = reset;
            chkAskForGoldSave.Checked = askForGoldSave;
            chkSRCLoadtimes.Checked = SRCLoadtimes;
            cbOrdered.Checked = Ordered;

            foreach (var split in Splits)
            {
                SubnauticaSplitSetting setting;

                switch (split)
                {
                    case ItemSplit itemSplit:
                        setting = new SubnauticaItemSplit();
                        setting.CbSplitOnce.Checked = itemSplit.OnlySplitOnce;
                        var data1 = rdAlpha.Checked ? ItemsAlphaSorted : Items;
                        var combo1 = setting.ComboBox;

                        combo1.DisplayMember = "Display";
                        combo1.ValueMember = "Value";
                        combo1.DataSource = data1;

                        combo1.SelectedValue = itemSplit.Item;
                        break;

                    case BlueprintSplit bpSplit:
                        setting = new SubnauticaBlueprintSplit();
                        setting.CbSplitOnce.Checked = bpSplit.OnlySplitOnce;
                        var data3 = rdAlpha.Checked ? BlueprintsAlphaSorted : Blueprints;
                        var combo3 = setting.ComboBox;

                        combo3.DisplayMember = "Display";
                        combo3.ValueMember = "Value";
                        combo3.DataSource = data3;

                        combo3.SelectedValue = bpSplit.Blueprint;
                        break;

                    case EncySplit encySplit:
                        setting = new SubnauticaEncySplit();
                        setting.CbSplitOnce.Checked = encySplit.OnlySplitOnce;
                        var data4 = rdAlpha.Checked ? EncyEntriesAlphaSorted : EncyEntries;
                        var combo4 = setting.ComboBox;

                        combo4.DisplayMember = "Display";
                        combo4.ValueMember = "Value";
                        combo4.DataSource = data4;

                        combo4.SelectedValue = encySplit.Entry;
                        break;

                    default:
                        setting = new SubnauticaPrefabSplit();
                        setting.CbSplitOnce.Checked = split.OnlySplitOnce;
                        var data2 = rdAlpha.Checked ? PrefabSplitsAlphaSorted : PrefabSplits;
                        var combo2 = setting.ComboBox;

                        combo2.DisplayMember = "Display";
                        combo2.ValueMember = "Value";
                        combo2.DataSource = data2;

                        combo2.SelectedValue = split.SplitName;
                        break;
                }

                setting.ComboBox.Enabled = false;
                AddHandlers(setting);
                flowMain.Controls.Add(setting);
            }

            isLoading.ExitReadLock();
            this.flowMain.ResumeLayout(true);
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            LoadSettings();
        }

        public SubnauticaPrefabSplit CreatePrefabSplit()
        {
            SubnauticaPrefabSplit setting = new SubnauticaPrefabSplit();

            var data = rdAlpha.Checked ? PrefabSplitsAlphaSorted : PrefabSplits;
            setting.cboName.DisplayMember = "Display";
            setting.cboName.ValueMember = "Value";
            setting.cboName.DataSource = data;

            if (data.Count > 0)
                setting.cboName.SelectedValue = data[0].Value;

            setting.btnEdit.Text = "✔";
            AddHandlers(setting);
            return setting;
        }

        public SubnauticaItemSplit CreateItemSplit()
        {
            SubnauticaItemSplit setting = new SubnauticaItemSplit();

            var data = rdAlpha.Checked ? ItemsAlphaSorted : Items;
            setting.cboItem.DisplayMember = "Display";
            setting.cboItem.ValueMember = "Value";
            setting.cboItem.DataSource = data;

            if (data.Count > 0)
                setting.cboItem.SelectedValue = data[0].Value;

            setting.btnEdit.Text = "✔";
            AddHandlers(setting);
            return setting;
        }

        public SubnauticaBlueprintSplit CreateBlueprintSplit()
        {
            SubnauticaBlueprintSplit setting = new SubnauticaBlueprintSplit();

            var data = rdAlpha.Checked ? BlueprintsAlphaSorted : Blueprints;
            setting.cboBlueprint.DisplayMember = "Display";
            setting.cboBlueprint.ValueMember = "Value";
            setting.cboBlueprint.DataSource = data;

            if (data.Count > 0)
                setting.cboBlueprint.SelectedValue = data[0].Value;

            setting.btnEdit.Text = "✔";
            AddHandlers(setting);
            return setting;
        }

        public SubnauticaEncySplit CreateEncySplit()
        {
            SubnauticaEncySplit setting = new SubnauticaEncySplit();

            var data = rdAlpha.Checked ? EncyEntriesAlphaSorted : EncyEntries;
            setting.cboEncy.DisplayMember = "Display";
            setting.cboEncy.ValueMember = "Value";
            setting.cboEncy.DataSource = data;

            if (data.Count > 0)
                setting.cboEncy.SelectedValue = data[0].Value;

            setting.btnEdit.Text = "✔";
            AddHandlers(setting);
            return setting;
        }

        //private List<string> GetAvailableSplits()
        //{
        //    if (availableSplits.Count == 0)
        //    {
        //        foreach (SplitName split in Enum.GetValues(typeof(SplitName)))
        //        {
        //            MemberInfo info = typeof(SplitName).GetMember(split.ToString())[0];
        //            DescriptionAttribute description = (DescriptionAttribute)info.GetCustomAttributes(typeof(DescriptionAttribute), false)[0];
        //            if ((int)split > 1) availableSplits.Add(description.Description);
        //            if ((int)split > 1) availableSplitsAlphaSorted.Add(description.Description);
        //        }
        //        availableSplitsAlphaSorted.Sort(delegate (string one, string two)
        //        {
        //            return one.CompareTo(two);
        //        });
        //    }
        //    return rdAlpha.Checked ? availableSplitsAlphaSorted : availableSplits;
        //}

        private void radio_CheckedChanged(object sender, EventArgs e)
        {
            foreach (Control c in flowMain.Controls)
            {
                if (c is SubnauticaSplitSetting setting)
                {
                    var combo = setting.ComboBox;
                    var prev = combo.SelectedValue;

                    combo.DisplayMember = "Display";
                    combo.ValueMember = "Value";

                    switch (setting)
                    {
                        case SubnauticaItemSplit itemSplit:
                            combo.DataSource = rdAlpha.Checked ? ItemsAlphaSorted : Items;
                            if (prev is InventoryItem prevItem)
                                combo.SelectedValue = prevItem;
                            break;
                        case SubnauticaBlueprintSplit bpSplit:
                            combo.DataSource = rdAlpha.Checked ? BlueprintsAlphaSorted : Blueprints;
                            if (prev is Unlockable prevBP)
                                combo.SelectedValue = prevBP;
                            break;
                        case SubnauticaEncySplit encySplit:
                            combo.DataSource = rdAlpha.Checked ? EncyEntriesAlphaSorted : EncyEntries;
                            if (prev is EncyEntry prevEntry)
                                combo.SelectedValue = prevEntry;
                            break;
                        default:
                            combo.DataSource = rdAlpha.Checked ? PrefabSplitsAlphaSorted : PrefabSplits;
                            if (prev is SplitName prevSplit)
                                combo.SelectedValue = prevSplit;
                            break;
                    }
                }
            }
        }

        private void flowMain_DragDrop(object sender, DragEventArgs e)
        {
            UpdateSplits();
        }
        private void flowMain_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }
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

            XmlElement xmlIntroStart = document.CreateElement("IntroStart");
            xmlIntroStart.InnerText = introStart.ToString();
            xmlSettings.AppendChild(xmlIntroStart);

            XmlElement xmlCreativeStart = document.CreateElement("CreativeStart");
            xmlCreativeStart.InnerText = creativeStart.ToString();
            xmlSettings.AppendChild(xmlCreativeStart);

            XmlElement xmlReset = document.CreateElement("Reset");
            xmlReset.InnerText = reset.ToString();
            xmlSettings.AppendChild(xmlReset);
            
            XmlElement xmlAskForGoldSave = document.CreateElement("AskForGoldSave");
            xmlAskForGoldSave.InnerText = askForGoldSave.ToString();
            xmlSettings.AppendChild(xmlAskForGoldSave);

            XmlElement xmlSRCLoadtimes = document.CreateElement("SRCLoadtimes");
            xmlSRCLoadtimes.InnerText = SRCLoadtimes.ToString();
            xmlSettings.AppendChild(xmlSRCLoadtimes);

            XmlElement xmlOrdered = document.CreateElement("Ordered");
            xmlOrdered.InnerText = Ordered.ToString();
            xmlSettings.AppendChild(xmlOrdered);

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
                // 5 seconds, higher priority than UpdateSplits
                if (!isLoading.TryEnterWriteLock(5000))
                {
                    return;
                }
            }
            catch (LockRecursionException)
            {
                return;
            }

            XmlNode splitsNode = settings.SelectSingleNode(".//Splits");

            if (splitsNode != null)
            {
                XmlNode introStartNode = settings.SelectSingleNode(".//IntroStart");
                XmlNode creativeStartNode = settings.SelectSingleNode(".//CreativeStart");
                XmlNode resetNode = settings.SelectSingleNode(".//Reset");
                XmlNode askForGoldSaveNode = settings.SelectSingleNode(".//AskForGoldSave");
                XmlNode SRCLoadtimesNode = settings.SelectSingleNode(".//SRCLoadtimes");
                XmlNode Ordered = settings.SelectSingleNode(".//Ordered");

                bool isIntroStart = false;
                bool isCreativeStart = false;
                bool isReset = false;
                bool isAskForGoldSave = false;
                bool isSRCLoadtimes = false;
                bool isOrdered = false;

                if (introStartNode != null)
                    bool.TryParse(introStartNode.InnerText, out isIntroStart);
                if (creativeStartNode != null)
                    bool.TryParse(creativeStartNode.InnerText, out isCreativeStart);   
                if (resetNode != null)
                    bool.TryParse(resetNode.InnerText, out isReset);
                if (askForGoldSaveNode != null)
                    bool.TryParse(askForGoldSaveNode.InnerText, out isAskForGoldSave);
                if (SRCLoadtimesNode != null)
                    bool.TryParse(SRCLoadtimesNode.InnerText, out isSRCLoadtimes);
                if (Ordered != null)
                    bool.TryParse(Ordered.InnerText, out isOrdered);

                introStart = isIntroStart;
                creativeStart = isCreativeStart;
                reset = isReset;
                askForGoldSave = isAskForGoldSave;
                SRCLoadtimes = isSRCLoadtimes;
                this.Ordered = isOrdered;

                Splits.Clear();
                XmlNodeList splitNodes = settings.SelectNodes(".//Splits/Split");

                foreach (XmlNode splitNode in splitNodes)
                {
                    bool onlySplitOnce = true;

                    string name = splitNode.SelectSingleNode("Name")?.InnerText;
                    bool.TryParse(splitNode.SelectSingleNode("OnlySplitOnce")?.InnerText, out onlySplitOnce);
                    string value = splitNode.SelectSingleNode("Value")?.InnerText;
                    

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
                        continue;

                    var splitName = SubnauticaItemSplit.GetSplitName(name);

                    switch (splitName)
                    {
                        case SplitName.Inventory:
                            var item = SubnauticaItemSplit.GetTechType(value);
                            Splits.Add(new ItemSplit(item.ConvertTo<InventoryItem>(), onlySplitOnce));
                            break;
                        case SplitName.Blueprint:
                            var blueprint = SubnauticaItemSplit.GetTechType(value);
                            Splits.Add(new BlueprintSplit(blueprint.ConvertTo<Unlockable>(), onlySplitOnce));
                            break;
                        case SplitName.Encyclopedia:
                            var encyEntry = SubnauticaItemSplit.GetEncyEntry(value);
                            Splits.Add(new EncySplit(encyEntry, onlySplitOnce));
                            break;
                        default:
                            Splits.Add(new PrefabSplit(splitName, onlySplitOnce));
                            break;
                    }
                }
            }
            else
            {
                // no splits settings, default
                introStart = false;
                creativeStart = false;
                Splits.Clear();
            }

            isLoading.ExitWriteLock();
        }
    }
    public sealed class ComboItem<T>
    {
        public T Value { get; set; }
        public string Display { get; set; }
    }
}
