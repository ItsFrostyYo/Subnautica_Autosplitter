using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.UI.Components;

namespace LiveSplit.Subnautica
{
    public partial class SplitSettingsDialog : Form
    {
        public SplitSettingsDialog(SubnauticaBaseSettings settings)
        {
            InitializeComponent();
            MainPanel.Controls.Add(settings);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
