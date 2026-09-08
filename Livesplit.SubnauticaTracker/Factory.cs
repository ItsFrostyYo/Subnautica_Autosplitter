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
        public string UpdateURL => "https://raw.githubusercontent.com/ItsFrostyYo/Subnautica_Autosplitter/LiveSplit.Subnautica/";
        public string XMLURL => UpdateURL + "Components/SubnauticaTracker.Updates.xml";
        public Version Version => new Version(1, 6, 3, 0);

        public IComponent Create(LiveSplitState state) => new Component(state);
    }
}
