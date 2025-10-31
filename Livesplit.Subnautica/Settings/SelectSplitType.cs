using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Livesplit.Subnautica.Settings
{
    public partial class SelectSplitType : Form
    {
        public Func<SubnauticaSplitSetting> Func { get; set; }
        public SelectSplitType(SubnauticaSettings settings)
        {
            InitializeComponent();           
            var items = new List<SplitType>
            {
                new SplitType { Text = "Prefabricated", Func = settings.CreatePrefabSplit },
                new SplitType { Text = "Inventory", Func = settings.CreateItemSplit },
                new SplitType { Text = "Blueprint", Func = settings.CreateBlueprintSplit },
                new SplitType { Text = "Encyclopedia", Func = settings.CreateEncySplit },
            };
            cboSplitType.DisplayMember = nameof(SplitType.Text);
            cboSplitType.ValueMember = nameof(SplitType.Func);
            cboSplitType.DataSource = items;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (cboSplitType.SelectedValue is Func<SubnauticaSplitSetting> func)
            Func = func;
            DialogResult = DialogResult.OK;
        }

        private class SplitType
        {
            public string Text { get; set; }
            public Func<SubnauticaSplitSetting> Func { get; set; }

            public override string ToString() => Text;
        }
    }
}
