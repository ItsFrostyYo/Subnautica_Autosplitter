using LiveSplit.SubnauticaMarco.Configuration;
using LiveSplit.Model.Input;
using LiveSplit.UI;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.SubnauticaMarco
{
    public sealed class Settings : UserControl
    {
        private readonly CheckBox enabledCheckBox;
        private readonly TextBox hotkeyTextBox;
        private readonly ComboBox gameModeComboBox;
        private readonly CheckBox explosionCheckBox;
        private readonly ComboBox explosionPresetComboBox;
        private readonly TextBox customMinimumTextBox;
        private readonly TextBox customMaximumTextBox;
        private readonly Label customRangeLabel;
        private readonly ToolTip toolTip;
        private readonly CheckBox saveDeleterCheckBox;
        private readonly ComboBox saveGameModeComboBox;
        private readonly ComboBox saveTimingComboBox;
        private bool capturingHotkey;
        private int hotkey;
        private string hotkeyTextBeforeCapture;

        internal event EventHandler HotkeyCaptureChanged;
        internal bool IsCapturingHotkey => capturingHotkey;

        public Settings()
        {
            hotkey = (int)Keys.F5;
            toolTip = new ToolTip
            {
                AutoPopDelay = 10000,
                InitialDelay = 400,
                ReshowDelay = 100
            };

            enabledCheckBox = new CheckBox
            {
                AutoSize = true,
                Text = "Enable Reset Macro"
            };
            hotkeyTextBox = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                Text = Keys.F5.ToString(),
                Width = 150
            };
            gameModeComboBox = CreateDropDown();
            gameModeComboBox.Items.AddRange(new object[]
            {
                "Survival (Vanilla)",
                "Hardcore (Vanilla)",
                "Creative (Vanilla)",
                "Freedom (Vanilla)",
                "BetterRNG Survival (Speedrun Mod)",
                "BetterRNG Hardcore (Speedrun Mod)",
                "BetterRNG Creative (Speedrun Mod)"
            });

            explosionCheckBox = new CheckBox
            {
                AutoSize = true,
                Text = "Explosion Reset Macro"
            };
            explosionPresetComboBox = CreateDropDown();
            explosionPresetComboBox.Items.AddRange(new object[]
            {
                "46:00 - 46:30",
                "46:00 - 47:00",
                "46:00 - 48:00",
                "46:00 - 50:00",
                "Under 1 Hour",
                "Over 1 Hour",
                "Custom"
            });

            saveDeleterCheckBox = new CheckBox { AutoSize = true, Text = "Hardcore Save Deleter" };
            saveGameModeComboBox = CreateDropDown();
            saveGameModeComboBox.Items.AddRange(new object[] { "Hardcore", "All Gamemodes" });
            saveTimingComboBox = CreateDropDown();
            saveTimingComboBox.Items.AddRange(new object[] { "After Reset Completes", "Automatically Delete all Saves" });

            customRangeLabel = new Label
            {
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Text = "Custom Range:"
            };
            customMinimumTextBox = new TextBox { Text = "46:00", Width = 65 };
            customMaximumTextBox = new TextBox { Text = "46:30", Width = 65 };

            var customRangePanel = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = Padding.Empty,
                WrapContents = false
            };
            customRangePanel.Controls.Add(customMinimumTextBox);
            customRangePanel.Controls.Add(new Label
            {
                AutoSize = true,
                Margin = new Padding(6, 5, 6, 0),
                Text = "to"
            });
            customRangePanel.Controls.Add(customMaximumTextBox);

            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Dock = DockStyle.Top,
                Location = new Point(7, 7),
                Padding = new Padding(7)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            AddRow(layout, 0, string.Empty, enabledCheckBox);
            AddRow(layout, 1, "Reset Hotkey:", hotkeyTextBox);
            AddRow(layout, 2, "Game Mode:", gameModeComboBox);
            AddRow(layout, 3, string.Empty, explosionCheckBox);
            AddRow(layout, 4, "Explosion Preset:", explosionPresetComboBox);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 29f));
            layout.Controls.Add(customRangeLabel, 0, 5);
            layout.Controls.Add(customRangePanel, 1, 5);

            AddRow(layout, 6, string.Empty, saveDeleterCheckBox);
            AddRow(layout, 7, "Gamemode:", saveGameModeComboBox);
            AddRow(layout, 8, "When to Delete:", saveTimingComboBox);

            AutoScaleDimensions = new SizeF(6f, 13f);
            AutoScaleMode = AutoScaleMode.Font;
            Size = new Size(476, 292);
            Controls.Add(layout);

            enabledCheckBox.CheckedChanged += delegate { RefreshEnabledState(); };
            hotkeyTextBox.Enter += BeginHotkeyCapture;
            hotkeyTextBox.KeyDown += CaptureHotkeyDown;
            hotkeyTextBox.KeyUp += CaptureHotkeyUp;
            hotkeyTextBox.Leave += CancelHotkeyCapture;
            saveDeleterCheckBox.CheckedChanged += delegate { RefreshEnabledState(); };
            explosionCheckBox.CheckedChanged += delegate { RefreshEnabledState(); };
            explosionPresetComboBox.SelectedIndexChanged += delegate { RefreshEnabledState(); };

            toolTip.SetToolTip(enabledCheckBox, "Enable the global reset hotkey while LiveSplit is running.");
            toolTip.SetToolTip(hotkeyTextBox, "Select this box, then press the keyboard hotkey used to run or cancel the reset macro. Press Escape to clear it.");
            toolTip.SetToolTip(gameModeComboBox, "Choose the game mode that the reset macro starts. BetterRNG presets use the mod's 2018/2021 menu flow.");
            toolTip.SetToolTip(explosionCheckBox, "Repeatedly reset until the memory-read Aurora explosion time is inside the selected range. Press the hotkey again to cancel.");
            toolTip.SetToolTip(explosionPresetComboBox, "Choose the acceptable Aurora explosion time range.");
            toolTip.SetToolTip(customMinimumTextBox, "Custom minimum explosion time in minutes:seconds.");
            toolTip.SetToolTip(customMaximumTextBox, "Custom maximum explosion time in minutes:seconds.");

            toolTip.SetToolTip(saveDeleterCheckBox, "Permanently deletes matching slot#### save folders from the running game installation. This cannot be undone.");
            toolTip.SetToolTip(saveTimingComboBox, "After Reset Completes deletes the latest existing matching save only after a successful in-game reset. Automatic scans all existing and new matching save folders every 5 seconds while enabled.");
            saveGameModeComboBox.SelectedIndex = 0;
            saveTimingComboBox.SelectedIndex = 0;
            gameModeComboBox.SelectedIndex = 0;
            explosionPresetComboBox.SelectedIndex = 0;
            RefreshEnabledState();
        }

        internal MacroConfiguration GetConfiguration()
        {
            int minimum;
            int maximum;
            if (!TryParseTime(customMinimumTextBox.Text, out minimum))
                minimum = 2760;
            if (!TryParseTime(customMaximumTextBox.Text, out maximum))
                maximum = 2790;

            return new MacroConfiguration
            {
                Enabled = enabledCheckBox.Checked,
                HardcoreSaveDeleter = saveDeleterCheckBox.Checked,
                SaveDeletionGameMode = (SaveDeletionGameMode)Math.Max(0, saveGameModeComboBox.SelectedIndex),
                SaveDeletionTiming = (SaveDeletionTiming)Math.Max(0, saveTimingComboBox.SelectedIndex),
                Hotkey = hotkey,
                GameMode = (ResetGameMode)Math.Max(0, gameModeComboBox.SelectedIndex),
                ExplosionReset = explosionCheckBox.Checked,
                ExplosionPreset = (ExplosionPreset)Math.Max(0, explosionPresetComboBox.SelectedIndex),
                CustomMinimumSeconds = minimum,
                CustomMaximumSeconds = Math.Max(minimum, maximum)
            };
        }

        public void SetSettings(XmlNode node)
        {
            var element = node as XmlElement;
            if (element == null)
                return;

            saveDeleterCheckBox.Checked = SettingsHelper.ParseBool(element["HardcoreSaveDeleter"], false);
            saveGameModeComboBox.SelectedIndex = Clamp((int)SettingsHelper.ParseEnum(element["SaveDeletionGameMode"], SaveDeletionGameMode.Hardcore), 0, 1);
            saveTimingComboBox.SelectedIndex = Clamp((int)SettingsHelper.ParseEnum(element["SaveDeletionTiming"], SaveDeletionTiming.AfterResetCompletes), 0, 1);
            enabledCheckBox.Checked = SettingsHelper.ParseBool(element["Enabled"], false);
            hotkey = SettingsHelper.ParseInt(element["Hotkey"], (int)Keys.F5);
            string settingsVersion = SettingsHelper.ParseString(element["Version"], "1.0");
            Version parsedVersion;
            if (hotkey == 0
                && Version.TryParse(settingsVersion, out parsedVersion)
                && parsedVersion <= new Version(1, 0))
            {
                hotkey = (int)Keys.F5;
            }
            gameModeComboBox.SelectedIndex = Clamp(
                (int)SettingsHelper.ParseEnum(element["GameMode"], ResetGameMode.Survival),
                0,
                gameModeComboBox.Items.Count - 1);
            explosionCheckBox.Checked = SettingsHelper.ParseBool(element["ExplosionReset"], false);
            explosionPresetComboBox.SelectedIndex = Clamp(
                (int)SettingsHelper.ParseEnum(element["ExplosionPreset"], ExplosionPreset.Minutes46To46_30),
                0,
                explosionPresetComboBox.Items.Count - 1);
            customMinimumTextBox.Text = FormatTime(SettingsHelper.ParseInt(element["CustomMinimumSeconds"], 2760));
            customMaximumTextBox.Text = FormatTime(SettingsHelper.ParseInt(element["CustomMaximumSeconds"], 2790));
            UpdateHotkeyText();
            RefreshEnabledState();
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            MacroConfiguration configuration = GetConfiguration();
            XmlElement settings = document.CreateElement("Settings");
            SettingsHelper.CreateSetting(document, settings, "Version", "1.2");
            SettingsHelper.CreateSetting(document, settings, "HardcoreSaveDeleter", configuration.HardcoreSaveDeleter);
            SettingsHelper.CreateSetting(document, settings, "SaveDeletionGameMode", configuration.SaveDeletionGameMode);
            SettingsHelper.CreateSetting(document, settings, "SaveDeletionTiming", configuration.SaveDeletionTiming);
            SettingsHelper.CreateSetting(document, settings, "Enabled", configuration.Enabled);
            SettingsHelper.CreateSetting(document, settings, "Hotkey", configuration.Hotkey);
            SettingsHelper.CreateSetting(document, settings, "GameMode", configuration.GameMode);
            SettingsHelper.CreateSetting(document, settings, "ExplosionReset", configuration.ExplosionReset);
            SettingsHelper.CreateSetting(document, settings, "ExplosionPreset", configuration.ExplosionPreset);
            SettingsHelper.CreateSetting(document, settings, "CustomMinimumSeconds", configuration.CustomMinimumSeconds);
            SettingsHelper.CreateSetting(document, settings, "CustomMaximumSeconds", configuration.CustomMaximumSeconds);
            return settings;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                toolTip.Dispose();
            base.Dispose(disposing);
        }

        private void BeginHotkeyCapture(object sender, EventArgs args)
        {
            capturingHotkey = true;
            hotkeyTextBeforeCapture = hotkeyTextBox.Text;
            hotkeyTextBox.Text = "Set Hotkey...";
            hotkeyTextBox.Select(0, 0);
            HotkeyCaptureChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CaptureHotkeyDown(object sender, KeyEventArgs args)
        {
            if (!capturingHotkey)
                return;

            args.Handled = true;
            args.SuppressKeyPress = true;

            if (args.KeyCode != Keys.ShiftKey
                && args.KeyCode != Keys.ControlKey
                && args.KeyCode != Keys.Menu)
            {
                CompleteHotkeyCapture(args.KeyCode == Keys.Escape
                    ? Keys.None
                    : args.KeyCode | args.Modifiers);
            }
        }

        private void CaptureHotkeyUp(object sender, KeyEventArgs args)
        {
            if (!capturingHotkey)
                return;

            if (args.KeyCode == Keys.ShiftKey
                || args.KeyCode == Keys.ControlKey
                || args.KeyCode == Keys.Menu)
            {
                CompleteHotkeyCapture(args.KeyCode == Keys.Escape
                    ? Keys.None
                    : args.KeyCode | args.Modifiers);
            }
        }

        private void CompleteHotkeyCapture(Keys value)
        {
            hotkey = (int)value;
            capturingHotkey = false;
            UpdateHotkeyText();
            enabledCheckBox.Focus();
            HotkeyCaptureChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CancelHotkeyCapture(object sender, EventArgs args)
        {
            if (!capturingHotkey)
                return;

            capturingHotkey = false;
            hotkeyTextBox.Text = hotkeyTextBeforeCapture;
            HotkeyCaptureChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateHotkeyText()
        {
            hotkeyTextBox.Text = hotkey <= 0
                ? "None"
                : new KeyOrButton((Keys)hotkey).ToString();
        }

        private void RefreshEnabledState()
        {
            saveGameModeComboBox.Enabled = saveDeleterCheckBox.Checked;
            saveTimingComboBox.Enabled = saveDeleterCheckBox.Checked;
            hotkeyTextBox.Enabled = enabledCheckBox.Checked;
            gameModeComboBox.Enabled = enabledCheckBox.Checked;
            explosionCheckBox.Enabled = enabledCheckBox.Checked;
            explosionPresetComboBox.Enabled = enabledCheckBox.Checked && explosionCheckBox.Checked;
            bool custom = explosionPresetComboBox.SelectedIndex == (int)ExplosionPreset.Custom;
            customRangeLabel.Enabled = enabledCheckBox.Checked && explosionCheckBox.Checked && custom;
            customMinimumTextBox.Enabled = customRangeLabel.Enabled;
            customMaximumTextBox.Enabled = customRangeLabel.Enabled;
        }

        private static ComboBox CreateDropDown()
        {
            return new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        private static void AddRow(TableLayoutPanel layout, int row, string labelText, Control control)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 29f));
            if (!string.IsNullOrEmpty(labelText))
            {
                layout.Controls.Add(new Label
                {
                    Anchor = AnchorStyles.Left,
                    AutoSize = true,
                    Text = labelText
                }, 0, row);
                layout.Controls.Add(control, 1, row);
            }
            else
            {
                layout.Controls.Add(control, 0, row);
                layout.SetColumnSpan(control, 2);
            }
        }

        private static bool TryParseTime(string text, out int seconds)
        {
            seconds = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string[] parts = text.Trim().Split(':');
            int minutes;
            int remainingSeconds;
            if (parts.Length != 2
                || !int.TryParse(parts[0], out minutes)
                || !int.TryParse(parts[1], out remainingSeconds)
                || minutes < 0 || remainingSeconds < 0 || remainingSeconds > 59)
            {
                return false;
            }

            seconds = minutes * 60 + remainingSeconds;
            return true;
        }

        private static string FormatTime(int seconds)
        {
            seconds = Math.Max(0, seconds);
            return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
