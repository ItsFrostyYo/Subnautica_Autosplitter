using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Livesplit.Subnautica
{
    public partial class SubnauticaItemSplit : SubnauticaSplitSetting
    {
        public ItemSplit _split = new ItemSplit(InventoryItem.None, true);

        private int mX = 0;
        private int mY = 0;
        private bool isDragging = false;

        public SubnauticaItemSplit()
        {
            InitializeComponent();
            cboItem.DropDownStyle = ComboBoxStyle.DropDownList;
            cboItem.MouseWheel += (o, e) => ((HandledMouseEventArgs)e).Handled = true;
            cboItem.DisplayMember = "Display";
            cboItem.ValueMember = "Value";
        }

        private void cboName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboItem.SelectedValue is InventoryItem t)
                _split.Item = t;
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

        public override ComboBox ComboBox => this.cboItem;
        public override CheckBox CbSplitOnce => this.cbSplitOnce;
        public override Button BtnEdit => this.btnEdit;
        public override Button BtnRemove => this.btnRemove;
        public override SplitName SplitName => SplitName.Inventory;
        public override SubnauticaSplit Split => this._split;
    }

    public class ItemSplit : SubnauticaSplit
    {
        public InventoryItem Item { get; set; }

        public ItemSplit(InventoryItem item, bool onlySplitOnce)
        {
            Item = item;
            this.OnlySplitOnce = onlySplitOnce;
            this.SplitName = SplitName.Inventory;
        }
        public override string GetDescription() => $"{Localization.GetDisplayName(Item)} in Inventory Split";
    }
}
