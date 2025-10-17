using System.Windows.Forms;

namespace LiveSplit.VoxSplitter {
    partial class TreeSettings {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if(disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.TableLayoutPanelTree = new System.Windows.Forms.TableLayoutPanel();
            this.TableLayoutPanelSplit = new System.Windows.Forms.TableLayoutPanel();
            this.LabelSplits = new System.Windows.Forms.Label();
            this.LabelSplitCount = new System.Windows.Forms.Label();
            this.ButtonSplitGenerator = new System.Windows.Forms.Button();
            this.GroupBoxImages = new System.Windows.Forms.GroupBox();
            this.TableLayoutPanelImages = new System.Windows.Forms.TableLayoutPanel();
            this.CheckBoxIcons = new System.Windows.Forms.CheckBox();
            this.GroupBoxTip = new System.Windows.Forms.GroupBox();
            this.ComboBoxTip = new System.Windows.Forms.ComboBox();
            this.GroupBoxSort = new System.Windows.Forms.GroupBox();
            this.RadioButtonType = new System.Windows.Forms.RadioButton();
            this.RadioButtonAlphabet = new System.Windows.Forms.RadioButton();
            this.GroupBoxShow = new System.Windows.Forms.GroupBox();
            this.RadioButtonAll = new System.Windows.Forms.RadioButton();
            this.RadioButtonCheck = new System.Windows.Forms.RadioButton();
            this.RadioButtonUncheck = new System.Windows.Forms.RadioButton();
            this.FlowLayoutPanelExpand = new System.Windows.Forms.FlowLayoutPanel();
            this.ButtonExpand = new System.Windows.Forms.Button();
            this.ButtonCollapse = new System.Windows.Forms.Button();
            this.TreeCustomSettings = new LiveSplit.VoxSplitter.TreeSettings.NewTreeView();
            this.IconList = new System.Windows.Forms.ImageList(this.components);
            this.TableLayoutTreeSettings = new System.Windows.Forms.TableLayoutPanel();
            this.TableLayoutPanelTree.SuspendLayout();
            this.TableLayoutPanelSplit.SuspendLayout();
            this.GroupBoxImages.SuspendLayout();
            this.TableLayoutPanelImages.SuspendLayout();
            this.GroupBoxTip.SuspendLayout();
            this.GroupBoxSort.SuspendLayout();
            this.GroupBoxShow.SuspendLayout();
            this.FlowLayoutPanelExpand.SuspendLayout();
            this.TableLayoutTreeSettings.SuspendLayout();
            this.SuspendLayout();
            // 
            // TableLayoutPanelTree
            // 
            this.TableLayoutPanelTree.AutoSize = true;
            this.TableLayoutPanelTree.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.TableLayoutPanelTree.ColumnCount = 1;
            this.TableLayoutPanelTree.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutPanelTree.Controls.Add(this.TableLayoutPanelSplit, 0, 0);
            this.TableLayoutPanelTree.Controls.Add(this.GroupBoxImages, 0, 1);
            this.TableLayoutPanelTree.Controls.Add(this.GroupBoxSort, 0, 2);
            this.TableLayoutPanelTree.Controls.Add(this.GroupBoxShow, 0, 3);
            this.TableLayoutPanelTree.Controls.Add(this.FlowLayoutPanelExpand, 0, 4);
            this.TableLayoutPanelTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TableLayoutPanelTree.Location = new System.Drawing.Point(4, 4);
            this.TableLayoutPanelTree.Margin = new System.Windows.Forms.Padding(4);
            this.TableLayoutPanelTree.Name = "TableLayoutPanelTree";
            this.TableLayoutPanelTree.RowCount = 5;
            this.TableLayoutPanelTree.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutPanelTree.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this.TableLayoutPanelTree.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.TableLayoutPanelTree.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 118F));
            this.TableLayoutPanelTree.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 74F));
            this.TableLayoutPanelTree.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.TableLayoutPanelTree.Size = new System.Drawing.Size(103, 622);
            this.TableLayoutPanelTree.TabIndex = 0;
            // 
            // TableLayoutPanelSplit
            // 
            this.TableLayoutPanelSplit.AutoSize = true;
            this.TableLayoutPanelSplit.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.TableLayoutPanelSplit.ColumnCount = 2;
            this.TableLayoutPanelSplit.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.TableLayoutPanelSplit.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutPanelSplit.Controls.Add(this.LabelSplits, 0, 0);
            this.TableLayoutPanelSplit.Controls.Add(this.LabelSplitCount, 1, 0);
            this.TableLayoutPanelSplit.Controls.Add(this.ButtonSplitGenerator, 0, 1);
            this.TableLayoutPanelSplit.Dock = System.Windows.Forms.DockStyle.Top;
            this.TableLayoutPanelSplit.Location = new System.Drawing.Point(0, 0);
            this.TableLayoutPanelSplit.Margin = new System.Windows.Forms.Padding(0);
            this.TableLayoutPanelSplit.Name = "TableLayoutPanelSplit";
            this.TableLayoutPanelSplit.RowCount = 2;
            this.TableLayoutPanelSplit.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutPanelSplit.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.TableLayoutPanelSplit.Size = new System.Drawing.Size(103, 89);
            this.TableLayoutPanelSplit.TabIndex = 0;
            // 
            // LabelSplits
            // 
            this.LabelSplits.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LabelSplits.AutoSize = true;
            this.LabelSplits.Location = new System.Drawing.Point(0, 12);
            this.LabelSplits.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
            this.LabelSplits.Name = "LabelSplits";
            this.LabelSplits.Size = new System.Drawing.Size(46, 17);
            this.LabelSplits.TabIndex = 0;
            this.LabelSplits.Text = "Splits:";
            // 
            // LabelSplitCount
            // 
            this.LabelSplitCount.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LabelSplitCount.AutoSize = true;
            this.LabelSplitCount.Location = new System.Drawing.Point(46, 12);
            this.LabelSplitCount.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
            this.LabelSplitCount.Name = "LabelSplitCount";
            this.LabelSplitCount.Size = new System.Drawing.Size(57, 17);
            this.LabelSplitCount.TabIndex = 0;
            this.LabelSplitCount.Text = "0";
            // 
            // ButtonSplitGenerator
            // 
            this.TableLayoutPanelSplit.SetColumnSpan(this.ButtonSplitGenerator, 2);
            this.ButtonSplitGenerator.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ButtonSplitGenerator.Location = new System.Drawing.Point(9, 41);
            this.ButtonSplitGenerator.Margin = new System.Windows.Forms.Padding(9, 12, 9, 3);
            this.ButtonSplitGenerator.Name = "ButtonSplitGenerator";
            this.ButtonSplitGenerator.Size = new System.Drawing.Size(85, 45);
            this.ButtonSplitGenerator.TabIndex = 1;
            this.ButtonSplitGenerator.Text = "Splits Generator";
            this.ButtonSplitGenerator.UseVisualStyleBackColor = true;
            this.ButtonSplitGenerator.Click += new System.EventHandler(this.ButtonSplitGenerator_Click);
            // 
            // GroupBoxImages
            // 
            this.GroupBoxImages.AutoSize = true;
            this.GroupBoxImages.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.GroupBoxImages.Controls.Add(this.TableLayoutPanelImages);
            this.GroupBoxImages.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.GroupBoxImages.Location = new System.Drawing.Point(0, 215);
            this.GroupBoxImages.Margin = new System.Windows.Forms.Padding(0, 0, 0, 7);
            this.GroupBoxImages.Name = "GroupBoxImages";
            this.GroupBoxImages.Padding = new System.Windows.Forms.Padding(4);
            this.GroupBoxImages.Size = new System.Drawing.Size(103, 118);
            this.GroupBoxImages.TabIndex = 0;
            this.GroupBoxImages.TabStop = false;
            this.GroupBoxImages.Text = "Images:";
            // 
            // TableLayoutPanelImages
            // 
            this.TableLayoutPanelImages.AutoSize = true;
            this.TableLayoutPanelImages.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.TableLayoutPanelImages.ColumnCount = 1;
            this.TableLayoutPanelImages.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutPanelImages.Controls.Add(this.CheckBoxIcons, 0, 0);
            this.TableLayoutPanelImages.Controls.Add(this.GroupBoxTip, 0, 1);
            this.TableLayoutPanelImages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TableLayoutPanelImages.Location = new System.Drawing.Point(4, 19);
            this.TableLayoutPanelImages.Margin = new System.Windows.Forms.Padding(0);
            this.TableLayoutPanelImages.Name = "TableLayoutPanelImages";
            this.TableLayoutPanelImages.RowCount = 2;
            this.TableLayoutPanelImages.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.TableLayoutPanelImages.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.TableLayoutPanelImages.Size = new System.Drawing.Size(95, 95);
            this.TableLayoutPanelImages.TabIndex = 0;
            // 
            // CheckBoxIcons
            // 
            this.CheckBoxIcons.Checked = true;
            this.CheckBoxIcons.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CheckBoxIcons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CheckBoxIcons.Location = new System.Drawing.Point(4, 4);
            this.CheckBoxIcons.Margin = new System.Windows.Forms.Padding(4);
            this.CheckBoxIcons.Name = "CheckBoxIcons";
            this.CheckBoxIcons.Size = new System.Drawing.Size(87, 30);
            this.CheckBoxIcons.TabIndex = 0;
            this.CheckBoxIcons.Text = "Icons";
            this.CheckBoxIcons.UseVisualStyleBackColor = true;
            this.CheckBoxIcons.CheckedChanged += new System.EventHandler(this.CheckBoxIcons_CheckedChanged);
            // 
            // GroupBoxTip
            // 
            this.GroupBoxTip.Controls.Add(this.ComboBoxTip);
            this.GroupBoxTip.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.GroupBoxTip.Location = new System.Drawing.Point(4, 43);
            this.GroupBoxTip.Margin = new System.Windows.Forms.Padding(4);
            this.GroupBoxTip.Name = "GroupBoxTip";
            this.GroupBoxTip.Padding = new System.Windows.Forms.Padding(4);
            this.GroupBoxTip.Size = new System.Drawing.Size(87, 48);
            this.GroupBoxTip.TabIndex = 0;
            this.GroupBoxTip.TabStop = false;
            this.GroupBoxTip.Text = "Tip Size:";
            // 
            // ComboBoxTip
            // 
            this.ComboBoxTip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ComboBoxTip.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBoxTip.FormattingEnabled = true;
            this.ComboBoxTip.Items.AddRange(new object[] {
            "Large",
            "Medium",
            "Small"});
            this.ComboBoxTip.Location = new System.Drawing.Point(4, 19);
            this.ComboBoxTip.Margin = new System.Windows.Forms.Padding(4);
            this.ComboBoxTip.Name = "ComboBoxTip";
            this.ComboBoxTip.Size = new System.Drawing.Size(79, 24);
            this.ComboBoxTip.TabIndex = 0;
            this.ComboBoxTip.SelectedIndexChanged += new System.EventHandler(this.ComboBoxTip_SelectedIndexChanged);
            // 
            // GroupBoxSort
            // 
            this.GroupBoxSort.Controls.Add(this.RadioButtonType);
            this.GroupBoxSort.Controls.Add(this.RadioButtonAlphabet);
            this.GroupBoxSort.Dock = System.Windows.Forms.DockStyle.Top;
            this.GroupBoxSort.Location = new System.Drawing.Point(0, 344);
            this.GroupBoxSort.Margin = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.GroupBoxSort.Name = "GroupBoxSort";
            this.GroupBoxSort.Padding = new System.Windows.Forms.Padding(4);
            this.GroupBoxSort.Size = new System.Drawing.Size(103, 79);
            this.GroupBoxSort.TabIndex = 0;
            this.GroupBoxSort.TabStop = false;
            this.GroupBoxSort.Text = "Sort:";
            // 
            // RadioButtonType
            // 
            this.RadioButtonType.Checked = true;
            this.RadioButtonType.Location = new System.Drawing.Point(8, 21);
            this.RadioButtonType.Margin = new System.Windows.Forms.Padding(4);
            this.RadioButtonType.Name = "RadioButtonType";
            this.RadioButtonType.Size = new System.Drawing.Size(65, 21);
            this.RadioButtonType.TabIndex = 0;
            this.RadioButtonType.TabStop = true;
            this.RadioButtonType.Text = "Type";
            this.RadioButtonType.UseVisualStyleBackColor = true;
            this.RadioButtonType.CheckedChanged += new System.EventHandler(this.RadioButtonSort_CheckedChanged);
            // 
            // RadioButtonAlphabet
            // 
            this.RadioButtonAlphabet.Location = new System.Drawing.Point(8, 49);
            this.RadioButtonAlphabet.Margin = new System.Windows.Forms.Padding(4);
            this.RadioButtonAlphabet.Name = "RadioButtonAlphabet";
            this.RadioButtonAlphabet.Size = new System.Drawing.Size(89, 21);
            this.RadioButtonAlphabet.TabIndex = 0;
            this.RadioButtonAlphabet.Text = "Alphabet";
            this.RadioButtonAlphabet.UseVisualStyleBackColor = true;
            this.RadioButtonAlphabet.CheckedChanged += new System.EventHandler(this.RadioButtonSort_CheckedChanged);
            // 
            // GroupBoxShow
            // 
            this.GroupBoxShow.Controls.Add(this.RadioButtonAll);
            this.GroupBoxShow.Controls.Add(this.RadioButtonCheck);
            this.GroupBoxShow.Controls.Add(this.RadioButtonUncheck);
            this.GroupBoxShow.Dock = System.Windows.Forms.DockStyle.Top;
            this.GroupBoxShow.Location = new System.Drawing.Point(0, 434);
            this.GroupBoxShow.Margin = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.GroupBoxShow.Name = "GroupBoxShow";
            this.GroupBoxShow.Padding = new System.Windows.Forms.Padding(4);
            this.GroupBoxShow.Size = new System.Drawing.Size(103, 107);
            this.GroupBoxShow.TabIndex = 0;
            this.GroupBoxShow.TabStop = false;
            this.GroupBoxShow.Text = "Show:";
            // 
            // RadioButtonAll
            // 
            this.RadioButtonAll.Checked = true;
            this.RadioButtonAll.Location = new System.Drawing.Point(8, 21);
            this.RadioButtonAll.Margin = new System.Windows.Forms.Padding(4);
            this.RadioButtonAll.Name = "RadioButtonAll";
            this.RadioButtonAll.Size = new System.Drawing.Size(48, 21);
            this.RadioButtonAll.TabIndex = 0;
            this.RadioButtonAll.TabStop = true;
            this.RadioButtonAll.Text = "All";
            this.RadioButtonAll.UseVisualStyleBackColor = true;
            this.RadioButtonAll.CheckedChanged += new System.EventHandler(this.RadioButtonShow_CheckedChanged);
            // 
            // RadioButtonCheck
            // 
            this.RadioButtonCheck.Location = new System.Drawing.Point(8, 49);
            this.RadioButtonCheck.Margin = new System.Windows.Forms.Padding(4);
            this.RadioButtonCheck.Name = "RadioButtonCheck";
            this.RadioButtonCheck.Size = new System.Drawing.Size(75, 21);
            this.RadioButtonCheck.TabIndex = 0;
            this.RadioButtonCheck.Text = "Check";
            this.RadioButtonCheck.UseVisualStyleBackColor = true;
            this.RadioButtonCheck.CheckedChanged += new System.EventHandler(this.RadioButtonShow_CheckedChanged);
            // 
            // RadioButtonUncheck
            // 
            this.RadioButtonUncheck.Location = new System.Drawing.Point(8, 78);
            this.RadioButtonUncheck.Margin = new System.Windows.Forms.Padding(4);
            this.RadioButtonUncheck.Name = "RadioButtonUncheck";
            this.RadioButtonUncheck.Size = new System.Drawing.Size(92, 21);
            this.RadioButtonUncheck.TabIndex = 0;
            this.RadioButtonUncheck.Text = "Uncheck";
            this.RadioButtonUncheck.UseVisualStyleBackColor = true;
            this.RadioButtonUncheck.CheckedChanged += new System.EventHandler(this.RadioButtonShow_CheckedChanged);
            // 
            // FlowLayoutPanelExpand
            // 
            this.FlowLayoutPanelExpand.AutoSize = true;
            this.FlowLayoutPanelExpand.Controls.Add(this.ButtonExpand);
            this.FlowLayoutPanelExpand.Controls.Add(this.ButtonCollapse);
            this.FlowLayoutPanelExpand.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FlowLayoutPanelExpand.Location = new System.Drawing.Point(0, 548);
            this.FlowLayoutPanelExpand.Margin = new System.Windows.Forms.Padding(0);
            this.FlowLayoutPanelExpand.Name = "FlowLayoutPanelExpand";
            this.FlowLayoutPanelExpand.Size = new System.Drawing.Size(103, 74);
            this.FlowLayoutPanelExpand.TabIndex = 0;
            // 
            // ButtonExpand
            // 
            this.ButtonExpand.Location = new System.Drawing.Point(4, 4);
            this.ButtonExpand.Margin = new System.Windows.Forms.Padding(4);
            this.ButtonExpand.Name = "ButtonExpand";
            this.ButtonExpand.Size = new System.Drawing.Size(95, 28);
            this.ButtonExpand.TabIndex = 0;
            this.ButtonExpand.Text = "Expand All";
            this.ButtonExpand.UseVisualStyleBackColor = true;
            this.ButtonExpand.Click += new System.EventHandler(this.ButtonExpand_Click);
            // 
            // ButtonCollapse
            // 
            this.ButtonCollapse.Location = new System.Drawing.Point(4, 40);
            this.ButtonCollapse.Margin = new System.Windows.Forms.Padding(4);
            this.ButtonCollapse.Name = "ButtonCollapse";
            this.ButtonCollapse.Size = new System.Drawing.Size(95, 28);
            this.ButtonCollapse.TabIndex = 0;
            this.ButtonCollapse.Text = "Collapse All";
            this.ButtonCollapse.UseVisualStyleBackColor = true;
            this.ButtonCollapse.Click += new System.EventHandler(this.ButtonCollapse_Click);
            // 
            // TreeCustomSettings
            // 
            this.TreeCustomSettings.CheckBoxes = true;
            this.TreeCustomSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TreeCustomSettings.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText;
            this.TreeCustomSettings.ImageIndex = 0;
            this.TreeCustomSettings.ImageList = this.IconList;
            this.TreeCustomSettings.Location = new System.Drawing.Point(115, 4);
            this.TreeCustomSettings.Margin = new System.Windows.Forms.Padding(4);
            this.TreeCustomSettings.Name = "TreeCustomSettings";
            this.TreeCustomSettings.SelectedImageIndex = 0;
            this.TreeCustomSettings.Size = new System.Drawing.Size(516, 622);
            this.TreeCustomSettings.TabIndex = 0;
            this.TreeCustomSettings.Scroll += new System.EventHandler(this.TreeCustomSettings_Scroll);
            this.TreeCustomSettings.BeforeCheck += new System.Windows.Forms.TreeViewCancelEventHandler(this.TreeCustomSettings_BeforeCheck);
            this.TreeCustomSettings.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.TreeCustomSettings_AfterCheck);
            this.TreeCustomSettings.BeforeCollapse += new System.Windows.Forms.TreeViewCancelEventHandler(this.SuspendDrawing);
            this.TreeCustomSettings.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.ResumeDrawing);
            this.TreeCustomSettings.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.SuspendDrawing);
            this.TreeCustomSettings.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.ResumeDrawing);
            this.TreeCustomSettings.DrawNode += new System.Windows.Forms.DrawTreeNodeEventHandler(this.TreeCustomSettings_DrawNode);
            this.TreeCustomSettings.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TreeCustomSettings_AfterSelect);
            this.TreeCustomSettings.MouseLeave += new System.EventHandler(this.TreeCustomSettings_MouseLeave);
            this.TreeCustomSettings.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TreeCustomSettings_MouseMove);
            // 
            // IconList
            // 
            this.IconList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.IconList.ImageSize = new System.Drawing.Size(20, 20);
            this.IconList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // TableLayoutTreeSettings
            // 
            this.TableLayoutTreeSettings.ColumnCount = 2;
            this.TableLayoutTreeSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 111F));
            this.TableLayoutTreeSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutTreeSettings.Controls.Add(this.TableLayoutPanelTree, 0, 0);
            this.TableLayoutTreeSettings.Controls.Add(this.TreeCustomSettings, 1, 0);
            this.TableLayoutTreeSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TableLayoutTreeSettings.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.TableLayoutTreeSettings.Location = new System.Drawing.Point(0, 0);
            this.TableLayoutTreeSettings.Margin = new System.Windows.Forms.Padding(0);
            this.TableLayoutTreeSettings.Name = "TableLayoutTreeSettings";
            this.TableLayoutTreeSettings.RowCount = 1;
            this.TableLayoutTreeSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutTreeSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 630F));
            this.TableLayoutTreeSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 630F));
            this.TableLayoutTreeSettings.Size = new System.Drawing.Size(635, 630);
            this.TableLayoutTreeSettings.TabIndex = 0;
            // 
            // TreeSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.TableLayoutTreeSettings);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "TreeSettings";
            this.Size = new System.Drawing.Size(635, 630);
            this.Load += new System.EventHandler(this.Settings_Load);
            this.TableLayoutPanelTree.ResumeLayout(false);
            this.TableLayoutPanelTree.PerformLayout();
            this.TableLayoutPanelSplit.ResumeLayout(false);
            this.TableLayoutPanelSplit.PerformLayout();
            this.GroupBoxImages.ResumeLayout(false);
            this.GroupBoxImages.PerformLayout();
            this.TableLayoutPanelImages.ResumeLayout(false);
            this.GroupBoxTip.ResumeLayout(false);
            this.GroupBoxSort.ResumeLayout(false);
            this.GroupBoxShow.ResumeLayout(false);
            this.FlowLayoutPanelExpand.ResumeLayout(false);
            this.TableLayoutTreeSettings.ResumeLayout(false);
            this.TableLayoutTreeSettings.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private RadioButton RadioButtonType;
        private RadioButton RadioButtonAlphabet;
        private RadioButton RadioButtonAll;
        private RadioButton RadioButtonCheck;
        private RadioButton RadioButtonUncheck;
        private FlowLayoutPanel FlowLayoutPanelExpand;
        private Button ButtonExpand;
        private Button ButtonCollapse;
        private CheckBox CheckBoxIcons;
        private TableLayoutPanel TableLayoutPanelTree;
        private ComboBox ComboBoxTip;
        private GroupBox GroupBoxSort;
        private GroupBox GroupBoxShow;
        private GroupBox GroupBoxImages;
        private GroupBox GroupBoxTip;
        private Label LabelSplits;
        private TableLayoutPanel TableLayoutPanelImages;
        private TableLayoutPanel TableLayoutPanelSplit;
        private Label LabelSplitCount;
        private NewTreeView TreeCustomSettings;
        private TableLayoutPanel TableLayoutTreeSettings;
        private ImageList IconList;
        private Button ButtonSplitGenerator;
    }
}