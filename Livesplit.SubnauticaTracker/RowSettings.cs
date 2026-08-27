using System.Drawing;

namespace LiveSplit.SubnauticaTracker
{
    public enum TrackerTextCentering
    {
        Left,
        Right,
        Center
    }

    public enum TrackerRowCategory
    {
        Completion,
        BlueprintsAndDatabanks,
        Blueprints,
        Databanks,
        Achievements
    }

    public enum TrackerDisplayValue
    {
        Number,
        Percentage
    }

    public sealed class TrackerRowSettings
    {
        public TrackerRowSettings()
        {
            ResetToDefaults(TrackerRowCategory.BlueprintsAndDatabanks);
        }

        public TrackerRowCategory Category { get; set; }
        public TrackerDisplayValue DisplayValue { get; set; }
        public TrackerTextCentering TextCentering { get; set; }
        public Color TextColor { get; set; }

        public static TrackerDisplayValue GetDefaultDisplayValue(TrackerRowCategory category)
        {
            return category == TrackerRowCategory.Completion
                ? TrackerDisplayValue.Percentage
                : TrackerDisplayValue.Number;
        }

        public void ResetToDefaults(TrackerRowCategory category)
        {
            Category = category;
            DisplayValue = GetDefaultDisplayValue(category);
            TextCentering = TrackerTextCentering.Center;
            TextColor = Color.White;
        }

        public TrackerRowSettings Clone()
        {
            return new TrackerRowSettings
            {
                Category = Category,
                DisplayValue = DisplayValue,
                TextCentering = TextCentering,
                TextColor = TextColor
            };
        }
    }
}
