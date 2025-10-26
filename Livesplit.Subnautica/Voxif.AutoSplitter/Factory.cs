using Livesplit.Subnautica;
using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;
using System.Reflection;

namespace LiveSplit.Subnautica {
    public class Factory : IComponentFactory {
        public string ComponentName => "Subnautica Autosplitter";

        public string Description => "Autosplitter for Subnautica";

        public ComponentCategory Category => ComponentCategory.Control;

        public string UpdateName => ComponentName;

        public string XMLURL => UpdateURL + "Components/Subnautica.Updates.xml";

        public string UpdateURL => "https://raw.githubusercontent.com/Sprinter31/Subnautica_Autosplitter/LiveSplit.Subnautica-voxif/";

        public Version Version => ExAssembly.GetName().Version;

        public IComponent Create(LiveSplitState state) => new SubnauticaComponent(state);

        public static Assembly ExAssembly = Assembly.GetExecutingAssembly();
    }
}