using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.VoxSplitter {

    public partial class TreeSettings : Settings {

        protected const string SortType = "Type", SortAlphabet = "Alphabet";
        protected const string ShowAll = "All", ShowCheck = "Check", ShowUncheck = "Uncheck";

        protected string sortName;
        protected string showName;

        protected readonly bool defIcons;
        protected readonly int defTips;

        protected readonly int itemHeight;

        protected PopupForm tooltipForm;
        protected TooltipSettings tooltipSettings;

        protected readonly Dictionary<string, NewTreeNode> settingsDict;
        protected readonly HashSet<string> parentList;

        protected LabelHashSet<string> splits;
        public override HashSet<string> Splits => splits;

        public Dictionary<string, string> convertableSettings;

        protected readonly LiveSplitState state;

        protected bool Icons {
            get => !CheckBoxIcons.IsDisposed && CheckBoxIcons.Checked;
            set => CheckBoxIcons.Checked = value;
        }

        protected int Tips {
            get => ComboBoxTip.IsDisposed ? -1 : ComboBoxTip.SelectedIndex;
            set => ComboBoxTip.SelectedIndex = value;
        }

        public TreeSettings(LiveSplitState state, SettingInfo? start, SettingInfo? reset, OptionsInfo? options, int defPreset = 0) : base(start, reset, options) {
            this.state = state; 
            int height = Height;

            InitializeComponent();

            defIcons = false;
            defTips = ComboBoxTip.Items.Count - 1;

            itemHeight = TreeCustomSettings.ItemHeight;

            TableLayoutTreeSettings.Padding = new Padding(0, height, 0, 0);

            sortName = SortType;
            showName = ShowAll;

            settingsDict = new Dictionary<string, NewTreeNode>();
            parentList = new HashSet<string>();
            splits = new LabelHashSet<string>(LabelSplitCount, ButtonSplitGenerator);

            XmlDocument settingsXML = new XmlDocument();
            settingsXML.Load(Factory.ExAssembly.GetManifestResourceStream(Factory.ExAssembly.GetName().Name + ".Splits.Splits.xml"));
            SetupSettings(settingsXML.SelectNodes("Splits/Split"));

            SetupImages();

            tooltipSettings = new TooltipSettings();

            tooltipForm = new TooltipForm();
            tooltipForm.Activate();
            tooltipForm.Hide();
            tooltipForm.Controls.Add(tooltipSettings);

            Icons = defIcons;
            Tips = defTips;

            ComboBoxPreset.SelectedIndex = defPreset;

            XmlElement splitterSetting = state.Run?.AutoSplitterSettings;
            if(splitterSetting != null) {
                SetSettings(splitterSetting);
            }
        }

        protected void SetupSettings(XmlNodeList nodeList, string parent = null) {
            if(!String.IsNullOrEmpty(parent)) {
                parentList.Add(parent);
            }

            foreach(XmlNode node in nodeList) {
                string name = node.Attributes["name"].Value;
                string text = node.Attributes["text"].Value;

                NewTreeNode treeNode = new NewTreeNode(name, text, parent, node.Attributes["ico"]?.Value, node.Attributes["tip"]?.Value, node.Attributes["img"]?.Value);

                settingsDict.Add(name, treeNode);
                if(node.ChildNodes.Count > 0) {
                    SetupSettings(node.ChildNodes, name);
                }
            }
        }

        protected void SetupImages() {
            HashSet<string> icoNodes = new HashSet<string>();
            HashSet<string> tipNodes = new HashSet<string>();
            foreach(KeyValuePair<string, NewTreeNode> kvp in settingsDict) {
                if(!String.IsNullOrEmpty(kvp.Value.ImageKey)) {
                    icoNodes.Add(kvp.Value.ImageKey);
                }
                if(!String.IsNullOrEmpty(kvp.Value.ToolTipKey)) {
                    tipNodes.Add(kvp.Value.ToolTipKey);
                }
            }

            if(icoNodes.Count == 0 && tipNodes.Count == 0) {
                GroupBoxImages.Dispose();
            } else {
                Task.Factory.StartNew(() => DownloadImages(icoNodes, tipNodes));
                
                if(icoNodes.Count == 0) {
                    CheckBoxIcons.Dispose();
                    TableLayoutPanelImages.RowStyles.RemoveAt(0);
                }

                if(tipNodes.Count == 0) {
                    GroupBoxTip.Dispose();
                    TableLayoutPanelImages.RowStyles.RemoveAt(1);
                }
            }
        }

        private void DownloadImages(HashSet<string> icoNames, HashSet<string> tipNames) {
            try {
                if(!Directory.Exists(Factory.ExAssembly.ResourcesPath())) {
                    Directory.CreateDirectory(Factory.ExAssembly.ResourcesPath());
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(Path.Combine(Factory.ExAssembly.ResourcesURL(), "ResourcesUpdate.xml"));
                Version newerVer = Version.Parse(doc.SelectSingleNode("updates/update").Attributes["version"].InnerText);

                string versionPath = Path.Combine(Factory.ExAssembly.ResourcesPath(), "Version");
                if(File.Exists(versionPath)) {
                    Version resourcesVer = new Version(File.ReadAllText(versionPath));

                    if(newerVer > resourcesVer) {
                        HashSet<string> changedFiles = new HashSet<string>();
                        HashSet<string> removedFiles = new HashSet<string>();
                        foreach(XmlNode updateNode in doc.SelectNodes("updates/update")) {
                            if(Version.Parse(updateNode.Attributes["version"].InnerText) <= resourcesVer) {
                                break;
                            }

                            foreach(XmlNode fileNode in updateNode["files"].ChildNodes) {
                                string filePath = fileNode.Attributes["path"].InnerText;
                                if(fileNode.Attributes["status"].InnerText.Equals("changed")) {
                                    if(!removedFiles.Contains(filePath)) {
                                        changedFiles.Add(filePath);
                                    }
                                } else {
                                    if(!changedFiles.Contains(filePath)) {
                                        removedFiles.Add(filePath);
                                    }
                                }
                            }
                        }

                        foreach(string path in changedFiles) {
                            DownloadFile(Path.Combine(Factory.ExAssembly.ResourcesURL(), path), Path.Combine(Factory.ExAssembly.ResourcesPath(), path));
                        }

                        foreach(string path in removedFiles) {
                            File.Delete(Path.Combine(Factory.ExAssembly.ResourcesPath(), path));
                        }

                        File.WriteAllText(versionPath, newerVer.ToString(3));
                    }
                } else {
                    File.WriteAllText(versionPath, newerVer.ToString(3));
                }

                foreach(string icoName in icoNames) {
                    IconList.Images.Add(icoName, new Bitmap(DownloadImage(icoName, "Icons")));
                }

                foreach(string tipName in tipNames) {
                    DownloadImage(tipName, "Tooltips");
                }
            } catch(Exception e) { LiveSplit.Options.Log.Error(e); }
        }

        private string DownloadImage(string name, string directory) {
            string imgPath = Path.Combine(Factory.ExAssembly.ResourcesPath(), directory, name);

            if(!File.Exists(imgPath)) {
                DownloadFile(Path.Combine(Factory.ExAssembly.ResourcesURL(), directory, name), imgPath);
            }

            return imgPath;
        }

        private void DownloadFile(string url, string path) {
            string dir = Path.GetDirectoryName(path);
            if(!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            using(WebClient webClient = new WebClient()) {
                webClient.DownloadFile(url, path);
            }
        }

        protected Bitmap GetTooltipImage(string name) {
            string path = Path.Combine(Factory.ExAssembly.ResourcesPath(), "Tooltips", name);
            return File.Exists(path) ? new Bitmap(path) : null;
        }

        public override XmlNode GetSettings(XmlDocument doc) {
            XmlElement xmlElement = (XmlElement)base.GetSettings(doc);

            if(Icons) { xmlElement.AppendChild(ToElement(doc, "Icons", 1)); }

            if(Tips >= 0 && Tips != defTips) { xmlElement.AppendChild(ToElement(doc, "Tips", Tips)); }

            XmlElement xmlSplits = doc.CreateElement("Splits");
            foreach(KeyValuePair<string, NewTreeNode> setting in settingsDict) {
                if(setting.Value.Checked) {
                    xmlSplits.AppendChild(ToElement(doc, "Split", setting.Key));
                }
            }
            xmlElement.AppendChild(xmlSplits);
            return xmlElement;
        }

        public override void SetSettings(XmlNode settings) {
            base.SetSettings(settings);

            Icons = settings["Icons"] != null;
            if(settings["Tips"] != null) { Tips = Int32.Parse(settings["Tips"].InnerText); }

            XmlNodeList componentList, aslList = null;
            if((componentList = settings.SelectNodes("Splits/Split")).Count > 0
            || (aslList = settings.SelectNodes("CustomSettings/Setting")).Count > 0) {
                foreach(string split in settingsDict.Keys) {
                    settingsDict[split].Checked = false;
                }
                splits.Clear();

                if(componentList.Count > 0) {
                    foreach(XmlNode node in componentList) {
                        HandleSetting(node.InnerText);
                    }
                } else {
                    foreach(XmlNode node in aslList) {
                        if(!Boolean.Parse(node.InnerText)) {
                            continue;
                        }
                        HandleSetting(node.Attributes["id"].Value);
                    }
                }

                void HandleSetting(string setting) {
                    if(convertableSettings != null) {
                        foreach(KeyValuePair<string, string> kvp in convertableSettings) {
                            if(setting.StartsWith(kvp.Key)) {
                                setting = kvp.Value + setting.Substring(kvp.Key.Length);
                                break;
                            }
                        }
                    }
                    if(!parentList.Contains(setting) && settingsDict.ContainsKey(setting)) {
                        settingsDict[setting].Checked = true;
                        splits.Add(setting);
                    }
                }
            }
        }

        protected override void Settings_Load(object sender, EventArgs e) {
            base.Settings_Load(sender, e);

            TreeCustomSettings.ItemHeight = itemHeight;

            UpdateSorting(true);

            EnableIcons(Icons);
        }

        protected void TreeCustomSettings_DrawNode(object sender, DrawTreeNodeEventArgs e) {
            NewTreeNode node = (NewTreeNode)e.Node;
            if(node.Nodes.Count > 0) {
                NativeMethods.HideCheckBox(node);
            }

            int startX = node.Bounds.X - NodeImageOffset(node);

            Rectangle bounds = new Rectangle(startX, node.Bounds.Y, Width, node.Bounds.Height);
            e.Graphics.FillRectangle(SystemBrushes.Window, bounds);

            if(node.IsSelected) {
                bounds.Width = TextRenderer.MeasureText(node.Text, TreeCustomSettings.Font).Width;

                e.Graphics.FillRectangle(SystemBrushes.Highlight, bounds);

                using(Pen focusPen = new Pen(Color.Black)) {
                    focusPen.DashStyle = DashStyle.Dot;

                    bounds.Width--; bounds.Height--;
                    e.Graphics.DrawRectangle(focusPen, bounds);
                }
            }

            int startY = node.Bounds.Y + (node.Bounds.Height - node.TreeView.Font.Height) / 2;
            TextRenderer.DrawText(
                e.Graphics, node.Text, TreeCustomSettings.Font,
                new Point(startX, startY),
                node.IsSelected ? SystemColors.Window : SystemColors.ControlText
            );
        }

        protected int NodeImageOffset(TreeNode n) {
            return 3 + ((!Icons || String.IsNullOrEmpty(n.ImageKey)) ? IconList.ImageSize.Width : 0);
        }

        protected void CheckBoxIcons_CheckedChanged(object sender, EventArgs e) {
            EnableIcons(((CheckBox)sender).Checked);
        }

        protected void EnableIcons(bool value) {
            TreeCustomSettings.ItemHeight = value ? IconList.ImageSize.Height + 2 : itemHeight;
        }

        protected void TreeCustomSettings_BeforeCheck(object sender, TreeViewCancelEventArgs e) {
            e.Cancel = e.Node.Nodes.Count > 0;
        }

        protected void TreeCustomSettings_AfterCheck(object sender, TreeViewEventArgs e) {
            if(e.Node.Checked) {
                splits.Add(e.Node.Name);
            } else {
                splits.Remove(e.Node.Name);
            }

            if(showName != ShowAll) {
                TreeNode node = settingsDict[e.Node.Name];
                while(node.Parent != null && node.Nodes.Count == 0) {
                    TreeNode parent = node.Parent;
                    parent.Nodes.Remove(node);
                    node = parent;
                }
                if(node.Parent == null && node.Nodes.Count == 0) {
                    TreeCustomSettings.Nodes.Remove(node);
                }
            }

            UpdatePreset();
        }

        protected override void ComboBoxPreset_SelectedIndexChanged(object sender, EventArgs e) {
            base.ComboBoxPreset_SelectedIndexChanged(sender, e);

            if(ComboBoxPreset.Text.Equals(CustomPreset)) {
                return;
            }

            HashSet<string> presetList = presetsDict[ComboBoxPreset.SelectedItem.ToString()];
            splits.Set(presetList);

            bool hasChangedSetting = false;
            TreeCustomSettings.AfterCheck -= TreeCustomSettings_AfterCheck;
            foreach(NewTreeNode node in settingsDict.Values) {
                bool isInPreset = presetList.Contains(node.Name);
                if((isInPreset && !node.Checked) || (!isInPreset && node.Checked)) {
                    node.Checked = isInPreset;
                    hasChangedSetting = true;
                }
            }
            TreeCustomSettings.AfterCheck += TreeCustomSettings_AfterCheck;
            if(hasChangedSetting) {
                UpdateSorting();
            }
        }

        protected override void CheckAll(bool value) {
            if(value) {
                splits.Set(settingsDict.Where(s => !parentList.Contains(s.Key)).Select(s => s.Key));
            } else {
                splits.Clear();
            }

            bool hasChangedSetting = false;

            TreeCustomSettings.AfterCheck -= TreeCustomSettings_AfterCheck;
            foreach(KeyValuePair<string, NewTreeNode> setting in settingsDict) {
                if(!parentList.Contains(setting.Key)) {
                    if(setting.Value.Checked != value) {
                        setting.Value.Checked = value;
                        hasChangedSetting = true;
                    }
                }
            }
            TreeCustomSettings.AfterCheck += TreeCustomSettings_AfterCheck;

            if(hasChangedSetting) {
                UpdateSorting();
            }

            UpdatePreset();
        }

        protected void RadioButtonSort_CheckedChanged(object sender, EventArgs e) {
            if(!((RadioButton)sender).Checked) { return; }

            sortName = ((RadioButton)sender).Text;

            UpdateSorting(true);
        }

        protected void RadioButtonShow_CheckedChanged(object sender, EventArgs e) {
            if(!((RadioButton)sender).Checked) { return; }

            showName = ((RadioButton)sender).Text;

            UpdateSorting(true);
        }

        protected void UpdateSorting(bool forceSorting = false) {
            if(!forceSorting && showName == ShowAll) { return; }

            TreeCustomSettings.SuspendLayout();
            SuspendAllDrawing();
            TreeCustomSettings.Nodes.Clear();
            List<NewTreeNode> rootSettings = new List<NewTreeNode>();

            if(sortName == SortType) {
                if(showName == ShowAll) {
                    foreach(var kvp in settingsDict) {
                        NewTreeNode node = kvp.Value;
                        node.Nodes.Clear();
                        if(node.ParentName != null) {
                            settingsDict[node.ParentName].Nodes.Add(node);
                        } else {
                            rootSettings.Add(node);
                        }
                    }
                } else {
                    bool showCheck = showName == ShowCheck;

                    foreach(KeyValuePair<string, NewTreeNode> kvp in settingsDict) {
                        kvp.Value.Nodes.Clear();
                    }

                    foreach(KeyValuePair<string, NewTreeNode> kvp in settingsDict.Reverse()) {
                        NewTreeNode node = kvp.Value;
                        if((!parentList.Contains(node.Name) && node.Checked == showCheck) || (parentList.Contains(node.Name) && node.Nodes.Count > 0)) {
                            if(node.ParentName != null) {
                                settingsDict[node.ParentName].Nodes.Insert(0, node);
                            } else {
                                rootSettings.Insert(0, node);
                            }
                        }
                    }
                }
            } else if(sortName == SortAlphabet) {
                foreach(KeyValuePair<string, NewTreeNode> kvp in settingsDict) {
                    kvp.Value.Nodes.Clear();
                    if(!parentList.Contains(kvp.Key) && (showName == ShowAll ||
                                                        (showName == ShowCheck && kvp.Value.Checked) ||
                                                        (showName == ShowUncheck && !kvp.Value.Checked))) {
                        rootSettings.Add(kvp.Value);
                    }
                }
                rootSettings = rootSettings.OrderBy(n => n.Text).ToList();
            }

            TreeCustomSettings.Nodes.AddRange(rootSettings.ToArray<TreeNode>());
            ResumeAllDrawing();
            TreeCustomSettings.ResumeLayout();
        }

        protected Point mousePos;
        protected void TreeCustomSettings_MouseMove(object sender, MouseEventArgs e) {
            if(mousePos == null || mousePos.Equals(MousePosition)) { return; }

            mousePos = MousePosition;

            UpdateTooltipFromMouse();
        }

        protected void TreeCustomSettings_Scroll(object sender, EventArgs e) {
            UpdateTooltipFromMouse();
        }

        protected void TreeCustomSettings_MouseLeave(object sender, EventArgs e) {
            tooltipForm.Hide();
            tooltipSettings.SetName(null);
        }

        protected void TreeCustomSettings_AfterSelect(object sender, TreeViewEventArgs e) {
            UpdateTooltip(e.Node);
        }

        protected void ComboBoxTip_SelectedIndexChanged(object sender, EventArgs e) {
            if(tooltipForm.Visible) {
                tooltipSettings.SetImage(tooltipSettings.GetImage(), Tips);
                SetTooltipPos(TreeCustomSettings.SelectedNode);
            }
        }

        protected void UpdateTooltipFromMouse() {
            Point mouseToTreePos = TreeCustomSettings.PointToClient(MousePosition);
            NewTreeNode node = (NewTreeNode)TreeCustomSettings.GetNodeAt(mouseToTreePos);
            if(node != null) {
                int boundLeft = node.Bounds.Left - (3 + IconList.ImageSize.Width);
                int boundRight = node.Bounds.Right - NodeImageOffset(node) + 3;

                if(mouseToTreePos.X >= boundLeft && mouseToTreePos.X <= boundRight) {
                    UpdateTooltip(node);
                    return;
                }
            }
            UpdateTooltip(null);
        }

        protected void UpdateTooltip(TreeNode n) {
            NewTreeNode node = (NewTreeNode)n;
            if((tooltipSettings.GetName() ?? "") != (node?.Text ?? "")) {
                if(String.IsNullOrEmpty(node?.ToolTipText) && String.IsNullOrEmpty(node?.ToolTipKey)) {
                    if(tooltipForm.Visible) {
                        tooltipForm.Hide();
                    }
                    tooltipSettings.SetName(node?.Text);
                } else {
                    Bitmap img = GetTooltipImage(node.ToolTipKey);
                    if(img == null && node.ToolTipText == null) {
                        return;
                    }

                    tooltipSettings.SetName(node.Text);
                    tooltipSettings.SetText(node.ToolTipText ?? node.Text);

                    if(!String.IsNullOrEmpty(node.ToolTipKey)) {
                        tooltipSettings.SetImage(img, Tips);
                    } else {
                        tooltipSettings.SetImage(null);
                    }

                    SetTooltipPos(node);

                    if(!tooltipForm.Visible) {
                        tooltipForm.Show(this);
                    }
                }
            }
        }

        protected void SetTooltipPos(TreeNode node) {
            Point nodeToScreenPos = TreeCustomSettings.PointToScreen(node.Bounds.Location);

            tooltipForm.Location = new Point(nodeToScreenPos.X + node.Bounds.Width - NodeImageOffset(node) + (int)AutoScaleDimensions.Width * 3,
                                             nodeToScreenPos.Y + node.Bounds.Height / 2 - tooltipSettings.Height / 2);
        }

        private void ButtonSplitGenerator_Click(object sender, EventArgs e) {
            if(MessageBox.Show("Generating the splits will overwrite the existing splits and times, do you want to overwrite them?",
                "Generate Splits?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) {
                return;
            }

            using(SplitsGenerator splitGen = new SplitsGenerator()) {
                int maxWidth = 0;
                foreach(string split in Splits) {
                    string splitName = settingsDict[split].Text;
                    splitGen.ListView.Items.Add(splitName);
                    int width = TextRenderer.MeasureText(splitName, splitGen.ListView.Font).Width;
                    if(width > maxWidth) {
                        maxWidth = width;
                    }
                }
                splitGen.ListView.Columns[0].Width = maxWidth + 10;
                splitGen.ListView.Size = new Size(maxWidth + 30, (int)Math.Min(splitGen.ListView.Items[0].Bounds.Height * (splitGen.ListView.Items.Count+1), Screen.PrimaryScreen.Bounds.Height * .75f));
                if(splitGen.ShowDialog() != DialogResult.OK) {
                    return;
                }
                //Doesn't work with subsplits + show last split
                state.Run.Clear();
                foreach(ListViewItem item in splitGen.ListView.Items) {
                    state.Run.AddSegment(item.Text);
                }
                state.Form.Refresh();
            }
        }

        protected void ButtonExpand_Click(object sender, EventArgs e) {
            SuspendAllDrawing();
            StoreTreeScroll();
            TreeCustomSettings.ExpandAll();
            RestoreTreeScroll();
            ResumeAllDrawing();
        }

        protected void ButtonCollapse_Click(object sender, EventArgs e) {
            SuspendAllDrawing();
            TreeCustomSettings.CollapseAll();
            ResumeAllDrawing();
        }

        protected void SuspendAllDrawing() {
            TreeCustomSettings.BeforeExpand -= SuspendDrawing;
            TreeCustomSettings.AfterExpand -= ResumeDrawing;
            TreeCustomSettings.BeforeCollapse -= SuspendDrawing;
            TreeCustomSettings.AfterCollapse -= ResumeDrawing;
            SuspendDrawing();
        }

        protected void ResumeAllDrawing() {
            ResumeDrawing();
            TreeCustomSettings.BeforeExpand += SuspendDrawing;
            TreeCustomSettings.AfterExpand += ResumeDrawing;
            TreeCustomSettings.BeforeCollapse += SuspendDrawing;
            TreeCustomSettings.AfterCollapse += ResumeDrawing;
        }

        TreeNode scrollNode = null;
        protected void StoreTreeScroll() {
            scrollNode = TreeCustomSettings?.TopNode;
        }

        protected void RestoreTreeScroll() {
            if(scrollNode == null) { return; }

            if((parentList.Contains(scrollNode.Name) && scrollNode.Nodes.Count != 0) || (!parentList.Contains(scrollNode.Name) && scrollNode.Parent == null)) {
                int id = Array.IndexOf(settingsDict.Values.ToArray(), scrollNode);
                int nodeOffset = 0;
                for(int offset = 1; offset < settingsDict.Count; offset++) {
                    nodeOffset += (offset % 2 == 0) ? +offset : -offset;
                    int nodeId = id + nodeOffset;
                    if(nodeId < 0 || nodeId >= settingsDict.Count) {
                        continue;
                    }
                    TreeNode node = settingsDict.Values.ElementAt(nodeId);
                    if(node.Parent != null || node.Nodes.Count != 0) {
                        scrollNode = node;
                        break;
                    }
                }
            }
            TreeCustomSettings.TopNode = scrollNode;
        }

        public class NewTreeView : TreeView {

            protected override void OnHandleCreated(EventArgs e) {
                NativeMethods.CreateHandler(Handle);
                base.OnHandleCreated(e);
            }

            public event EventHandler Scroll;

            private const int WM_VSCROLL = 0x115;
            private const int WM_LBUTTONDOWN = 0x201;
            private const int WM_LBUTTONDBLCLK = 0x203;
            private const int WM_MOUSEWHEEL = 0x20A;

            protected override void WndProc(ref Message m) {
                if(m.Msg == WM_LBUTTONDBLCLK) {
                    Point localPos = PointToClient(Cursor.Position);
                    TreeViewHitTestInfo hitTestInfo = HitTest(localPos);

                    if(hitTestInfo.Location == TreeViewHitTestLocations.StateImage) {
                        m.Msg = WM_LBUTTONDOWN;
                    }
                }

                try {
                    base.WndProc(ref m);
                } catch(Exception e) {
                    // Inconsistent? error at Custom Draw (WM_REFLECT + WM_NOTIFY)
                    // TODO Clean fix
                    LiveSplit.Options.Log.Error("TreeView " + e.ToString());
                }

                if(m.Msg == WM_VSCROLL || m.Msg == WM_MOUSEWHEEL) {
                    Scroll?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public class NewTreeNode : TreeNode {

            public string ParentName { get; }
            public string ToolTipKey { get; }

            public NewTreeNode(string name, string desc, string parent, string ico, string tipText, string tipImg) {
                Name = name;
                Text = desc;
                ParentName = parent;
                ToolTipText = tipText;
                SelectedImageKey = ImageKey = ico;
                ToolTipKey = tipImg;
            }
        }

        public class LabelHashSet<T> : HashSet<T> {

            private readonly Label label;

            private readonly Button generator;

            public LabelHashSet(Label label, Button generator = null) {
                this.label = label;
                this.generator = generator;
            }

            public void Set(IEnumerable<T> set) {
                base.Clear();
                UnionWith(set);
                SetText();
            }

            public new void Clear() {
                base.Clear();
                SetText();
            }

            public new bool Add(T item) {
                if(!base.Add(item)) { return false; }

                SetText();
                return true;
            }

            public new bool Remove(T item) {
                if(!base.Remove(item)) { return false; }

                SetText();
                return true;
            }

            private void SetText() {
                if(generator != null) {
                    generator.Enabled = Count > 0;
                }
                label.Text = Count.ToString();
            }
        }
    }
}