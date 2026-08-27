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
            ClientSize = new Size(350, 143);
            category = settings.Category;
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
                RowCount = 3,
                Size = new Size(334, 87)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 29f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < layout.RowCount; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 29f));

            displayValueComboBox = CreateDropDown();
            displayValueComboBox.Items.AddRange(new object[] { "#", "%" });
            displayValueComboBox.SelectedItem = settings.DisplayValue == TrackerDisplayValue.Percentage
                ? "%"
                : "#";

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

            toolTip.SetToolTip(displayValueComboBox, "Choose # for an unlocked/total count or % for completion percentage.");
            toolTip.SetToolTip(textCenteringComboBox, "Align this row's text to the left, right, or center.");
            toolTip.SetToolTip(textColorButton, "Choose this row's text color. The default is white.");

            AddSettingRow(layout, 0, "Display Value:", displayValueComboBox, true);
            AddSettingRow(layout, 1, "Text Centering:", textCenteringComboBox, true);
            AddSettingRow(layout, 2, "Text Color:", textColorButton, false);

            var okButton = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(186, 107),
                Size = new Size(75, 25),
                Text = "OK",
                UseVisualStyleBackColor = true
            };
            var cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(267, 107),
                Size = new Size(75, 25),
                Text = "Cancel",
                UseVisualStyleBackColor = true
            };
            var resetButton = new Button
            {
                Location = new Point(8, 107),
                Size = new Size(115, 25),
                Text = "Reset to Defaults",
                UseVisualStyleBackColor = true
            };
            resetButton.Click += ResetButtonClick;

            toolTip.SetToolTip(resetButton, "Restore this category's default value format, centered alignment, and white text.");
            toolTip.SetToolTip(okButton, "Save these row settings.");
            toolTip.SetToolTip(cancelButton, "Close without changing this row.");

            AcceptButton = okButton;
            CancelButton = cancelButton;
            Controls.Add(layout);
            Controls.Add(resetButton);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
        }

        public TrackerDisplayValue DisplayValue =>
            string.Equals(displayValueComboBox.SelectedItem as string, "%", StringComparison.Ordinal)
                ? TrackerDisplayValue.Percentage
                : TrackerDisplayValue.Number;

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
            displayValueComboBox.SelectedItem =
                TrackerRowSettings.GetDefaultDisplayValue(category) == TrackerDisplayValue.Percentage
                    ? "%"
                    : "#";
            textCenteringComboBox.SelectedItem = TrackerTextCentering.Center.ToString();
            textColorButton.BackColor = Color.White;
        }
    }
}
