using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Voxif.AutoSplitter;

namespace LiveSplit.Subnautica
{
    // TODO: Add tooltips to the items while in the dropdown menu
    public partial class SubnauticaPrefabSplit : SubnauticaSplitSetting
    {
        public PrefabSplit _split;

        private int mX = 0;
        private int mY = 0;
        private bool isDragging = false;

        public SubnauticaPrefabSplit() : this(new PrefabSplit(SplitName.None, onlySplitOnce: true, isSubCondition: false)) { }
        public SubnauticaPrefabSplit(PrefabSplit prefabSplit)
        {
            InitializeComponent();

            _split = prefabSplit ?? new PrefabSplit(SplitName.None, onlySplitOnce: true, isSubCondition: false);

            cboName.DropDownStyle = ComboBoxStyle.DropDownList;
            cboName.MouseWheel += (o, e) => ((HandledMouseEventArgs)e).Handled = true;
            cboName.DisplayMember = "Display";
            cboName.ValueMember = "Value";
        }

        private void BtnOptions_Click(object sender, EventArgs e)
        {
            var splitSettings = new SubnauticaPrefabSplitSettings(_split);
            var settings = new SplitSettingsDialog(splitSettings) { StartPosition = FormStartPosition.CenterParent };

            if (settings.ShowDialog() == DialogResult.OK)
            {
                _split.OnlySplitOnce = splitSettings.OnlySplitOnce;
                _split.Conditions = splitSettings.Splits;
            }
        }

        private void cboName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (IsLoading)
                return;

            string splitDescription = cboName.SelectedValue.ToString();
            SplitName split = GetSplitName(splitDescription);
            _split.SplitName = split;

            MemberInfo info = typeof(SplitName).GetMember(split.ToString())[0];
            DescriptionAttribute description = (DescriptionAttribute)info.GetCustomAttributes(typeof(DescriptionAttribute), false)[0];
            ToolTipAttribute tooltip = (ToolTipAttribute)info.GetCustomAttributes(typeof(ToolTipAttribute), false)[0];
            ToolTips.SetToolTip(cboName, tooltip.ToolTip);
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

        public override ComboBox ComboBox => this.cboName;
        public override Button BtnEdit => this.btnEdit;
        public override Button BtnRemove => this.btnRemove;
        public override SplitName SplitName => GetSplitName(cboName.Text);
        public override SubnauticaSplit Split => this._split;
    }

    public class PrefabSplit : SubnauticaSplit
    {
        public PrefabSplit(SplitName splitName, bool onlySplitOnce, bool isSubCondition)
        {
            this.SplitName = splitName;
            this.OnlySplitOnce = onlySplitOnce;
            this.IsSubCondition = isSubCondition;
        }
        public override string GetDescription() => this.SplitName.GetDescription();
    }
}
