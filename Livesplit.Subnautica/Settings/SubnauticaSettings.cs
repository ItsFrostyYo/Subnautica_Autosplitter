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
        public HashSet<TechType> InvItems() => Splits.OfType<ItemSplit>().Select(s => s.Item).ToHashSet();
        public List<ComboItem<TechType>> Items;
        public List<ComboItem<TechType>> ItemsAlphaSorted;
        public bool introStart { get; set; }
        public bool creativeStart { get; set; }
        public bool reset {  get; set; }
        public bool askForGoldSave { get; set; }
        public bool SRCLoadtimes { get; set; }
        private LiveSplitState _state;

        private static ReaderWriterLockSlim isLoading = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private List<string> availableSplits = new List<string>();
        private List<string> availableSplitsAlphaSorted = new List<string>();
        public SubnauticaSettings(LiveSplitState state)
        {
            InitializeComponent();            
            Splits = new List<SubnauticaSplit>();
            _state = state;

            Items = Enum.GetValues(typeof(TechType))
                        .Cast<TechType>()
                        .Select(t => new ComboItem<TechType> { Value = t, Display = Localization.GetDisplayName(t) })
                        .ToList();

            ItemsAlphaSorted = Items.OrderBy(x => x.Display).ToList();
        }

        #region Buttons
        private void btnAddSplit_Click(object sender, EventArgs e)
        {
            var setting = createItemSplit();
            flowMain.Controls.Add(setting);
            UpdateSplits();
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
            if (setting is SubnauticaItemSplit itemSplit)
            {
                var combo = itemSplit.cboItem;
                var prev = combo.SelectedValue;

                combo.DisplayMember = "Display";
                combo.ValueMember = "Value";
                combo.DataSource = rdAlpha.Checked ? ItemsAlphaSorted : Items;

                if (prev is TechType prevTech)
                    combo.SelectedValue = prevTech;
            }
            else
            {
                setting.ComboBox.DataSource = GetAvailableSplits();
            }
            setting.ComboBox.Enabled = true;
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

            foreach (var split in Splits)
            {
                SubnauticaSplitSetting setting;

                switch (split)
                {
                    case ItemSplit itemSplit:
                        setting = new SubnauticaItemSplit();
                        setting.CbSplitOnce.Checked = itemSplit.OnlySplitOnce;
                        var data = rdAlpha.Checked ? ItemsAlphaSorted : Items;
                        var combo = ((SubnauticaItemSplit)setting).cboItem;

                        combo.DisplayMember = "Display";
                        combo.ValueMember = "Value";
                        combo.DataSource = data;

                        combo.SelectedValue = itemSplit.Item;
                        break;

                    default:
                        setting = new SubnauticaPrefabSplit();
                        var desc = split.GetDescription();
                        setting.CbSplitOnce.Checked = split.OnlySplitOnce;
                        setting.ComboBox.DataSource = new List<string> { desc };
                        setting.ComboBox.Text = desc;
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

        private SubnauticaPrefabSplit createPrefabSplit()
        {
            SubnauticaPrefabSplit setting = new SubnauticaPrefabSplit();
            List<string> splitNames = GetAvailableSplits();
            setting.cboName.DataSource = splitNames;
            setting.cboName.Text = splitNames[0];
            setting.btnEdit.Text = "✔";
            AddHandlers(setting);
            return setting;
        }

        private SubnauticaItemSplit createItemSplit()
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

        private List<string> GetAvailableSplits()
        {
            if (availableSplits.Count == 0)
            {
                foreach (SplitName split in Enum.GetValues(typeof(SplitName)))
                {
                    MemberInfo info = typeof(SplitName).GetMember(split.ToString())[0];
                    DescriptionAttribute description = (DescriptionAttribute)info.GetCustomAttributes(typeof(DescriptionAttribute), false)[0];
                    if ((int)split > 1) availableSplits.Add(description.Description);
                    if ((int)split > 1) availableSplitsAlphaSorted.Add(description.Description);
                }
                availableSplitsAlphaSorted.Sort(delegate (string one, string two)
                {
                    return one.CompareTo(two);
                });
            }
            return rdAlpha.Checked ? availableSplitsAlphaSorted : availableSplits;
        }

        private void radio_CheckedChanged(object sender, EventArgs e)
        {
            foreach (Control c in flowMain.Controls)
            {
                if (c is SubnauticaPrefabSplit s && s.cboName.Enabled)
                {
                    var text = s.cboName.Text;
                    s.cboName.DataSource = GetAvailableSplits();
                    s.cboName.Text = text;
                    SubnauticaPrefabSplit setting = (SubnauticaPrefabSplit)c;
                }
                if (c is SubnauticaItemSplit si && si.cboItem.Enabled)
                {
                    var combo = si.cboItem;
                    var prev = combo.SelectedValue;

                    combo.DisplayMember = "Display";
                    combo.ValueMember = "Value";
                    combo.DataSource = rdAlpha.Checked ? ItemsAlphaSorted : Items;

                    if (prev is TechType prevTech)
                        combo.SelectedValue = prevTech;
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
            SubnauticaPrefabSplit data = (SubnauticaPrefabSplit)e.Data.GetData(typeof(SubnauticaPrefabSplit));
            FlowLayoutPanel destination = (FlowLayoutPanel)sender;
            Point p = destination.PointToClient(new Point(e.X, e.Y));
            var item = destination.GetChildAtPoint(p);
            int index = destination.Controls.GetChildIndex(item, false);
            if (index == 0)
            {
                e.Effect = DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.Move;
                int oldIndex = destination.Controls.GetChildIndex(data);
                if (oldIndex != index)
                {
                    enableEdit(data);
                    destination.Controls.SetChildIndex(data, index);
                    destination.Invalidate();
                }
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
                    default:
                        xmlValue.InnerText = string.Empty;
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

                bool isIntroStart = false;
                bool isCreativeStart = false;
                bool isReset = false;
                bool isAskForGoldSave = false;
                bool isSRCLoadtimes = false;

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

                introStart = isIntroStart;
                creativeStart = isCreativeStart;
                reset = isReset;
                askForGoldSave = isAskForGoldSave;
                SRCLoadtimes = isSRCLoadtimes;

                Splits.Clear();
                XmlNodeList splitNodes = settings.SelectNodes(".//Splits/Split");

                foreach (XmlNode splitNode in splitNodes)
                {
                    bool onlySplitOnce = true;

                    string name = splitNode.SelectSingleNode("Name")?.InnerText;
                    bool.TryParse(splitNode.SelectSingleNode("OnlySplitOnce")?.InnerText, out onlySplitOnce);
                    string value = splitNode.SelectSingleNode("Value")?.InnerText;
                    

                    if (string.IsNullOrEmpty(name))
                        continue;

                    if (!string.IsNullOrEmpty(value))
                    {
                        var splitName = SubnauticaItemSplit.GetSplitName(name);
                        var techType = SubnauticaItemSplit.GetTechType(value);
                        Splits.Add(new ItemSplit(techType, onlySplitOnce));
                    }
                    else
                    {
                        var splitName = SubnauticaItemSplit.GetSplitName(name);
                        Splits.Add(new PrefabSplit(splitName, onlySplitOnce));
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
