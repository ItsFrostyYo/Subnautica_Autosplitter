using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Livesplit.Subnautica
{
    public partial class SubnauticaBiomeSplit : SubnauticaSplitSetting
    {
        public BiomeSplit _split = new BiomeSplit((Biome.None, Biome.None), true);

        private int mX = 0;
        private int mY = 0;
        private bool isDragging = false;

        public SubnauticaBiomeSplit()
        {
            InitializeComponent();
            cboBiome1.MouseWheel += (o, e) => ((HandledMouseEventArgs)e).Handled = true;
            cboBiome1.DisplayMember = "Display";
            cboBiome1.ValueMember = "Value";

            cboBiome2.MouseWheel += (o, e) => ((HandledMouseEventArgs)e).Handled = true;
            cboBiome2.DisplayMember = "Display";
            cboBiome2.ValueMember = "Value";
        }

        private void cbSplitOnce_CheckedChanged(object sender, EventArgs e)
        {
            _split.OnlySplitOnce = cbSplitOnce.Checked;
        }

        private void cboBiome_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboBiome1.SelectedValue is Biome biome1 && cboBiome2.SelectedValue is Biome biome2)
            {
                _split.Biomes.Biome1 = biome1;
                _split.Biomes.Biome2 = biome2;
            }
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

        public override ComboBox ComboBox => this.cboBiome1;
        public override ComboBox ComboBox2 => this.cboBiome2;
        public override CheckBox CbSplitOnce => this.cbSplitOnce;
        public override Button BtnEdit => this.btnEdit;
        public override Button BtnRemove => this.btnRemove;
        public override SplitName SplitName => SplitName.Biome;
        public override SubnauticaSplit Split => this._split;
    }

    public class BiomeSplit : SubnauticaSplit
    {
        public (Biome Biome1, Biome Biome2) Biomes;

        public BiomeSplit((Biome biome1, Biome biome2) biomes, bool onlySplitOnce)
        {
            Biomes.Biome1 = biomes.biome1;
            Biomes.Biome2 = biomes.biome2;
            this.OnlySplitOnce = onlySplitOnce;
            this.SplitName = SplitName.Biome;
        }
        public override string GetDescription() => $"From {Biomes.Biome1} to {Biomes.Biome2} Split";
    }
}
