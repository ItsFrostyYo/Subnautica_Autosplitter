using LiveSplit.Model;
using LiveSplit.Options;
using LiveSplit.UI;
using LiveSplit.UI.Components.AutoSplit;
using LiveSplit.VoxSplitter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace Livesplit.Subnautica
{
    public class SubnauticaComponent : LiveSplit.VoxSplitter.Component
    {
        public SubnauticaComponent(LiveSplitState state) : base(state)
        {
            settings = new SubnauticaSettings(state);
            memory = new SubnauticaMemory(state, logger, settings);           
        }
    }
}
