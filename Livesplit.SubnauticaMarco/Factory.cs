using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;
using System.Reflection;
using UpdateManager;

namespace LiveSplit.SubnauticaMarco
{
    internal sealed class Factory : IComponentFactory, IUpdateable
    {
        public string ComponentName => "Subnautica Reset Macro";
        public string Description => "Memory-synchronized reset macros for Subnautica.";
        public ComponentCategory Category => ComponentCategory.Control;
        public string UpdateName => ComponentName;
        public string UpdateURL => "https://raw.githubusercontent.com/ItsFrostyYo/Subnautica_Autosplitter/LiveSplit.Subnautica/";
        public string XMLURL => UpdateURL + "Components/SubnauticaMarco.Updates.xml";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public IComponent Create(LiveSplitState state) => new Component(state);
    }
}
