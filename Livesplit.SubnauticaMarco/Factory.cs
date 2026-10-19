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
        public string Description => "Fully Configurable Subnautica Macro for Faster and Automated Resetting for Speedrunning. Supports Vanilla Subnautica UI for 2018, 2021, 2023 and 2025 Versions, Allows all 4 Normal Base Gamemodes, also has support for Subnautica Speedrunning Mod's BetterRNG Specific Custom Gamemodes UI, and its Hardcore Modes Custom Quit Button. The Reset Macro also allows for Explosion Time Resetting that runs either Until it finds a Suitable Explosion Time within the Set Range or you Abort using the Reset Button Again.";
        public ComponentCategory Category => ComponentCategory.Control;
        public string UpdateName => ComponentName;
        public string UpdateURL => "https://raw.githubusercontent.com/ItsFrostyYo/Subnautica_Autosplitter/LiveSplit.Subnautica/";
        public string XMLURL => UpdateURL + "Components/SubnauticaMarco.Updates.xml";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public IComponent Create(LiveSplitState state) => new Component(state);
    }
}
