namespace LiveSplit.SubnauticaMarco.Configuration
{
    public enum ResetGameMode
    {
        Survival,
        Hardcore,
        Creative,
        Freedom,
        BetterRngSurvival,
        BetterRngHardcore,
        BetterRngCreative
    }

    public enum ExplosionPreset
    {
        Minutes46To46_30,
        Minutes46To47,
        Minutes46To48,
        Minutes46To50,
        UnderOneHour,
        OverOneHour,
        Custom
    }

    internal sealed class MacroConfiguration
    {
        public bool Enabled { get; set; }
        public int Hotkey { get; set; }
        public ResetGameMode GameMode { get; set; }
        public bool ExplosionReset { get; set; }
        public ExplosionPreset ExplosionPreset { get; set; }
        public int CustomMinimumSeconds { get; set; }
        public int CustomMaximumSeconds { get; set; }
    }
}
