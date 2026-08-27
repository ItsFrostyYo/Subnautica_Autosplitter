using LiveSplit.Model;
using LiveSplit.SubnauticaTracker.Tracking;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.SubnauticaTracker
{
    public sealed class Component : IComponent, IDisposable
    {
        private const float DefaultRowHeight = 23f;
        private const float MinimumRowHeight = 21f;
        private const float DefaultHorizontalWidth = 220f;

        private readonly SimpleLabel[] rowLabels;
        private readonly GraphicsCache cache;
        private readonly TrackerService tracker;

        public Settings Settings { get; }

        public string ComponentName => "Subnautica Tracker";
        public float HorizontalWidth => DefaultHorizontalWidth;
        public float MinimumHeight => MinimumRowHeight * Settings.RowCount;
        public float VerticalHeight => DefaultRowHeight * Settings.RowCount;
        public float MinimumWidth => 20f;
        public float PaddingTop => 0f;
        public float PaddingBottom => 0f;
        public float PaddingLeft => 7f;
        public float PaddingRight => 7f;
        public IDictionary<string, Action> ContextMenuControls => null;

        public Component(LiveSplitState state)
        {
            Settings = new Settings();
            rowLabels = new SimpleLabel[Settings.MaximumRows];
            for (int i = 0; i < rowLabels.Length; i++)
            {
                rowLabels[i] = new SimpleLabel
                {
                    HorizontalAlignment = StringAlignment.Center,
                    VerticalAlignment = StringAlignment.Center
                };
            }

            cache = new GraphicsCache();
            tracker = new TrackerService();
        }

        public void Dispose()
        {
            tracker.Dispose();
            Settings.Dispose();
        }

        public void DrawHorizontal(Graphics graphics, LiveSplitState state, float height, Region clipRegion)
        {
            DrawGeneral(graphics, state, HorizontalWidth, height);
        }

        public void DrawVertical(Graphics graphics, LiveSplitState state, float width, Region clipRegion)
        {
            DrawGeneral(graphics, state, width, VerticalHeight);
        }

        public XmlNode GetSettings(XmlDocument document) => Settings.GetSettings(document);

        public Control GetSettingsControl(LayoutMode mode) => Settings;

        public void SetSettings(XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public void Update(
            IInvalidator invalidator,
            LiveSplitState state,
            float width,
            float height,
            LayoutMode mode)
        {
            cache.Restart();
            cache["Settings"] = Settings.GetSettingsHashCode();
            cache["TextFont"] = state.LayoutSettings.TextFont;
            cache["DropShadows"] = state.LayoutSettings.DropShadows;

            TrackerSnapshot snapshot = tracker.Snapshot;
            cache["TrackerState"] = (int)snapshot.State;
            cache["SaveSlot"] = snapshot.SaveSlot;
            cache["Version"] = snapshot.Version;
            cache["Blueprints"] = CountHash(snapshot.Blueprints);
            cache["Databanks"] = CountHash(snapshot.Databanks);
            cache["Achievements"] = CountHash(snapshot.Achievements);

            if (invalidator != null && cache.HasChanged)
                invalidator.Invalidate(0f, 0f, width, height);
        }

        private void DrawGeneral(Graphics graphics, LiveSplitState state, float width, float height)
        {
            if (width <= 0f || height <= 0f)
                return;

            DrawBackground(graphics, width, height);

            int count = Settings.RowCount;
            float rowHeight = height / count;
            TrackerSnapshot snapshot = tracker.Snapshot;

            for (int i = 0; i < count; i++)
            {
                TrackerRowSettings rowSettings = Settings.GetRowSettings(i);
                SimpleLabel label = rowLabels[i];
                label.Text = FormatRowText(
                    i,
                    rowSettings,
                    snapshot,
                    graphics,
                    state.LayoutSettings.TextFont,
                    Math.Max(0f, width - PaddingLeft - PaddingRight));
                label.Font = state.LayoutSettings.TextFont;
                label.ForeColor = rowSettings.TextColor;
                label.OutlineColor = state.LayoutSettings.TextOutlineColor;
                label.ShadowColor = state.LayoutSettings.ShadowsColor;
                label.HasShadow = state.LayoutSettings.DropShadows;
                label.HorizontalAlignment = GetTextAlignment(rowSettings.TextCentering);
                label.VerticalAlignment = StringAlignment.Center;
                label.X = PaddingLeft;
                label.Y = i * rowHeight;
                label.Width = Math.Max(0f, width - PaddingLeft - PaddingRight);
                label.Height = rowHeight;
                label.Draw(graphics);
            }
        }

        private static int CountHash(TrackerCount count)
        {
            unchecked
            {
                int hash = count.Available ? 1 : 0;
                hash = (hash * 397) ^ count.Unlocked;
                return (hash * 397) ^ count.Total;
            }
        }

        private static string FormatRowText(
            int rowIndex,
            TrackerRowSettings rowSettings,
            TrackerSnapshot snapshot,
            Graphics graphics,
            Font font,
            float availableWidth)
        {
            if (snapshot.State == TrackerState.WaitingForGame)
                return rowIndex == 0 ? "No Process" : string.Empty;

            if (snapshot.State == TrackerState.Initializing)
            {
                return rowIndex == 0
                    ? FormatStatus(snapshot.Version, "Initializing")
                    : string.Empty;
            }

            if (snapshot.State == TrackerState.MainMenu)
            {
                return rowIndex == 0
                    ? FormatStatus(snapshot.Version, "No Save")
                    : string.Empty;
            }

            if (snapshot.State == TrackerState.Error)
            {
                return rowIndex == 0
                    ? FormatStatus(snapshot.Version, "Tracker Error")
                    : string.Empty;
            }

            if (rowSettings.Category == TrackerRowCategory.BlueprintsAndDatabanks)
            {
                string combinedFull;
                string combinedCompact;
                if (rowSettings.DisplayValue == TrackerDisplayValue.Percentage)
                {
                    combinedFull = FormatPercentage("Blueprints", snapshot.Blueprints)
                        + " | "
                        + FormatPercentage("Databanks", snapshot.Databanks);
                    combinedCompact = FormatPercentage("BP's", snapshot.Blueprints)
                        + " | "
                        + FormatPercentage("DB's", snapshot.Databanks);
                }
                else
                {
                    combinedFull = FormatCount("Blueprints", snapshot.Blueprints)
                        + " | "
                        + FormatCount("Databanks", snapshot.Databanks);
                    combinedCompact = FormatCount("BP's", snapshot.Blueprints)
                        + " | "
                        + FormatCount("DB's", snapshot.Databanks);
                }

                if (graphics == null || font == null || graphics.MeasureString(combinedFull, font).Width <= availableWidth)
                    return combinedFull;
                return combinedCompact;
            }

            TrackerCount count;
            string fullName;
            string compactName;
            switch (rowSettings.Category)
            {
                case TrackerRowCategory.Completion:
                    count = CombineCounts(
                        snapshot.Blueprints,
                        snapshot.Databanks,
                        snapshot.Achievements);
                    fullName = "Completion";
                    compactName = "C";
                    break;

                case TrackerRowCategory.Blueprints:
                    count = snapshot.Blueprints;
                    fullName = "Blueprints";
                    compactName = "BP's";
                    break;

                case TrackerRowCategory.Databanks:
                    count = snapshot.Databanks;
                    fullName = "Databanks";
                    compactName = "DB's";
                    break;

                default:
                    count = snapshot.Achievements;
                    fullName = "Achievements";
                    compactName = "A's";
                    break;
            }

            string full;
            string compact;
            if (rowSettings.DisplayValue == TrackerDisplayValue.Percentage)
            {
                int percentage = GetPercentage(count);
                string value = percentage >= 0 ? percentage + "%" : "-%";
                bool valueFirst = rowSettings.Category == TrackerRowCategory.Completion;
                full = valueFirst ? value + " " + fullName : fullName + " " + value;
                compact = valueFirst ? value + " " + compactName : compactName + " " + value;
            }
            else
            {
                full = FormatCount(fullName, count);
                compact = FormatCount(compactName, count);
            }

            if (graphics == null || font == null || graphics.MeasureString(full, font).Width <= availableWidth)
                return full;
            return compact;
        }

        private static string FormatStatus(string version, string status)
        {
            return string.IsNullOrWhiteSpace(version)
                ? status
                : "Subnautica " + version + " - " + status;
        }

        private static TrackerCount CombineCounts(params TrackerCount[] groups)
        {
            int unlocked = 0;
            int total = 0;
            foreach (TrackerCount group in groups)
            {
                if (!group.Available || group.Total <= 0)
                    return TrackerCount.Unknown;

                unlocked += Math.Max(0, Math.Min(group.Unlocked, group.Total));
                total += group.Total;
            }

            return total > 0
                ? new TrackerCount(true, unlocked, total)
                : TrackerCount.Unknown;
        }

        private static int GetPercentage(TrackerCount count)
        {
            if (!count.Available || count.Total <= 0)
                return -1;

            int unlocked = Math.Max(0, Math.Min(count.Unlocked, count.Total));
            return unlocked >= count.Total
                ? 100
                : (int)Math.Floor(unlocked * 100d / count.Total);
        }

        private static string FormatCount(string name, TrackerCount count)
        {
            return count.Available
                ? $"{name} {count.Unlocked}/{count.Total}"
                : $"{name} -/-";
        }

        private static string FormatPercentage(string name, TrackerCount count)
        {
            int percentage = GetPercentage(count);
            return name + " " + (percentage >= 0 ? percentage + "%" : "-%");
        }

        private static StringAlignment GetTextAlignment(TrackerTextCentering centering)
        {
            switch (centering)
            {
                case TrackerTextCentering.Left:
                    return StringAlignment.Near;
                case TrackerTextCentering.Right:
                    return StringAlignment.Far;
                default:
                    return StringAlignment.Center;
            }
        }

        private void DrawBackground(Graphics graphics, float width, float height)
        {
            if (Settings.BackgroundColor.A == 0
                && (Settings.BackgroundGradient == GradientType.Plain
                    || Settings.BackgroundColor2.A == 0))
            {
                return;
            }

            PointF endPoint = Settings.BackgroundGradient == GradientType.Horizontal
                ? new PointF(width, 0f)
                : new PointF(0f, height);
            Color endColor = Settings.BackgroundGradient == GradientType.Plain
                ? Settings.BackgroundColor
                : Settings.BackgroundColor2;

            using (var brush = new LinearGradientBrush(
                new PointF(0f, 0f),
                endPoint,
                Settings.BackgroundColor,
                endColor))
            {
                graphics.FillRectangle(brush, 0f, 0f, width, height);
            }
        }
    }
}
