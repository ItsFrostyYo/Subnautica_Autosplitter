using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;
using UpdateManager;

namespace LiveSplit.SubnauticaTracker
{
    internal sealed class Factory : IComponentFactory, IUpdateable
    {
        public string ComponentName => "Subnautica Tracker";
        public string Description => "Configurable Subnautica Progression Tracker for Speedrunning.";
        public ComponentCategory Category => ComponentCategory.Information;
        public string UpdateName => ComponentName;
        public string XMLURL => string.Empty;
        public string UpdateURL => string.Empty;
        public Version Version => new Version(1, 4, 2, 0);

        public IComponent Create(LiveSplitState state) => new Component(state);
    }
}
