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
        private readonly ComboBox timerDigitsComboBox;
        private readonly ComboBox timerAccuracyComboBox;
        private readonly TrackBar timerDecimalsTrackBar;
        private readonly CheckBox timerGradientCheckBox;
        private readonly CheckBox timerCenterCheckBox;
        private readonly Button timerColorButton;
        private readonly TrackBar timerHeightTrackBar;
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
            bool isDamage = category == TrackerRowCategory.DamageTaken;
            bool isExplosion = category == TrackerRowCategory.ExplosionTime;
            bool isBulbTimer = category == TrackerRowCategory.BulbZoneTimer;
            int settingRows = isBulbTimer ? 6 : isDamage ? 4 : isExplosion ? 2 : 3;
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
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < layout.RowCount; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 29f));

            displayValueComboBox = CreateDropDown();
            if (isDamage)
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
            textColorButton = CreateColorButton(settings.TextColor);
            textColorButton.Click += TextColorButtonClick;
            excludeIntroDamageCheckBox = new CheckBox
            {
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Checked = settings.ExcludeIntroDamage,
                Text = "Enabled"
            };

            timerDigitsComboBox = CreateDropDown();
            timerDigitsComboBox.Items.AddRange(new object[] { "1", "00:01", "0:00:01", "00:00:01" });
            timerDigitsComboBox.SelectedItem = settings.TimerDigitsFormat;
            timerAccuracyComboBox = CreateDropDown();
            timerAccuracyComboBox.Items.AddRange(new object[] { "", ".2", ".23", ".234" });
            timerAccuracyComboBox.SelectedItem = settings.TimerAccuracy;
            timerDecimalsTrackBar = CreateTrackBar(10, 50, (int)settings.TimerDecimalsSize);
            timerGradientCheckBox = CreateCheckBox("Show Gradient", settings.TimerShowGradient);
            timerCenterCheckBox = CreateCheckBox("Align to Center", settings.TimerCenter);
            timerColorButton = CreateColorButton(settings.TimerColor);
            timerColorButton.Click += TimerColorButtonClick;
            timerHeightTrackBar = CreateTrackBar(20, 150, (int)settings.TimerHeight);

            if (isBulbTimer)
            {
                var timerFormatPanel = new TableLayoutPanel
                {
                    ColumnCount = 2,
                    Dock = DockStyle.Fill,
                    Margin = Padding.Empty
                };
                timerFormatPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
                timerFormatPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
                timerFormatPanel.Controls.Add(timerDigitsComboBox, 0, 0);
                timerFormatPanel.Controls.Add(timerAccuracyComboBox, 1, 0);
                AddSettingRow(layout, 0, "Timer Format:", timerFormatPanel, true);
                AddSettingRow(layout, 1, "Decimals Font Size:", timerDecimalsTrackBar, true);
                layout.Controls.Add(timerGradientCheckBox, 0, 2);
                layout.SetColumnSpan(timerGradientCheckBox, 3);
                layout.Controls.Add(timerCenterCheckBox, 0, 3);
                layout.SetColumnSpan(timerCenterCheckBox, 3);
                AddSettingRow(layout, 4, "Timer Color:", timerColorButton, true);
                AddSettingRow(layout, 5, "Height:", timerHeightTrackBar, true);
            }
            else
            {
                int row = 0;
                if (!isExplosion)
                    AddSettingRow(layout, row++, "Display Value:", displayValueComboBox, true);
                AddSettingRow(layout, row++, "Text Centering:", textCenteringComboBox, true);
                AddSettingRow(layout, row++, "Text Color:", textColorButton, true);
                if (isDamage)
                    AddSettingRow(layout, row, "Exclude Intro Damage:", excludeIntroDamageCheckBox, true);
            }

            SetToolTips(isDamage, isBulbTimer);

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
            toolTip.SetToolTip(resetButton, "Restore this row category's default settings.");
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
                    return selected == "Hits Taken" ? TrackerDisplayValue.HitsTaken : TrackerDisplayValue.DamageTaken;
                return selected == "%" ? TrackerDisplayValue.Percentage : TrackerDisplayValue.Number;
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
        public string TimerDigitsFormat => timerDigitsComboBox.SelectedItem as string ?? "1";
        public string TimerAccuracy => timerAccuracyComboBox.SelectedItem as string ?? ".23";
        public float TimerDecimalsSize => timerDecimalsTrackBar.Value;
        public bool TimerShowGradient => timerGradientCheckBox.Checked;
        public bool TimerCenter => timerCenterCheckBox.Checked;
        public Color TimerColor => timerColorButton.BackColor;
        public float TimerHeight => timerHeightTrackBar.Value;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                toolTip.Dispose();
            base.Dispose(disposing);
        }

        private void SetToolTips(bool isDamage, bool isBulbTimer)
        {
            toolTip.SetToolTip(displayValueComboBox, isDamage
                ? "Choose Hits Taken for the number of damaging hits, or # Taken for total health damage."
                : "Choose # for an unlocked/total count or % for completion percentage.");
            toolTip.SetToolTip(textCenteringComboBox, "Align this row's text to the left, right, or center.");
            toolTip.SetToolTip(textColorButton, "Choose this row's text color. The default is white.");
            toolTip.SetToolTip(excludeIntroDamageCheckBox,
                "Exclude the unavoidable first 20 damage from the lifepod intro. Enabled by default.");
            if (isBulbTimer)
            {
                toolTip.SetToolTip(timerDigitsComboBox, "Choose the timer's digit layout.");
                toolTip.SetToolTip(timerAccuracyComboBox, "Choose how many decimal places the timer displays.");
                toolTip.SetToolTip(timerDecimalsTrackBar, "Adjust the decimals font size, matching LiveSplit's Timer component.");
                toolTip.SetToolTip(timerGradientCheckBox, "Apply LiveSplit's timer text gradient.");
                toolTip.SetToolTip(timerCenterCheckBox, "Center the timer instead of aligning it to the right.");
                toolTip.SetToolTip(timerColorButton, "Choose the Bulb Zone timer color.");
                toolTip.SetToolTip(timerHeightTrackBar, "Set this timer row's height.");
            }
        }

        private static ComboBox CreateDropDown()
        {
            return new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        private static Button CreateColorButton(Color color)
        {
            return new Button
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = color,
                FlatStyle = FlatStyle.Popup,
                Margin = new Padding(3),
                UseVisualStyleBackColor = false
            };
        }

        private static TrackBar CreateTrackBar(int minimum, int maximum, int value)
        {
            return new TrackBar
            {
                Dock = DockStyle.Fill,
                Minimum = minimum,
                Maximum = maximum,
                TickStyle = TickStyle.None,
                Value = Math.Max(minimum, Math.Min(maximum, value))
            };
        }

        private static CheckBox CreateCheckBox(string text, bool value)
        {
            return new CheckBox
            {
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Checked = value,
                Text = text
            };
        }

        private static void AddSettingRow(TableLayoutPanel layout, int row, string labelText, Control control, bool spanControl)
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

        private void TimerColorButtonClick(object sender, EventArgs e)
        {
            SettingsHelper.ColorButtonClick(timerColorButton, this);
        }

        private void ResetButtonClick(object sender, EventArgs e)
        {
            var defaults = new TrackerRowSettings();
            defaults.ResetToDefaults(category);
            displayValueComboBox.SelectedItem = category == TrackerRowCategory.DamageTaken
                ? "# Taken"
                : defaults.DisplayValue == TrackerDisplayValue.Percentage ? "%" : "#";
            textCenteringComboBox.SelectedItem = defaults.TextCentering.ToString();
            textColorButton.BackColor = defaults.TextColor;
            excludeIntroDamageCheckBox.Checked = defaults.ExcludeIntroDamage;
            timerDigitsComboBox.SelectedItem = defaults.TimerDigitsFormat;
            timerAccuracyComboBox.SelectedItem = defaults.TimerAccuracy;
            timerDecimalsTrackBar.Value = (int)defaults.TimerDecimalsSize;
            timerGradientCheckBox.Checked = defaults.TimerShowGradient;
            timerCenterCheckBox.Checked = defaults.TimerCenter;
            timerColorButton.BackColor = defaults.TimerColor;
            timerHeightTrackBar.Value = (int)defaults.TimerHeight;
        }
    }
}
