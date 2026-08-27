namespace LiveSplit.SubnauticaTracker.Tracking
{
    internal enum TrackerState
    {
        WaitingForGame,
        Initializing,
        MainMenu,
        Tracking,
        Error
    }

    internal sealed class TrackerCount
    {
        public static readonly TrackerCount Unknown = new TrackerCount(false, 0, 0);

        public TrackerCount(bool available, int unlocked, int total)
        {
            Available = available;
            Unlocked = unlocked;
            Total = total;
        }

        public bool Available { get; }
        public int Unlocked { get; }
        public int Total { get; }
    }

    internal sealed class TrackerSnapshot
    {
        public static readonly TrackerSnapshot Waiting = new TrackerSnapshot(
            TrackerState.WaitingForGame,
            string.Empty,
            string.Empty,
            TrackerCount.Unknown,
            TrackerCount.Unknown,
            TrackerCount.Unknown);

        public TrackerSnapshot(
            TrackerState state,
            string version,
            string saveSlot,
            TrackerCount blueprints,
            TrackerCount databanks,
            TrackerCount achievements)
        {
            State = state;
            Version = version ?? string.Empty;
            SaveSlot = saveSlot ?? string.Empty;
            Blueprints = blueprints ?? TrackerCount.Unknown;
            Databanks = databanks ?? TrackerCount.Unknown;
            Achievements = achievements ?? TrackerCount.Unknown;
        }

        public TrackerState State { get; }
        public string Version { get; }
        public string SaveSlot { get; }
        public TrackerCount Blueprints { get; }
        public TrackerCount Databanks { get; }
        public TrackerCount Achievements { get; }
    }
}
