using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace Livesplit.Subnautica
{
    public partial class SubnauticaEncySplit : SubnauticaSplitSetting
    {
        public EncySplit _split = new EncySplit(EncyEntry.None, true);

        private int mX = 0;
        private int mY = 0;
        private bool isDragging = false;

        public SubnauticaEncySplit()
        {
            InitializeComponent();
            cboEncy.DropDownStyle = ComboBoxStyle.DropDownList;
            cboEncy.MouseWheel += (o, e) => ((HandledMouseEventArgs)e).Handled = true;
            cboEncy.DisplayMember = "Display";
            cboEncy.ValueMember = "Value";
        }

        private void cboName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboEncy.SelectedValue is EncyEntry entry)
                _split.Entry = entry;
        }

        private void cbSplitOnce_CheckedChanged(object sender, EventArgs e)
        {
            _split.OnlySplitOnce = cbSplitOnce.Checked;
        }

        private void picHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging)
            {
                if (e.Button == MouseButtons.Left)
                {
                    int num1 = mX - e.X;
                    int num2 = mY - e.Y;
                    if (((num1 * num1) + (num2 * num2)) > 20)
                    {
                        DoDragDrop(this, DragDropEffects.All);
                        isDragging = true;
                        return;
                    }
                }
            }
        }

        private void picHandle_MouseDown(object sender, MouseEventArgs e)
        {
            mX = e.X;
            mY = e.Y;
            isDragging = false;
        }

        public override ComboBox ComboBox => this.cboEncy;
        public override CheckBox CbSplitOnce => this.cbSplitOnce;
        public override Button BtnEdit => this.btnEdit;
        public override Button BtnRemove => this.btnRemove;
        public override SplitName SplitName => SplitName.Encyclopedia;
        public override SubnauticaSplit Split => this._split;
    }

    public class EncySplit : SubnauticaSplit
    {
        public EncyEntry Entry { get; set; }

        public EncySplit(EncyEntry entry, bool onlySplitOnce)
        {
            Entry = entry;
            this.OnlySplitOnce = onlySplitOnce;
            this.SplitName = SplitName.Encyclopedia;
        }
        public override string GetDescription() => $"{Localization.GetDisplayName(Entry)} in Encyclopedia Split";
    }
}
