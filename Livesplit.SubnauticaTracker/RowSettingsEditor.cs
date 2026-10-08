using LiveSplit.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace LiveSplit.SubnauticaTracker
{
    internal sealed class RowSettingsEditor : Form
    {
        private readonly ComboBox displayValueComboBox;
        private readonly ComboBox textCenteringComboBox;
        private readonly Button textColorButton;
        private readonly CheckBox excludeIntroDamageCheckBox;
        private readonly TrackerRowCategory category;
        private readonly ToolTip toolTip;

        public RowSettingsEditor(int rowNumber, TrackerRowSettings settings)
        {
            Text = "Row " + rowNumber + " Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            category = settings.Category;
            int settingRows = category == TrackerRowCategory.DamageTaken ? 4 : 3;
            int buttonsY = 20 + settingRows * 29;
            ClientSize = new Size(350, buttonsY + 36);
            toolTip = new ToolTip
            {
                AutoPopDelay = 10000,
                InitialDelay = 400,
                ReshowDelay = 100
            };

            var layout = new TableLayoutPanel
            {
                ColumnCount = 3,
                Dock = DockStyle.Top,
                Location = new Point(8, 8),
                RowCount = settingRows,
                Size = new Size(334, settingRows * 29)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 29f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < layout.RowCount; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 29f));

            displayValueComboBox = CreateDropDown();
            if (category == TrackerRowCategory.DamageTaken)
            {
                displayValueComboBox.Items.AddRange(new object[] { "Hits Taken", "# Taken" });
                displayValueComboBox.SelectedItem = settings.DisplayValue == TrackerDisplayValue.HitsTaken
                    ? "Hits Taken"
                    : "# Taken";
            }
            else
            {
                displayValueComboBox.Items.AddRange(new object[] { "#", "%" });
                displayValueComboBox.SelectedItem = settings.DisplayValue == TrackerDisplayValue.Percentage
                    ? "%"
                    : "#";
            }

            textCenteringComboBox = CreateDropDown();
            textCenteringComboBox.Items.AddRange(new object[] { "Left", "Right", "Center" });
            textCenteringComboBox.SelectedItem = settings.TextCentering.ToString();

            textColorButton = new Button
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = settings.TextColor,
                FlatStyle = FlatStyle.Popup,
                Margin = new Padding(3),
                UseVisualStyleBackColor = false
            };
            textColorButton.Click += TextColorButtonClick;

            excludeIntroDamageCheckBox = new CheckBox
            {
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Checked = settings.ExcludeIntroDamage,
                Text = "Enabled"
            };

            toolTip.SetToolTip(
                displayValueComboBox,
                category == TrackerRowCategory.DamageTaken
                    ? "Choose Hits Taken for the number of damaging hits, or # Taken for total health damage."
                    : "Choose # for an unlocked/total count or % for completion percentage.");
            toolTip.SetToolTip(textCenteringComboBox, "Align this row's text to the left, right, or center.");
            toolTip.SetToolTip(textColorButton, "Choose this row's text color. The default is white.");
            toolTip.SetToolTip(
                excludeIntroDamageCheckBox,
                "Exclude the unavoidable first 20 damage from the lifepod intro. Enabled by default.");

            AddSettingRow(layout, 0, "Display Value:", displayValueComboBox, true);
            AddSettingRow(layout, 1, "Text Centering:", textCenteringComboBox, true);
            AddSettingRow(layout, 2, "Text Color:", textColorButton, false);
            if (category == TrackerRowCategory.DamageTaken)
                AddSettingRow(layout, 3, "Exclude Intro Damage:", excludeIntroDamageCheckBox, true);

            var okButton = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(186, buttonsY),
                Size = new Size(75, 25),
                Text = "OK",
                UseVisualStyleBackColor = true
            };
            var cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(267, buttonsY),
                Size = new Size(75, 25),
                Text = "Cancel",
                UseVisualStyleBackColor = true
            };
            var resetButton = new Button
            {
                Location = new Point(8, buttonsY),
                Size = new Size(115, 25),
                Text = "Reset to Defaults",
                UseVisualStyleBackColor = true
            };
            resetButton.Click += ResetButtonClick;

            toolTip.SetToolTip(resetButton, "Restore this category's default value format, alignment, color, and intro-damage setting.");
            toolTip.SetToolTip(okButton, "Save these row settings.");
            toolTip.SetToolTip(cancelButton, "Close without changing this row.");

            AcceptButton = okButton;
            CancelButton = cancelButton;
            Controls.Add(layout);
            Controls.Add(resetButton);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
        }

        public TrackerDisplayValue DisplayValue
        {
            get
            {
                string selected = displayValueComboBox.SelectedItem as string;
                if (category == TrackerRowCategory.DamageTaken)
                {
                    return string.Equals(selected, "Hits Taken", StringComparison.Ordinal)
                        ? TrackerDisplayValue.HitsTaken
                        : TrackerDisplayValue.DamageTaken;
                }

                return string.Equals(selected, "%", StringComparison.Ordinal)
                    ? TrackerDisplayValue.Percentage
                    : TrackerDisplayValue.Number;
            }
        }

        public TrackerTextCentering TextCentering
        {
            get
            {
                TrackerTextCentering value;
                return Enum.TryParse(textCenteringComboBox.SelectedItem as string, out value)
                    ? value
                    : TrackerTextCentering.Center;
            }
        }

        public Color TextColor => textColorButton.BackColor;
        public bool ExcludeIntroDamage => excludeIntroDamageCheckBox.Checked;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                toolTip.Dispose();
            base.Dispose(disposing);
        }

        private static ComboBox CreateDropDown()
        {
            return new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        private static void AddSettingRow(
            TableLayoutPanel layout,
            int row,
            string labelText,
            Control control,
            bool spanControl)
        {
            layout.Controls.Add(new Label
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = true,
                Text = labelText
            }, 0, row);
            layout.Controls.Add(control, 1, row);
            if (spanControl)
                layout.SetColumnSpan(control, 2);
        }

        private void TextColorButtonClick(object sender, EventArgs e)
        {
            SettingsHelper.ColorButtonClick(textColorButton, this);
        }

        private void ResetButtonClick(object sender, EventArgs e)
        {
            TrackerDisplayValue defaultValue = TrackerRowSettings.GetDefaultDisplayValue(category);
            displayValueComboBox.SelectedItem = category == TrackerRowCategory.DamageTaken
                ? "# Taken"
                : defaultValue == TrackerDisplayValue.Percentage ? "%" : "#";
            textCenteringComboBox.SelectedItem = TrackerTextCentering.Center.ToString();
            textColorButton.BackColor = Color.White;
            excludeIntroDamageCheckBox.Checked = true;
        }
    }
}
