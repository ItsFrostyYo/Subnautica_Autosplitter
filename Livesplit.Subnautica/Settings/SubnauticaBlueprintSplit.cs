using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Livesplit.Subnautica
{
    public partial class SubnauticaBlueprintSplit : SubnauticaSplitSetting
    {
        public BlueprintSplit _split = new BlueprintSplit(Unlockable.None, true);

        private int mX = 0;
        private int mY = 0;
        private bool isDragging = false;

        public SubnauticaBlueprintSplit()
        {
            InitializeComponent();
            cboBlueprint.DropDownStyle = ComboBoxStyle.DropDownList;
            cboBlueprint.MouseWheel += (o, e) => ((HandledMouseEventArgs)e).Handled = true;
            cboBlueprint.DisplayMember = "Display";
            cboBlueprint.ValueMember = "Value";
        }

        private void cboName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboBlueprint.SelectedValue is Unlockable u)
                _split.Blueprint = u;
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

        public override ComboBox ComboBox => this.cboBlueprint;
        public override CheckBox CbSplitOnce => this.cbSplitOnce;
        public override Button BtnEdit => this.btnEdit;
        public override Button BtnRemove => this.btnRemove;
        public override SplitName SplitName => SplitName.Blueprint;
        public override SubnauticaSplit Split => this._split;
    }

    public class BlueprintSplit : SubnauticaSplit
    {
        public Unlockable Blueprint { get; set; }

        public BlueprintSplit(Unlockable bp, bool onlySplitOnce)
        {
            Blueprint = bp;
            this.OnlySplitOnce = onlySplitOnce;
            this.SplitName = SplitName.Blueprint;
        }
        public override string GetDescription() => $"{Localization.GetDisplayName(Blueprint)} unlock Split";
    }
}
