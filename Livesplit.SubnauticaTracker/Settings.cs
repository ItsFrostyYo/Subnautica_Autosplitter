using LiveSplit.UI;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.SubnauticaTracker
{
    public sealed class Settings : UserControl
    {
        public const int MaximumRows = 3;

        public static readonly string[] AvailableRowOptions =
        {
            "Completion",
            "Blueprints & Databanks",
            "Blueprints",
            "Databanks",
            "Achievements"
        };

        private static readonly Color DefaultBackgroundColor =
            Color.FromArgb(unchecked((int)0xFF000000));

        private readonly Button firstColorButton;
        private readonly Button secondColorButton;
        private readonly ComboBox gradientTypeComboBox;
        private readonly ComboBox[] rowComboBoxes;
        private readonly Label[] rowLabels;
        private readonly Button[] rowOptionsButtons;
        private readonly Button[] rowRemoveButtons;
        private readonly Button[] rowUpButtons;
        private readonly Button[] rowDownButtons;
        private readonly Button addRowButton;
        private readonly TableLayoutPanel layout;
        private readonly TrackerRowSettings[] rows;
        private readonly ToolTip toolTip;

        private bool refreshingRows;
        private int rowCount;

        public event EventHandler SettingsChanged;

        public Color BackgroundColor { get; set; }
        public Color BackgroundColor2 { get; set; }
        public GradientType BackgroundGradient { get; set; }
        public int RowCount => rowCount;

        public string GradientString
        {
            get => BackgroundGradient.ToString();
            set
            {
                GradientType gradient;
                if (Enum.TryParse(value, out gradient))
                    BackgroundGradient = gradient;
            }
        }

        public Settings()
        {
            BackgroundColor = DefaultBackgroundColor;
            BackgroundColor2 = DefaultBackgroundColor;
            BackgroundGradient = GradientType.Plain;
            rowCount = 1;
            rows = new TrackerRowSettings[MaximumRows];
            rows[0] = new TrackerRowSettings();
            toolTip = new ToolTip
            {
                AutoPopDelay = 10000,
                InitialDelay = 400,
                ReshowDelay = 100
            };

            var backgroundLabel = new Label
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = true,
                Text = "Background Color:"
            };

            firstColorButton = CreateColorButton();
            secondColorButton = CreateColorButton();
            gradientTypeComboBox = CreateDropDown();
            gradientTypeComboBox.Items.AddRange(new object[] { "Plain", "Vertical", "Horizontal" });
            toolTip.SetToolTip(firstColorButton, "Choose the first background color used by a gradient.");
            toolTip.SetToolTip(secondColorButton, "Choose the row background color, or the second gradient color.");
            toolTip.SetToolTip(gradientTypeComboBox, "Choose a plain background or a vertical/horizontal color gradient.");

            rowComboBoxes = new ComboBox[MaximumRows];
            rowLabels = new Label[MaximumRows];
            rowOptionsButtons = new Button[MaximumRows];
            rowRemoveButtons = new Button[MaximumRows];
            rowUpButtons = new Button[MaximumRows];
            rowDownButtons = new Button[MaximumRows];

            layout = new TableLayoutPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 7,
                Location = new Point(7, 7),
                RowCount = 5,
                Size = new Size(462, 145)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 29f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 29f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 186f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 29f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 29f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < layout.RowCount; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 29f));

            layout.Controls.Add(backgroundLabel, 0, 0);
            layout.Controls.Add(firstColorButton, 1, 0);
            layout.Controls.Add(secondColorButton, 2, 0);
            layout.Controls.Add(gradientTypeComboBox, 3, 0);
            layout.SetColumnSpan(gradientTypeComboBox, 4);

            for (int i = 0; i < MaximumRows; i++)
            {
                int index = i;
                rowLabels[i] = new Label
                {
                    Anchor = AnchorStyles.Left | AnchorStyles.Right,
                    AutoSize = true,
                    Text = "Row " + (i + 1) + ":"
                };
                rowOptionsButtons[i] = CreateRowButton("\u2699");
                rowOptionsButtons[i].Click += (sender, args) => EditRow(index);
                toolTip.SetToolTip(rowOptionsButtons[i], "Edit this row's value format, text alignment, and text color.");
                rowRemoveButtons[i] = CreateRowButton("\u2715");
                rowRemoveButtons[i].ForeColor = Color.Red;
                rowRemoveButtons[i].Click += (sender, args) => RemoveRow(index);
                toolTip.SetToolTip(rowRemoveButtons[i], "Remove this row from the tracker.");
                rowComboBoxes[i] = CreateDropDown();
                rowComboBoxes[i].Items.AddRange(AvailableRowOptions);
                rowComboBoxes[i].SelectedIndexChanged +=
                    (sender, args) => RowSelectionChanged(index);
                toolTip.SetToolTip(rowComboBoxes[i], "Choose the progress category displayed by this row. Duplicate categories are allowed.");
                rowUpButtons[i] = CreateRowButton("\u2191");
                rowUpButtons[i].Click += (sender, args) => MoveRow(index, -1);
                toolTip.SetToolTip(rowUpButtons[i], "Move this row up while keeping all of its row settings.");
                rowDownButtons[i] = CreateRowButton("\u2193");
                rowDownButtons[i].Click += (sender, args) => MoveRow(index, 1);
                toolTip.SetToolTip(rowDownButtons[i], "Move this row down while keeping all of its row settings.");

                int layoutRow = i + 1;
                layout.Controls.Add(rowLabels[i], 0, layoutRow);
                layout.Controls.Add(rowOptionsButtons[i], 1, layoutRow);
                layout.Controls.Add(rowRemoveButtons[i], 2, layoutRow);
                layout.Controls.Add(rowComboBoxes[i], 3, layoutRow);
                layout.Controls.Add(rowUpButtons[i], 4, layoutRow);
                layout.Controls.Add(rowDownButtons[i], 5, layoutRow);
            }

            addRowButton = new Button
            {
                AutoSize = true,
                Text = "Add Row",
                UseVisualStyleBackColor = true
            };
            layout.Controls.Add(addRowButton, 0, 4);
            layout.SetColumnSpan(addRowButton, 7);
            toolTip.SetToolTip(addRowButton, "Add another tracker row, up to a maximum of three.");

            AutoScaleDimensions = new SizeF(6f, 13f);
            AutoScaleMode = AutoScaleMode.Font;
            Size = new Size(476, 94);
            Controls.Add(layout);

            firstColorButton.Click += ColorButtonClick;
            secondColorButton.Click += ColorButtonClick;
            gradientTypeComboBox.SelectedIndexChanged += GradientTypeChanged;
            addRowButton.Click += AddRowClick;

            firstColorButton.DataBindings.Add(
                "BackColor",
                this,
                nameof(BackgroundColor),
                false,
                DataSourceUpdateMode.OnPropertyChanged);
            gradientTypeComboBox.DataBindings.Add(
                "SelectedItem",
                this,
                nameof(GradientString),
                false,
                DataSourceUpdateMode.OnPropertyChanged);

            gradientTypeComboBox.SelectedItem = GradientString;
            UpdateSecondColorBinding();
            RefreshRowControls();
        }

        public TrackerRowSettings GetRowSettings(int index)
        {
            if (index < 0 || index >= rowCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            return rows[index];
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                toolTip.Dispose();
            base.Dispose(disposing);
        }

        public void SetSettings(XmlNode node)
        {
            var element = node as XmlElement;
            if (element == null)
                return;

            BackgroundColor = SettingsHelper.ParseColor(
                element["BackgroundColor"],
                DefaultBackgroundColor);
            BackgroundColor2 = SettingsHelper.ParseColor(
                element["BackgroundColor2"],
                DefaultBackgroundColor);
            BackgroundGradient = SettingsHelper.ParseEnum(
                element["BackgroundGradient"],
                GradientType.Plain);

            TrackerTextCentering legacyCentering = SettingsHelper.ParseEnum(
                element["TextCentering"],
                TrackerTextCentering.Center);
            int parsedRowCount = SettingsHelper.ParseInt(element["RowCount"], 1);
            rowCount = Math.Max(1, Math.Min(MaximumRows, parsedRowCount));

            for (int i = 0; i < MaximumRows; i++)
            {
                rows[i] = null;
                if (i >= rowCount)
                    continue;

                string prefix = "Row" + (i + 1);
                var row = new TrackerRowSettings
                {
                    TextCentering = SettingsHelper.ParseEnum(
                        element[prefix + "TextCentering"],
                        legacyCentering),
                    TextColor = SettingsHelper.ParseColor(
                        element[prefix + "TextColor"],
                        Color.White)
                };

                if (element[prefix + "Category"] != null)
                {
                    row.Category = SettingsHelper.ParseEnum(
                        element[prefix + "Category"],
                        TrackerRowCategory.BlueprintsAndDatabanks);
                    row.DisplayValue = SettingsHelper.ParseEnum(
                        element[prefix + "DisplayValue"],
                        TrackerDisplayValue.Number);
                }
                else
                {
                    ApplyLegacySelection(
                        row,
                        SettingsHelper.ParseString(
                            element[prefix],
                            i == 0 ? "Blueprints & Databanks" : "Completion"));
                }

                rows[i] = row;
            }

            RefreshBindings();
            RefreshRowControls();
            OnSettingsChanged();
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            XmlElement settings = document.CreateElement("Settings");
            SettingsHelper.CreateSetting(document, settings, "Version", "2.0");
            SettingsHelper.CreateSetting(document, settings, "BackgroundColor", BackgroundColor);
            SettingsHelper.CreateSetting(document, settings, "BackgroundColor2", BackgroundColor2);
            SettingsHelper.CreateSetting(document, settings, "BackgroundGradient", BackgroundGradient);
            SettingsHelper.CreateSetting(document, settings, "RowCount", rowCount);

            for (int i = 0; i < rowCount; i++)
            {
                string prefix = "Row" + (i + 1);
                SettingsHelper.CreateSetting(document, settings, prefix + "Category", rows[i].Category);
                SettingsHelper.CreateSetting(document, settings, prefix + "DisplayValue", rows[i].DisplayValue);
                SettingsHelper.CreateSetting(document, settings, prefix + "TextCentering", rows[i].TextCentering);
                SettingsHelper.CreateSetting(document, settings, prefix + "TextColor", rows[i].TextColor);
            }

            return settings;
        }

        public int GetSettingsHashCode()
        {
            unchecked
            {
                int hash = BackgroundColor.ToArgb();
                hash = (hash * 397) ^ BackgroundColor2.ToArgb();
                hash = (hash * 397) ^ (int)BackgroundGradient;
                hash = (hash * 397) ^ rowCount;
                for (int i = 0; i < rowCount; i++)
                {
                    hash = (hash * 397) ^ (int)rows[i].Category;
                    hash = (hash * 397) ^ (int)rows[i].DisplayValue;
                    hash = (hash * 397) ^ (int)rows[i].TextCentering;
                    hash = (hash * 397) ^ rows[i].TextColor.ToArgb();
                }
                return hash;
            }
        }

        public static string GetCategoryName(TrackerRowCategory category)
        {
            switch (category)
            {
                case TrackerRowCategory.Completion:
                    return "Completion";
                case TrackerRowCategory.BlueprintsAndDatabanks:
                    return "Blueprints & Databanks";
                case TrackerRowCategory.Blueprints:
                    return "Blueprints";
                case TrackerRowCategory.Databanks:
                    return "Databanks";
                case TrackerRowCategory.Achievements:
                    return "Achievements";
                default:
                    return "Completion";
            }
        }

        private static TrackerRowCategory ParseCategoryName(string value)
        {
            switch (value)
            {
                case "Blueprints & Databanks":
                    return TrackerRowCategory.BlueprintsAndDatabanks;
                case "Blueprints":
                    return TrackerRowCategory.Blueprints;
                case "Databanks":
                    return TrackerRowCategory.Databanks;
                case "Achievements":
                    return TrackerRowCategory.Achievements;
                default:
                    return TrackerRowCategory.Completion;
            }
        }

        private static void ApplyLegacySelection(TrackerRowSettings row, string selection)
        {
            if (string.Equals(selection, "% Completion", StringComparison.Ordinal))
            {
                row.Category = TrackerRowCategory.Completion;
                row.DisplayValue = TrackerDisplayValue.Percentage;
                return;
            }

            row.Category = ParseCategoryName(selection);
            row.DisplayValue = TrackerDisplayValue.Number;
        }

        private static Button CreateColorButton()
        {
            return new Button
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                FlatStyle = FlatStyle.Popup,
                Margin = new Padding(3),
                Size = new Size(23, 23),
                UseVisualStyleBackColor = false
            };
        }

        private static ComboBox CreateDropDown()
        {
            return new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        private static Button CreateRowButton(string text)
        {
            return new Button
            {
                Anchor = AnchorStyles.None,
                Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Regular, GraphicsUnit.Point),
                Margin = new Padding(1, 3, 2, 3),
                Size = new Size(26, 23),
                Text = text,
                UseVisualStyleBackColor = true
            };
        }

        private void ColorButtonClick(object sender, EventArgs e)
        {
            SettingsHelper.ColorButtonClick((Button)sender, this);
            OnSettingsChanged();
        }

        private void GradientTypeChanged(object sender, EventArgs e)
        {
            if (gradientTypeComboBox.SelectedItem == null)
                return;

            GradientString = gradientTypeComboBox.SelectedItem.ToString();
            UpdateSecondColorBinding();
            OnSettingsChanged();
        }

        private void RowSelectionChanged(int index)
        {
            if (refreshingRows || index >= rowCount || rowComboBoxes[index].SelectedItem == null)
                return;

            TrackerRowCategory category = ParseCategoryName(
                rowComboBoxes[index].SelectedItem.ToString());
            if (rows[index].Category != category)
            {
                rows[index].Category = category;
                rows[index].DisplayValue = TrackerRowSettings.GetDefaultDisplayValue(category);
            }
            OnSettingsChanged();
        }

        private void AddRowClick(object sender, EventArgs e)
        {
            if (rowCount >= MaximumRows)
                return;

            rows[rowCount] = new TrackerRowSettings
            {
                Category = TrackerRowCategory.Completion,
                DisplayValue = TrackerDisplayValue.Percentage
            };
            rowCount++;
            RefreshRowControls();
            OnSettingsChanged();
        }

        private void RemoveRow(int index)
        {
            if (rowCount <= 1 || index < 0 || index >= rowCount)
                return;

            for (int i = index; i < rowCount - 1; i++)
                rows[i] = rows[i + 1];

            rowCount--;
            rows[rowCount] = null;
            RefreshRowControls();
            OnSettingsChanged();
        }

        private void MoveRow(int index, int direction)
        {
            int destination = index + direction;
            if (index < 0 || index >= rowCount || destination < 0 || destination >= rowCount)
                return;

            TrackerRowSettings moved = rows[index];
            rows[index] = rows[destination];
            rows[destination] = moved;
            RefreshRowControls();
            OnSettingsChanged();
        }

        private void EditRow(int index)
        {
            if (index < 0 || index >= rowCount)
                return;

            using (var editor = new RowSettingsEditor(index + 1, rows[index]))
            {
                if (editor.ShowDialog(FindForm()) != DialogResult.OK)
                    return;

                rows[index].DisplayValue = editor.DisplayValue;
                rows[index].TextCentering = editor.TextCentering;
                rows[index].TextColor = editor.TextColor;
            }

            OnSettingsChanged();
        }

        private void RefreshRowControls()
        {
            refreshingRows = true;
            try
            {
                for (int i = 0; i < MaximumRows; i++)
                {
                    bool visible = i < rowCount;
                    rowLabels[i].Visible = visible;
                    rowOptionsButtons[i].Visible = visible;
                    rowRemoveButtons[i].Visible = visible;
                    rowRemoveButtons[i].Enabled = visible && rowCount > 1;
                    rowComboBoxes[i].Visible = visible;
                    rowUpButtons[i].Visible = visible;
                    rowDownButtons[i].Visible = visible;
                    rowUpButtons[i].Enabled = visible && i > 0;
                    rowDownButtons[i].Enabled = visible && i < rowCount - 1;
                    layout.RowStyles[i + 1].Height = visible ? 29f : 0f;

                    if (visible)
                        rowComboBoxes[i].SelectedItem = GetCategoryName(rows[i].Category);
                }
            }
            finally
            {
                refreshingRows = false;
            }

            addRowButton.Enabled = rowCount < MaximumRows;
            int visibleHeight = 29 * (rowCount + 2);
            layout.Height = visibleHeight;
            Height = visibleHeight + 14;
        }

        private void UpdateSecondColorBinding()
        {
            firstColorButton.Visible = BackgroundGradient != GradientType.Plain;
            secondColorButton.DataBindings.Clear();
            secondColorButton.DataBindings.Add(
                "BackColor",
                this,
                BackgroundGradient == GradientType.Plain
                    ? nameof(BackgroundColor)
                    : nameof(BackgroundColor2),
                false,
                DataSourceUpdateMode.OnPropertyChanged);
        }

        private void RefreshBindings()
        {
            foreach (Binding binding in firstColorButton.DataBindings)
                binding.ReadValue();
            foreach (Binding binding in gradientTypeComboBox.DataBindings)
                binding.ReadValue();

            gradientTypeComboBox.SelectedItem = GradientString;
            UpdateSecondColorBinding();
            foreach (Binding binding in secondColorButton.DataBindings)
                binding.ReadValue();
        }

        private void OnSettingsChanged()
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
