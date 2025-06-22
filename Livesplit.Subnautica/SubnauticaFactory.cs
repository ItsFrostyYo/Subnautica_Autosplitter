using Livesplit.Subnautica;
using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Livesplit.Subnautica
{
    public class SubnauticaFactory : IComponentFactory
    {
        public string ComponentName => "Subnautica Autosplitter";

        public string Description => "Autosplitter for Subnautica";

        public ComponentCategory Category => ComponentCategory.Control;

        public string UpdateName => ComponentName;

        public string XMLURL => UpdateURL + "Subnautica.Updates.xml";

        public string UpdateURL => "https://raw.githubusercontent.com/Sprinter31/Subnautica_Autosplitter/Livesplit.Subnautica/";

        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public IComponent Create(LiveSplitState state) => new SubnauticaComponent(state);
    }
}
