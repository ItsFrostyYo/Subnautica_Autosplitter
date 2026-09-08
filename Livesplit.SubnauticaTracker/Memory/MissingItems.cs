namespace LiveSplit.SubnauticaTracker.Memory
{
    internal sealed class MissingItems
    {
        public MissingItems(string saveSlot, string[] blueprints, string[] databanks, string[] achievements)
        {
            SaveSlot = saveSlot ?? string.Empty;
            Blueprints = blueprints;
            Databanks = databanks;
            Achievements = achievements;
        }

        public string SaveSlot { get; }
        public string[] Blueprints { get; }
        public string[] Databanks { get; }
        public string[] Achievements { get; }
    }
}
