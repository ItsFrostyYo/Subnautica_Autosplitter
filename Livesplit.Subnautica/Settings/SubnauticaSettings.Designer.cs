namespace Livesplit.Subnautica
{
    partial class SubnauticaSettings
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.cb_StartIntro = new System.Windows.Forms.CheckBox();
            this.cb_Rocket = new System.Windows.Forms.CheckBox();
            this.flowMain = new System.Windows.Forms.FlowLayoutPanel();
            this.flowOptions = new System.Windows.Forms.FlowLayoutPanel();
            this.Options_GroupBox = new System.Windows.Forms.GroupBox();
            this.ButtonSplitGenerator = new System.Windows.Forms.Button();
            this.btnAddExplo = new System.Windows.Forms.Button();
            this.Other_GroupBox = new System.Windows.Forms.GroupBox();
            this.cbOrdered = new System.Windows.Forms.CheckBox();
            this.chkSRCLoadtimes = new System.Windows.Forms.CheckBox();
            this.chkAskForGoldSave = new System.Windows.Forms.CheckBox();
            this.btnAddSplit = new System.Windows.Forms.Button();
            this.StartReset_GroupBox = new System.Windows.Forms.GroupBox();
            this.chkReset = new System.Windows.Forms.CheckBox();
            this.chkCreativeStart = new System.Windows.Forms.CheckBox();
            this.chkIntroStart = new System.Windows.Forms.CheckBox();
            this.SortBy_GroupBox = new System.Windows.Forms.GroupBox();
            this.rdAlpha = new System.Windows.Forms.RadioButton();
            this.rdType = new System.Windows.Forms.RadioButton();
            this.ToolTips = new System.Windows.Forms.ToolTip(this.components);
            this.flowMain.SuspendLayout();
            this.flowOptions.SuspendLayout();
            this.Options_GroupBox.SuspendLayout();
            this.Other_GroupBox.SuspendLayout();
            this.StartReset_GroupBox.SuspendLayout();
            this.SortBy_GroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // cb_StartIntro
            // 
            this.cb_StartIntro.AutoSize = true;
            this.cb_StartIntro.Location = new System.Drawing.Point(3, 3);
            this.cb_StartIntro.Name = "cb_StartIntro";
            this.cb_StartIntro.Size = new System.Drawing.Size(113, 20);
            this.cb_StartIntro.TabIndex = 0;
            this.cb_StartIntro.Text = "Start after intro";
            this.cb_StartIntro.UseVisualStyleBackColor = true;
            // 
            // cb_Rocket
            // 
            this.cb_Rocket.AutoSize = true;
            this.cb_Rocket.Location = new System.Drawing.Point(3, 29);
            this.cb_Rocket.Name = "cb_Rocket";
            this.cb_Rocket.Size = new System.Drawing.Size(161, 20);
            this.cb_Rocket.TabIndex = 1;
            this.cb_Rocket.Text = "Split on Rocket launch";
            this.cb_Rocket.UseVisualStyleBackColor = true;
            // 
            // flowMain
            // 
            this.flowMain.AllowDrop = true;
            this.flowMain.AutoSize = true;
            this.flowMain.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowMain.Controls.Add(this.flowOptions);
            this.flowMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowMain.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowMain.Location = new System.Drawing.Point(0, 0);
            this.flowMain.Margin = new System.Windows.Forms.Padding(0);
            this.flowMain.Name = "flowMain";
            this.flowMain.Size = new System.Drawing.Size(472, 141);
            this.flowMain.TabIndex = 0;
            this.flowMain.WrapContents = false;
            this.flowMain.DragDrop += new System.Windows.Forms.DragEventHandler(this.flowMain_DragDrop);
            this.flowMain.DragEnter += new System.Windows.Forms.DragEventHandler(this.flowMain_DragEnter);
            this.flowMain.DragOver += new System.Windows.Forms.DragEventHandler(this.flowMain_DragOver);
            // 
            // flowOptions
            // 
            this.flowOptions.AutoSize = true;
            this.flowOptions.Controls.Add(this.Options_GroupBox);
            this.flowOptions.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowOptions.Location = new System.Drawing.Point(0, 0);
            this.flowOptions.Margin = new System.Windows.Forms.Padding(0);
            this.flowOptions.Name = "flowOptions";
            this.flowOptions.Size = new System.Drawing.Size(472, 141);
            this.flowOptions.TabIndex = 2;
            // 
            // Options_GroupBox
            // 
            this.Options_GroupBox.Controls.Add(this.ButtonSplitGenerator);
            this.Options_GroupBox.Controls.Add(this.btnAddExplo);
            this.Options_GroupBox.Controls.Add(this.Other_GroupBox);
            this.Options_GroupBox.Controls.Add(this.btnAddSplit);
            this.Options_GroupBox.Controls.Add(this.StartReset_GroupBox);
            this.Options_GroupBox.Controls.Add(this.SortBy_GroupBox);
            this.Options_GroupBox.Location = new System.Drawing.Point(3, 3);
            this.Options_GroupBox.Name = "Options_GroupBox";
            this.Options_GroupBox.Size = new System.Drawing.Size(466, 135);
            this.Options_GroupBox.TabIndex = 6;
            this.Options_GroupBox.TabStop = false;
            this.Options_GroupBox.Text = "Options";
            this.ToolTips.SetToolTip(this.Options_GroupBox, "This will add time to the actual load times to match the IGT shown on Speedrun.co" +
        "m (can be up to 0.1s inaccurate)\\nUnchecking this will not turn off the load rem" +
        "oval\")");
            // 
            // ButtonSplitGenerator
            // 
            this.ButtonSplitGenerator.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ButtonSplitGenerator.Location = new System.Drawing.Point(128, 102);
            this.ButtonSplitGenerator.Name = "ButtonSplitGenerator";
            this.ButtonSplitGenerator.Size = new System.Drawing.Size(162, 23);
            this.ButtonSplitGenerator.TabIndex = 10;
            this.ButtonSplitGenerator.Text = "Generate Splits";
            this.ButtonSplitGenerator.UseVisualStyleBackColor = true;
            this.ButtonSplitGenerator.Click += new System.EventHandler(this.ButtonSplitGenerator_Click);
            // 
            // btnAddExplo
            // 
            this.btnAddExplo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddExplo.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAddExplo.Location = new System.Drawing.Point(296, 102);
            this.btnAddExplo.Margin = new System.Windows.Forms.Padding(2);
            this.btnAddExplo.Name = "btnAddExplo";
            this.btnAddExplo.Size = new System.Drawing.Size(164, 23);
            this.btnAddExplo.TabIndex = 9;
            this.btnAddExplo.Text = "Add Explosion Time";
            this.ToolTips.SetToolTip(this.btnAddExplo, "This will add a text component to your layout which shows when the aurora is goin" +
        "g to explode");
            this.btnAddExplo.UseVisualStyleBackColor = true;
            this.btnAddExplo.Click += new System.EventHandler(this.btnAddExplo_Click);
            // 
            // Other_GroupBox
            // 
            this.Other_GroupBox.Controls.Add(this.cbOrdered);
            this.Other_GroupBox.Controls.Add(this.chkSRCLoadtimes);
            this.Other_GroupBox.Controls.Add(this.chkAskForGoldSave);
            this.Other_GroupBox.Location = new System.Drawing.Point(296, 15);
            this.Other_GroupBox.Name = "Other_GroupBox";
            this.Other_GroupBox.Size = new System.Drawing.Size(164, 81);
            this.Other_GroupBox.TabIndex = 8;
            this.Other_GroupBox.TabStop = false;
            this.Other_GroupBox.Text = "Others";
            // 
            // cbOrdered
            // 
            this.cbOrdered.AutoSize = true;
            this.cbOrdered.Location = new System.Drawing.Point(5, 60);
            this.cbOrdered.Name = "cbOrdered";
            this.cbOrdered.Size = new System.Drawing.Size(92, 17);
            this.cbOrdered.TabIndex = 2;
            this.cbOrdered.Text = "Ordered Splits";
            this.cbOrdered.UseVisualStyleBackColor = true;
            this.cbOrdered.CheckedChanged += new System.EventHandler(this.ControlChanged);
            // 
            // chkSRCLoadtimes
            // 
            this.chkSRCLoadtimes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.chkSRCLoadtimes.AutoSize = true;
            this.chkSRCLoadtimes.Checked = true;
            this.chkSRCLoadtimes.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSRCLoadtimes.Location = new System.Drawing.Point(5, 39);
            this.chkSRCLoadtimes.Margin = new System.Windows.Forms.Padding(2);
            this.chkSRCLoadtimes.Name = "chkSRCLoadtimes";
            this.chkSRCLoadtimes.Size = new System.Drawing.Size(99, 17);
            this.chkSRCLoadtimes.TabIndex = 1;
            this.chkSRCLoadtimes.Text = "SRC Loadtimes";
            this.ToolTips.SetToolTip(this.chkSRCLoadtimes, "This will add time to the actual load times to match the IGT shown on Speedrun.co" +
        "m (can be up to 0.1s inaccurate)\r\nUnchecking this will not turn off the load rem" +
        "oval");
            this.chkSRCLoadtimes.UseVisualStyleBackColor = true;
            this.chkSRCLoadtimes.CheckedChanged += new System.EventHandler(this.ControlChanged);
            // 
            // chkAskForGoldSave
            // 
            this.chkAskForGoldSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.chkAskForGoldSave.AutoSize = true;
            this.chkAskForGoldSave.Location = new System.Drawing.Point(5, 18);
            this.chkAskForGoldSave.Margin = new System.Windows.Forms.Padding(2);
            this.chkAskForGoldSave.Name = "chkAskForGoldSave";
            this.chkAskForGoldSave.Size = new System.Drawing.Size(157, 17);
            this.chkAskForGoldSave.TabIndex = 0;
            this.chkAskForGoldSave.Text = "Warn On Reset If Gold Split";
            this.ToolTips.SetToolTip(this.chkAskForGoldSave, "Ask to save golds when the timer auto resets");
            this.chkAskForGoldSave.UseVisualStyleBackColor = true;
            this.chkAskForGoldSave.CheckedChanged += new System.EventHandler(this.ControlChanged);
            // 
            // btnAddSplit
            // 
            this.btnAddSplit.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAddSplit.Location = new System.Drawing.Point(6, 102);
            this.btnAddSplit.Name = "btnAddSplit";
            this.btnAddSplit.Size = new System.Drawing.Size(116, 23);
            this.btnAddSplit.TabIndex = 0;
            this.btnAddSplit.Text = "Add Split";
            this.btnAddSplit.UseVisualStyleBackColor = true;
            this.btnAddSplit.Click += new System.EventHandler(this.btnAddSplit_Click);
            // 
            // StartReset_GroupBox
            // 
            this.StartReset_GroupBox.Controls.Add(this.chkReset);
            this.StartReset_GroupBox.Controls.Add(this.chkCreativeStart);
            this.StartReset_GroupBox.Controls.Add(this.chkIntroStart);
            this.StartReset_GroupBox.Location = new System.Drawing.Point(128, 15);
            this.StartReset_GroupBox.Name = "StartReset_GroupBox";
            this.StartReset_GroupBox.Size = new System.Drawing.Size(162, 81);
            this.StartReset_GroupBox.TabIndex = 7;
            this.StartReset_GroupBox.TabStop = false;
            this.StartReset_GroupBox.Text = "Start / Reset";
            // 
            // chkReset
            // 
            this.chkReset.AutoSize = true;
            this.chkReset.Location = new System.Drawing.Point(106, 23);
            this.chkReset.Margin = new System.Windows.Forms.Padding(2);
            this.chkReset.Name = "chkReset";
            this.chkReset.Size = new System.Drawing.Size(54, 17);
            this.chkReset.TabIndex = 2;
            this.chkReset.Text = "Reset";
            this.ToolTips.SetToolTip(this.chkReset, "Resets the timer when you come back to the main menu");
            this.chkReset.UseVisualStyleBackColor = true;
            this.chkReset.CheckedChanged += new System.EventHandler(this.ControlChanged);
            // 
            // chkCreativeStart
            // 
            this.chkCreativeStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.chkCreativeStart.AutoSize = true;
            this.chkCreativeStart.Checked = true;
            this.chkCreativeStart.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCreativeStart.Location = new System.Drawing.Point(5, 47);
            this.chkCreativeStart.Margin = new System.Windows.Forms.Padding(2);
            this.chkCreativeStart.Name = "chkCreativeStart";
            this.chkCreativeStart.Size = new System.Drawing.Size(90, 17);
            this.chkCreativeStart.TabIndex = 1;
            this.chkCreativeStart.Text = "Creative Start";
            this.ToolTips.SetToolTip(this.chkCreativeStart, "Starts the timer when you move horizontally, open your PDA or interact with the f" +
        "abricator");
            this.chkCreativeStart.UseVisualStyleBackColor = true;
            this.chkCreativeStart.CheckedChanged += new System.EventHandler(this.ControlChanged);
            // 
            // chkIntroStart
            // 
            this.chkIntroStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.chkIntroStart.AutoSize = true;
            this.chkIntroStart.Checked = true;
            this.chkIntroStart.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIntroStart.Location = new System.Drawing.Point(5, 23);
            this.chkIntroStart.Margin = new System.Windows.Forms.Padding(2);
            this.chkIntroStart.Name = "chkIntroStart";
            this.chkIntroStart.Size = new System.Drawing.Size(96, 17);
            this.chkIntroStart.TabIndex = 0;
            this.chkIntroStart.Text = "Start after Intro";
            this.ToolTips.SetToolTip(this.chkIntroStart, "Starts the timer after the intro animation");
            this.chkIntroStart.UseVisualStyleBackColor = true;
            this.chkIntroStart.CheckedChanged += new System.EventHandler(this.ControlChanged);
            // 
            // SortBy_GroupBox
            // 
            this.SortBy_GroupBox.Controls.Add(this.rdAlpha);
            this.SortBy_GroupBox.Controls.Add(this.rdType);
            this.SortBy_GroupBox.Location = new System.Drawing.Point(6, 15);
            this.SortBy_GroupBox.Name = "SortBy_GroupBox";
            this.SortBy_GroupBox.Size = new System.Drawing.Size(116, 81);
            this.SortBy_GroupBox.TabIndex = 6;
            this.SortBy_GroupBox.TabStop = false;
            this.SortBy_GroupBox.Text = "Sort Split Selects By";
            // 
            // rdAlpha
            // 
            this.rdAlpha.AutoSize = true;
            this.rdAlpha.Checked = true;
            this.rdAlpha.Location = new System.Drawing.Point(6, 42);
            this.rdAlpha.Name = "rdAlpha";
            this.rdAlpha.Size = new System.Drawing.Size(83, 17);
            this.rdAlpha.TabIndex = 3;
            this.rdAlpha.TabStop = true;
            this.rdAlpha.Text = "Alphabetical";
            this.rdAlpha.UseVisualStyleBackColor = true;
            this.rdAlpha.CheckedChanged += new System.EventHandler(this.radio_CheckedChanged);
            // 
            // rdType
            // 
            this.rdType.AutoSize = true;
            this.rdType.Location = new System.Drawing.Point(6, 19);
            this.rdType.Name = "rdType";
            this.rdType.Size = new System.Drawing.Size(49, 17);
            this.rdType.TabIndex = 2;
            this.rdType.Text = "Type";
            this.rdType.UseVisualStyleBackColor = true;
            this.rdType.CheckedChanged += new System.EventHandler(this.radio_CheckedChanged);
            // 
            // ToolTips
            // 
            this.ToolTips.AutoPopDelay = 9000;
            this.ToolTips.InitialDelay = 300;
            this.ToolTips.ReshowDelay = 180;
            this.ToolTips.ShowAlways = true;
            // 
            // SubnauticaSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.flowMain);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "SubnauticaSettings";
            this.Size = new System.Drawing.Size(472, 141);
            this.Load += new System.EventHandler(this.Settings_Load);
            this.flowMain.ResumeLayout(false);
            this.flowMain.PerformLayout();
            this.flowOptions.ResumeLayout(false);
            this.Options_GroupBox.ResumeLayout(false);
            this.Other_GroupBox.ResumeLayout(false);
            this.Other_GroupBox.PerformLayout();
            this.StartReset_GroupBox.ResumeLayout(false);
            this.StartReset_GroupBox.PerformLayout();
            this.SortBy_GroupBox.ResumeLayout(false);
            this.SortBy_GroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox cb_StartIntro;
        private System.Windows.Forms.CheckBox cb_Rocket;
        private System.Windows.Forms.FlowLayoutPanel flowMain;
        private System.Windows.Forms.FlowLayoutPanel flowOptions;
        private System.Windows.Forms.GroupBox Options_GroupBox;
        private System.Windows.Forms.Button btnAddSplit;
        private System.Windows.Forms.GroupBox StartReset_GroupBox;
        private System.Windows.Forms.GroupBox SortBy_GroupBox;
        private System.Windows.Forms.RadioButton rdAlpha;
        private System.Windows.Forms.RadioButton rdType;
        private System.Windows.Forms.CheckBox chkCreativeStart;
        private System.Windows.Forms.CheckBox chkIntroStart;
        private System.Windows.Forms.ToolTip ToolTips;
        private System.Windows.Forms.GroupBox Other_GroupBox;
        private System.Windows.Forms.CheckBox chkSRCLoadtimes;
        private System.Windows.Forms.CheckBox chkAskForGoldSave;
        private System.Windows.Forms.CheckBox chkReset;
        private System.Windows.Forms.Button btnAddExplo;
        private System.Windows.Forms.Button ButtonSplitGenerator;
        private System.Windows.Forms.CheckBox cbOrdered;
    }
}
