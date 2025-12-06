using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiveSplit.Subnautica.Settings
{
    public partial class SelectSplitType : Form
    {
        public Func<bool, SubnauticaSplitSetting> Func { get; set; }
        public SelectSplitType(SubnauticaBaseSettings settings, bool isSubCondition)
        {
            InitializeComponent();

            var items = new List<SplitType>
            {                
                new SplitType { Text = "Inventory", Func = settings.CreateItemSplit },
                new SplitType { Text = "Blueprint", Func = settings.CreateBlueprintSplit },
                new SplitType { Text = "Encyclopedia", Func = settings.CreateEncySplit },
                new SplitType { Text = "Biome", Func = settings.CreateBiomeSplit },
            };

            if (!isSubCondition)
            {
                items.Add(new SplitType { Text = "Prefabricated", Func = settings.CreatePrefabSplit });
                items.Add(new SplitType { Text = "Craft", Func = settings.CreateCraftSplit });
            }

            cboSplitType.DisplayMember = nameof(SplitType.Text);
            cboSplitType.ValueMember = nameof(SplitType.Func);
            cboSplitType.DataSource = items;
        }

        private class SplitType
        {
            public string Text { get; set; }
            public Func<bool, SubnauticaSplitSetting> Func { get; set; }

            public override string ToString() => Text;
        }

        private void btnOK_Click(object sender, EventArgs e) => OK();

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void OK()
        {
            if (cboSplitType.SelectedValue is Func<bool, SubnauticaSplitSetting> func)
                Func = func;
            DialogResult = DialogResult.OK;
        }
    }
}
