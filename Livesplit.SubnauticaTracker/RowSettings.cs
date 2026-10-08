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
        Achievements,
        DamageTaken
    }

    public enum TrackerDisplayValue
    {
        Number,
        Percentage,
        DamageTaken,
        HitsTaken
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
        public bool ExcludeIntroDamage { get; set; }

        public static TrackerDisplayValue GetDefaultDisplayValue(TrackerRowCategory category)
        {
            if (category == TrackerRowCategory.Completion)
                return TrackerDisplayValue.Percentage;
            if (category == TrackerRowCategory.DamageTaken)
                return TrackerDisplayValue.DamageTaken;
            return TrackerDisplayValue.Number;
        }

        public void ResetToDefaults(TrackerRowCategory category)
        {
            Category = category;
            DisplayValue = GetDefaultDisplayValue(category);
            TextCentering = TrackerTextCentering.Center;
            TextColor = Color.White;
            ExcludeIntroDamage = true;
        }

        public TrackerRowSettings Clone()
        {
            return new TrackerRowSettings
            {
                Category = Category,
                DisplayValue = DisplayValue,
                TextCentering = TextCentering,
                TextColor = TextColor,
                ExcludeIntroDamage = ExcludeIntroDamage
            };
        }
    }
}
