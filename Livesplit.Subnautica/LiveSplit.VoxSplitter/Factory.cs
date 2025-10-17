using LiveSplit.Model;
using LiveSplit.NewSuperLuckysTale;
using LiveSplit.UI.Components;
using LiveSplit.VoxSplitter;
using System;
using System.Reflection;

namespace LiveSplit.VoxSplitter {
    public class Factory : IComponentFactory {
        public string UpdateName => ComponentName;
        public string UpdateURL => ExAssembly.GitMainURL();
        public string XMLURL => UpdateURL + "Components/ComponentsUpdate.xml";
        public Version Version => ExAssembly.GetName().Version;
        public string ComponentName => ExAssembly.FullComponentName();
        public string Description => ExAssembly.Description();
        public ComponentCategory Category => ComponentCategory.Control;
        public IComponent Create(LiveSplitState state) => new NewSuperLuckysTaleComponent(state);

        public static Assembly ExAssembly = Assembly.GetExecutingAssembly();
    }
}