namespace Livesplit.Subnautica
{
    partial class SubnauticaPrefabSplit
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SubnauticaPrefabSplit));
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.cboName = new System.Windows.Forms.ComboBox();
            this.ToolTips = new System.Windows.Forms.ToolTip(this.components);
            this.picHandle = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.picHandle)).BeginInit();
            this.SuspendLayout();
            // 
            // btnEdit
            // 
            this.btnEdit.Location = new System.Drawing.Point(313, 2);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(26, 23);
            this.btnEdit.TabIndex = 12;
            this.btnEdit.Text = "✏";
            this.btnEdit.UseVisualStyleBackColor = true;
            // 
            // btnRemove
            // 
            this.btnRemove.Image = ((System.Drawing.Image)(resources.GetObject("btnRemove.Image")));
            this.btnRemove.Location = new System.Drawing.Point(281, 2);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(26, 23);
            this.btnRemove.TabIndex = 10;
            this.btnRemove.UseVisualStyleBackColor = true;
            // 
            // cboName
            // 
            this.cboName.FormattingEnabled = true;
            this.cboName.Location = new System.Drawing.Point(29, 3);
            this.cboName.Name = "cboName";
            this.cboName.Size = new System.Drawing.Size(246, 21);
            this.cboName.TabIndex = 9;
            this.cboName.SelectedIndexChanged += new System.EventHandler(this.cboName_SelectedIndexChanged);
            // 
            // ToolTips
            // 
            this.ToolTips.ShowAlways = true;
            // 
            // picHandle
            // 
            this.picHandle.Cursor = System.Windows.Forms.Cursors.SizeAll;
            this.picHandle.Image = ((System.Drawing.Image)(resources.GetObject("picHandle.Image")));
            this.picHandle.Location = new System.Drawing.Point(3, 4);
            this.picHandle.Name = "picHandle";
            this.picHandle.Size = new System.Drawing.Size(20, 20);
            this.picHandle.TabIndex = 15;
            this.picHandle.TabStop = false;
            this.picHandle.MouseDown += new System.Windows.Forms.MouseEventHandler(this.picHandle_MouseDown);
            this.picHandle.MouseMove += new System.Windows.Forms.MouseEventHandler(this.picHandle_MouseMove);
            // 
            // SubnauticaSplitSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.picHandle);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.cboName);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "SubnauticaSplitSettings";
            this.Size = new System.Drawing.Size(424, 28);
            ((System.ComponentModel.ISupportInitialize)(this.picHandle)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        public System.Windows.Forms.Button btnEdit;
        public System.Windows.Forms.Button btnRemove;
        public System.Windows.Forms.ComboBox cboName;
        private System.Windows.Forms.ToolTip ToolTips;
        private System.Windows.Forms.PictureBox picHandle;
    }
}
