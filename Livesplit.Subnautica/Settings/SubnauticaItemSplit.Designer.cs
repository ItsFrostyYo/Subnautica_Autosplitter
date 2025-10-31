namespace Livesplit.Subnautica
{
    partial class SubnauticaItemSplit
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SubnauticaItemSplit));
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.cboItem = new System.Windows.Forms.ComboBox();
            this.ToolTips = new System.Windows.Forms.ToolTip(this.components);
            this.picHandle = new System.Windows.Forms.PictureBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.l_name = new System.Windows.Forms.Label();
            this.cbSplitOnce = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.picHandle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnEdit
            // 
            this.btnEdit.Location = new System.Drawing.Point(408, 16);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(26, 23);
            this.btnEdit.TabIndex = 12;
            this.btnEdit.Text = "✏";
            this.btnEdit.UseVisualStyleBackColor = true;
            // 
            // btnRemove
            // 
            this.btnRemove.Image = ((System.Drawing.Image)(resources.GetObject("btnRemove.Image")));
            this.btnRemove.Location = new System.Drawing.Point(376, 16);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(26, 23);
            this.btnRemove.TabIndex = 10;
            this.btnRemove.UseVisualStyleBackColor = true;
            // 
            // cboItem
            // 
            this.cboItem.FormattingEnabled = true;
            this.cboItem.Location = new System.Drawing.Point(29, 18);
            this.cboItem.Name = "cboItem";
            this.cboItem.Size = new System.Drawing.Size(246, 21);
            this.cboItem.TabIndex = 9;
            this.cboItem.SelectedIndexChanged += new System.EventHandler(this.cboName_SelectedIndexChanged);
            // 
            // ToolTips
            // 
            this.ToolTips.ShowAlways = true;
            // 
            // picHandle
            // 
            this.picHandle.Cursor = System.Windows.Forms.Cursors.SizeAll;
            this.picHandle.Image = ((System.Drawing.Image)(resources.GetObject("picHandle.Image")));
            this.picHandle.Location = new System.Drawing.Point(3, 12);
            this.picHandle.Name = "picHandle";
            this.picHandle.Size = new System.Drawing.Size(20, 20);
            this.picHandle.TabIndex = 15;
            this.picHandle.TabStop = false;
            this.picHandle.MouseDown += new System.Windows.Forms.MouseEventHandler(this.picHandle_MouseDown);
            this.picHandle.MouseMove += new System.Windows.Forms.MouseEventHandler(this.picHandle_MouseMove);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.InitialImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.InitialImage")));
            this.pictureBox1.Location = new System.Drawing.Point(440, 17);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(26, 23);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 16;
            this.pictureBox1.TabStop = false;
            // 
            // l_name
            // 
            this.l_name.AutoSize = true;
            this.l_name.Location = new System.Drawing.Point(26, 2);
            this.l_name.Name = "l_name";
            this.l_name.Size = new System.Drawing.Size(51, 13);
            this.l_name.TabIndex = 18;
            this.l_name.Text = "Inventory";
            // 
            // cbSplitOnce
            // 
            this.cbSplitOnce.AutoSize = true;
            this.cbSplitOnce.Checked = true;
            this.cbSplitOnce.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbSplitOnce.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbSplitOnce.Location = new System.Drawing.Point(283, 18);
            this.cbSplitOnce.Name = "cbSplitOnce";
            this.cbSplitOnce.Size = new System.Drawing.Size(85, 20);
            this.cbSplitOnce.TabIndex = 19;
            this.cbSplitOnce.Text = "Split once";
            this.cbSplitOnce.UseVisualStyleBackColor = true;
            this.cbSplitOnce.CheckedChanged += new System.EventHandler(this.cbSplitOnce_CheckedChanged);
            // 
            // SubnauticaItemSplit
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.cbSplitOnce);
            this.Controls.Add(this.l_name);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.picHandle);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.cboItem);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "SubnauticaItemSplit";
            this.Size = new System.Drawing.Size(469, 47);
            ((System.ComponentModel.ISupportInitialize)(this.picHandle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        public System.Windows.Forms.Button btnEdit;
        public System.Windows.Forms.Button btnRemove;
        public System.Windows.Forms.ComboBox cboItem;
        private System.Windows.Forms.ToolTip ToolTips;
        private System.Windows.Forms.PictureBox picHandle;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label l_name;
        private System.Windows.Forms.CheckBox cbSplitOnce;
    }
}
