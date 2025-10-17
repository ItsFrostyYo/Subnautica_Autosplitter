using System.Windows.Forms;

namespace LiveSplit.VoxSplitter {
    abstract partial class Settings {
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
            this.TableLayoutSettings = new System.Windows.Forms.TableLayoutPanel();
            this.LabelSettings = new System.Windows.Forms.Label();
            this.TableLayoutPanelSettings = new System.Windows.Forms.TableLayoutPanel();
            this.TableLayoutPanelStart = new System.Windows.Forms.TableLayoutPanel();
            this.LabelStart = new System.Windows.Forms.Label();
            this.TableLayoutPanelReset = new System.Windows.Forms.TableLayoutPanel();
            this.LabelReset = new System.Windows.Forms.Label();
            this.TableLayoutPanelOptions = new System.Windows.Forms.TableLayoutPanel();
            this.LabelPreset = new System.Windows.Forms.Label();
            this.TableLayoutPanelCheck = new System.Windows.Forms.TableLayoutPanel();
            this.ComboBoxPreset = new System.Windows.Forms.ComboBox();
            this.ButtonUncheckAll = new System.Windows.Forms.Button();
            this.ButtonCheckAll = new System.Windows.Forms.Button();
            this.TableLayoutSettings.SuspendLayout();
            this.TableLayoutPanelSettings.SuspendLayout();
            this.TableLayoutPanelStart.SuspendLayout();
            this.TableLayoutPanelReset.SuspendLayout();
            this.TableLayoutPanelCheck.SuspendLayout();
            this.SuspendLayout();
            // 
            // TableLayoutSettings
            // 
            this.TableLayoutSettings.AutoSize = true;
            this.TableLayoutSettings.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.TableLayoutSettings.ColumnCount = 2;
            this.TableLayoutSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 111F));
            this.TableLayoutSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutSettings.Controls.Add(this.LabelSettings, 0, 0);
            this.TableLayoutSettings.Controls.Add(this.TableLayoutPanelSettings, 1, 0);
            this.TableLayoutSettings.Controls.Add(this.LabelPreset, 0, 1);
            this.TableLayoutSettings.Controls.Add(this.TableLayoutPanelCheck, 1, 1);
            this.TableLayoutSettings.Dock = System.Windows.Forms.DockStyle.Top;
            this.TableLayoutSettings.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.TableLayoutSettings.Location = new System.Drawing.Point(0, 0);
            this.TableLayoutSettings.Margin = new System.Windows.Forms.Padding(0);
            this.TableLayoutSettings.Name = "TableLayoutSettings";
            this.TableLayoutSettings.RowCount = 2;
            this.TableLayoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.TableLayoutSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.TableLayoutSettings.Size = new System.Drawing.Size(683, 94);
            this.TableLayoutSettings.TabIndex = 0;
            // 
            // LabelSettings
            // 
            this.LabelSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.LabelSettings.AutoSize = true;
            this.LabelSettings.Location = new System.Drawing.Point(4, 12);
            this.LabelSettings.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.LabelSettings.Name = "LabelSettings";
            this.LabelSettings.Size = new System.Drawing.Size(103, 17);
            this.LabelSettings.TabIndex = 1;
            this.LabelSettings.Text = "Settings:";
            this.LabelSettings.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TableLayoutPanelSettings
            // 
            this.TableLayoutPanelSettings.AutoSize = true;
            this.TableLayoutPanelSettings.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.TableLayoutPanelSettings.ColumnCount = 3;
            this.TableLayoutPanelSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.TableLayoutPanelSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.TableLayoutPanelSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutPanelSettings.Controls.Add(this.TableLayoutPanelStart, 0, 0);
            this.TableLayoutPanelSettings.Controls.Add(this.TableLayoutPanelReset, 1, 0);
            this.TableLayoutPanelSettings.Controls.Add(this.TableLayoutPanelOptions, 2, 0);
            this.TableLayoutPanelSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TableLayoutPanelSettings.Location = new System.Drawing.Point(118, 4);
            this.TableLayoutPanelSettings.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.TableLayoutPanelSettings.Name = "TableLayoutPanelSettings";
            this.TableLayoutPanelSettings.RowCount = 1;
            this.TableLayoutPanelSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutPanelSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.TableLayoutPanelSettings.Size = new System.Drawing.Size(558, 34);
            this.TableLayoutPanelSettings.TabIndex = 0;
            // 
            // TableLayoutPanelStart
            // 
            this.TableLayoutPanelStart.AutoSize = true;
            this.TableLayoutPanelStart.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.TableLayoutPanelStart.ColumnCount = 2;
            this.TableLayoutPanelStart.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.TableLayoutPanelStart.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.TableLayoutPanelStart.Controls.Add(this.LabelStart, 0, 0);
            this.TableLayoutPanelStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TableLayoutPanelStart.Location = new System.Drawing.Point(7, 0);
            this.TableLayoutPanelStart.Margin = new System.Windows.Forms.Padding(7, 0, 7, 0);
            this.TableLayoutPanelStart.Name = "TableLayoutPanelStart";
            this.TableLayoutPanelStart.RowCount = 1;
            this.TableLayoutPanelStart.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutPanelStart.Size = new System.Drawing.Size(46, 34);
            this.TableLayoutPanelStart.TabIndex = 0;
            // 
            // LabelStart
            // 
            this.LabelStart.AutoSize = true;
            this.LabelStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LabelStart.Location = new System.Drawing.Point(4, 0);
            this.LabelStart.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.LabelStart.Name = "LabelStart";
            this.LabelStart.Size = new System.Drawing.Size(38, 34);
            this.LabelStart.TabIndex = 0;
            this.LabelStart.Text = "Start";
            this.LabelStart.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // TableLayoutPanelReset
            // 
            this.TableLayoutPanelReset.AutoSize = true;
            this.TableLayoutPanelReset.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.TableLayoutPanelReset.ColumnCount = 2;
            this.TableLayoutPanelReset.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.TableLayoutPanelReset.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.TableLayoutPanelReset.Controls.Add(this.LabelReset, 0, 0);
            this.TableLayoutPanelReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TableLayoutPanelReset.Location = new System.Drawing.Point(67, 0);
            this.TableLayoutPanelReset.Margin = new System.Windows.Forms.Padding(7, 0, 13, 0);
            this.TableLayoutPanelReset.Name = "TableLayoutPanelReset";
            this.TableLayoutPanelReset.RowCount = 1;
            this.TableLayoutPanelReset.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutPanelReset.Size = new System.Drawing.Size(53, 34);
            this.TableLayoutPanelReset.TabIndex = 0;
            // 
            // LabelReset
            // 
            this.LabelReset.AutoSize = true;
            this.LabelReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LabelReset.Location = new System.Drawing.Point(4, 0);
            this.LabelReset.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.LabelReset.Name = "LabelReset";
            this.LabelReset.Size = new System.Drawing.Size(45, 34);
            this.LabelReset.TabIndex = 0;
            this.LabelReset.Text = "Reset";
            this.LabelReset.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // TableLayoutPanelOptions
            // 
            this.TableLayoutPanelOptions.AutoSize = true;
            this.TableLayoutPanelOptions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.TableLayoutPanelOptions.ColumnCount = 2;
            this.TableLayoutPanelOptions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.TableLayoutPanelOptions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutPanelOptions.Dock = System.Windows.Forms.DockStyle.Right;
            this.TableLayoutPanelOptions.Location = new System.Drawing.Point(558, 0);
            this.TableLayoutPanelOptions.Margin = new System.Windows.Forms.Padding(0);
            this.TableLayoutPanelOptions.Name = "TableLayoutPanelOptions";
            this.TableLayoutPanelOptions.RowCount = 1;
            this.TableLayoutPanelOptions.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutPanelOptions.Size = new System.Drawing.Size(0, 34);
            this.TableLayoutPanelOptions.TabIndex = 0;
            // 
            // LabelPreset
            // 
            this.LabelPreset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.LabelPreset.AutoSize = true;
            this.LabelPreset.Location = new System.Drawing.Point(4, 59);
            this.LabelPreset.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.LabelPreset.Name = "LabelPreset";
            this.LabelPreset.Size = new System.Drawing.Size(103, 17);
            this.LabelPreset.TabIndex = 0;
            this.LabelPreset.Text = "Presets:";
            this.LabelPreset.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TableLayoutPanelCheck
            // 
            this.TableLayoutPanelCheck.AutoSize = true;
            this.TableLayoutPanelCheck.ColumnCount = 3;
            this.TableLayoutPanelCheck.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutPanelCheck.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 112F));
            this.TableLayoutPanelCheck.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 95F));
            this.TableLayoutPanelCheck.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 27F));
            this.TableLayoutPanelCheck.Controls.Add(this.ComboBoxPreset, 0, 0);
            this.TableLayoutPanelCheck.Controls.Add(this.ButtonUncheckAll, 1, 0);
            this.TableLayoutPanelCheck.Controls.Add(this.ButtonCheckAll, 2, 0);
            this.TableLayoutPanelCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TableLayoutPanelCheck.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.TableLayoutPanelCheck.Location = new System.Drawing.Point(118, 48);
            this.TableLayoutPanelCheck.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.TableLayoutPanelCheck.Name = "TableLayoutPanelCheck";
            this.TableLayoutPanelCheck.RowCount = 1;
            this.TableLayoutPanelCheck.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutPanelCheck.Size = new System.Drawing.Size(558, 40);
            this.TableLayoutPanelCheck.TabIndex = 0;
            // 
            // ComboBoxPreset
            // 
            this.ComboBoxPreset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ComboBoxPreset.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBoxPreset.FormattingEnabled = true;
            this.ComboBoxPreset.Location = new System.Drawing.Point(7, 7);
            this.ComboBoxPreset.Margin = new System.Windows.Forms.Padding(7, 7, 7, 6);
            this.ComboBoxPreset.Name = "ComboBoxPreset";
            this.ComboBoxPreset.Size = new System.Drawing.Size(337, 24);
            this.ComboBoxPreset.TabIndex = 0;
            this.ComboBoxPreset.DropDown += new System.EventHandler(this.ComboBoxPreset_DropDown);
            this.ComboBoxPreset.SelectedIndexChanged += new System.EventHandler(this.ComboBoxPreset_SelectedIndexChanged);
            this.ComboBoxPreset.DropDownClosed += new System.EventHandler(this.ComboBoxPreset_DropDownClosed);
            // 
            // ButtonUncheckAll
            // 
            this.ButtonUncheckAll.AutoSize = true;
            this.ButtonUncheckAll.Location = new System.Drawing.Point(358, 6);
            this.ButtonUncheckAll.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.ButtonUncheckAll.Name = "ButtonUncheckAll";
            this.ButtonUncheckAll.Size = new System.Drawing.Size(98, 27);
            this.ButtonUncheckAll.TabIndex = 0;
            this.ButtonUncheckAll.Text = "Uncheck All";
            this.ButtonUncheckAll.UseVisualStyleBackColor = true;
            this.ButtonUncheckAll.Click += new System.EventHandler(this.ButtonUncheckAll_Click);
            // 
            // ButtonCheckAll
            // 
            this.ButtonCheckAll.AutoSize = true;
            this.ButtonCheckAll.Location = new System.Drawing.Point(470, 6);
            this.ButtonCheckAll.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.ButtonCheckAll.Name = "ButtonCheckAll";
            this.ButtonCheckAll.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.ButtonCheckAll.Size = new System.Drawing.Size(81, 27);
            this.ButtonCheckAll.TabIndex = 0;
            this.ButtonCheckAll.Text = "Check All";
            this.ButtonCheckAll.UseVisualStyleBackColor = true;
            this.ButtonCheckAll.Click += new System.EventHandler(this.ButtonCheckAll_Click);
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.TableLayoutSettings);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Settings";
            this.Size = new System.Drawing.Size(683, 94);
            this.Load += new System.EventHandler(this.Settings_Load);
            this.TableLayoutSettings.ResumeLayout(false);
            this.TableLayoutSettings.PerformLayout();
            this.TableLayoutPanelSettings.ResumeLayout(false);
            this.TableLayoutPanelSettings.PerformLayout();
            this.TableLayoutPanelStart.ResumeLayout(false);
            this.TableLayoutPanelStart.PerformLayout();
            this.TableLayoutPanelReset.ResumeLayout(false);
            this.TableLayoutPanelReset.PerformLayout();
            this.TableLayoutPanelCheck.ResumeLayout(false);
            this.TableLayoutPanelCheck.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        protected TableLayoutPanel TableLayoutSettings;
        protected TableLayoutPanel TableLayoutPanelSettings;
        protected TableLayoutPanel TableLayoutPanelStart;
        protected Label LabelStart;
        protected TableLayoutPanel TableLayoutPanelReset;
        protected Label LabelReset;
        protected TableLayoutPanel TableLayoutPanelOptions;
        protected Label LabelPreset;
        protected TableLayoutPanel TableLayoutPanelCheck;
        protected ComboBox ComboBoxPreset;
        protected Button ButtonUncheckAll;
        protected Button ButtonCheckAll;
        protected Label LabelSettings;
    }
}