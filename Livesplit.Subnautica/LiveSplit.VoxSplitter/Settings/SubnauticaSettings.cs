using Livesplit;
using LiveSplit.ComponentUtil;
using LiveSplit.Model;
using LiveSplit.Options;
using LiveSplit.UI;
using LiveSplit.UI.Components.AutoSplit;
using LiveSplit.VoxSplitter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static Livesplit.Subnautica.SubnauticaSplitSettings;
using static System.Windows.Forms.AxHost;

namespace Livesplit.Subnautica
{
    public partial class SubnauticaSettings : UserControl
    {
        public List<SplitName> Splits { get; private set; }
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
            Splits = new List<SplitName>();
            _state = state;
        }

        #region Buttons
        private void btnAddSplit_Click(object sender, EventArgs e)
        {
            SubnauticaSplitSettings setting = createSetting();
            flowMain.Controls.Add(setting);
            UpdateSplits();
        }

        public void btnRemove_Click(object sender, EventArgs e)
        {
            for (int i = flowMain.Controls.Count - 1; i > 0; i--)
            {
                if (flowMain.Controls[i].Contains((Control)sender))
                {
                    RemoveHandlers((SubnauticaSplitSettings)((Button)sender).Parent);

                    flowMain.Controls.RemoveAt(i);
                    break;
                }
            }
            UpdateSplits();
        }

        public void btnEdit_Click(object sender, EventArgs e)
        {
            for (int i = flowMain.Controls.Count - 1; i > 0; i--)
            {
                if (flowMain.Controls[i].Contains((Control)sender))
                {
                    SubnauticaSplitSettings setting = (SubnauticaSplitSettings)((Button)sender).Parent;
                    if (setting.cboName.Enabled)
                    {
                        disableEdit(setting);
                    }
                    else
                    {
                        enableEdit(setting);
                    }
                    break;
                }
            }
        }
        private void btnAddAbove_Click(object sender, EventArgs e)
        {
            for (int i = flowMain.Controls.Count - 1; i > 0; i--)
            {
                if (flowMain.Controls[i].Contains((Control)sender))
                {
                    SubnauticaSplitSettings setting = (SubnauticaSplitSettings)((Button)sender).Parent;
                    int index = setting.Parent.Controls.GetChildIndex(setting);
                    addSplitAtIndex(index);
                }
            }
        }
        private void btnAddBelow_Click(object sender, EventArgs e)
        {
            for (int i = flowMain.Controls.Count - 1; i > 0; i--)
            {
                if (flowMain.Controls[i].Contains((Control)sender))
                {
                    SubnauticaSplitSettings setting = (SubnauticaSplitSettings)((Button)sender).Parent;
                    int index = setting.Parent.Controls.GetChildIndex(setting);
                    addSplitAtIndex(index + 1);
                }
            }
        }

        private void btnAddExplo_Click(object sender, EventArgs e)
        {
            if(_state == null)
                return;

            var componentPath = @"Components\\SubnauticaShipExplosionInfo.dll";
            var exploTimeComponent = _state.Layout.LayoutComponents.Where(x => x.Component.GetType().FullName == "LiveSplit.UI.Components.Component").FirstOrDefault();

            if (!File.Exists(componentPath)) { MessageBox.Show($"File does not exist: {componentPath}"); return; }

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
        /*private void ButtonSplitGenerator_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Generating the splits will overwrite the existing splits and times, do you want to overwrite them?",
                "Generate Splits?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            using (SplitsGenerator splitGen = new SplitsGenerator())
            {
                int maxWidth = 0;
                foreach (string split in Splits)
                {
                    string splitName = settingsDict[split].Text;
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
                state.Run.Clear();
                foreach (ListViewItem item in splitGen.ListView.Items)
                {
                    state.Run.AddSegment(item.Text);
                }
                state.Form.Refresh();
            }
        }*/
        #endregion
        public void UpdateExploBtnContent()
        {
            if (_state?.Layout.LayoutComponents.Where(x => x.Component.GetType().FullName == "LiveSplit.UI.Components.Component").FirstOrDefault() != null)
                btnAddExplo.Text = "Remove Explosion Time";
            else
                btnAddExplo.Text = "Add Explosion Time";
        }
        private void addSplitAtIndex(int index)
        {
            SubnauticaSplitSettings setting = createSetting();
            flowMain.Controls.Add(setting);
            flowMain.Controls.SetChildIndex(setting, index);
            UpdateSplits();
        }
        private void enableEdit(SubnauticaSplitSettings setting)
        {
            string currentText = setting.cboName.Text;
            setting.btnEdit.Text = "✔";
            setting.cboName.DataSource = GetAvailableSplits();
            setting.cboName.Text = currentText;
            setting.cboName.Enabled = true;
        }
        private void disableEdit(SubnauticaSplitSettings setting)
        {
            setting.btnEdit.Text = "✏";
            setting.cboName.Enabled = false;
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
                if (c is SubnauticaSplitSettings)
                {
                    SubnauticaSplitSettings setting = (SubnauticaSplitSettings)c;
                    if (!string.IsNullOrEmpty(setting.cboName.Text))
                    {
                        SplitName split = GetSplitName(setting.cboName.Text);
                        Splits.Add(split);
                    }
                }
            }

            isLoading.ExitWriteLock();
        }
        

        private void AddHandlers(SubnauticaSplitSettings setting)
        {
            setting.cboName.SelectedIndexChanged += new EventHandler(ControlChanged);
            setting.btnRemove.Click += new EventHandler(btnRemove_Click);
            setting.btnEdit.Click += new EventHandler(btnEdit_Click);
            setting.btnAddAbove.Click += new EventHandler(btnAddAbove_Click);
            setting.btnAddBelow.Click += new EventHandler(btnAddBelow_Click);
        }
        private void RemoveHandlers(SubnauticaSplitSettings setting)
        {
            setting.cboName.SelectedIndexChanged -= ControlChanged;
            setting.btnRemove.Click -= btnRemove_Click;
            setting.btnEdit.Click -= btnEdit_Click;
            setting.btnAddAbove.Click -= btnAddAbove_Click;
            setting.btnAddBelow.Click -= btnAddBelow_Click;
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

            foreach (SplitName split in Splits)
            {
                MemberInfo info = typeof(SplitName).GetMember(split.ToString())[0];
                DescriptionAttribute description = (DescriptionAttribute)info.GetCustomAttributes(typeof(DescriptionAttribute), false)[0];

                SubnauticaSplitSettings setting = new SubnauticaSplitSettings();
                setting.cboName.DataSource = new List<string>() { description.Description };
                setting.cboName.Enabled = false;
                setting.cboName.Text = description.Description;
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

        private SubnauticaSplitSettings createSetting()
        {
            SubnauticaSplitSettings setting = new SubnauticaSplitSettings();
            List<string> splitNames = GetAvailableSplits();
            setting.cboName.DataSource = splitNames;
            setting.cboName.Text = splitNames[0];
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
                    availableSplits.Add(description.Description);
                    availableSplitsAlphaSorted.Add(description.Description);
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
                if (c is SubnauticaSplitSettings)
                {
                    SubnauticaSplitSettings setting = (SubnauticaSplitSettings)c;
                    if (setting.cboName.Enabled)
                    {
                        string text = setting.cboName.Text;
                        setting.cboName.DataSource = GetAvailableSplits();
                        setting.cboName.Text = text;
                    }
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

            foreach (SplitName split in Splits)
            {
                XmlElement xmlSplit = document.CreateElement("Split");
                xmlSplit.InnerText = split.ToString();

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
                    string splitDescription = splitNode.InnerText;
                    SplitName split = GetSplitName(splitDescription);
                    Splits.Add(split);
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
}
