using LiveSplit.Model;
using LiveSplit.SubnauticaMarco.Macros;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.SubnauticaMarco
{
    public sealed class Component : LogicComponent
    {
        private readonly MacroController controller;

        public Settings Settings { get; }

        public override string ComponentName => "Subnautica Reset Macro";

        public Component(LiveSplitState state)
        {
            Settings = new Settings();
            controller = new MacroController(state.Form.Handle);
            Settings.HotkeyCaptureChanged += SettingsHotkeyCaptureChanged;
        }

        public override void Dispose()
        {
            Settings.HotkeyCaptureChanged -= SettingsHotkeyCaptureChanged;
            controller.Dispose();
            Settings.Dispose();
        }

        public override XmlNode GetSettings(XmlDocument document) => Settings.GetSettings(document);

        public override Control GetSettingsControl(LayoutMode mode) => Settings;

        public override void SetSettings(XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public override void Update(
            IInvalidator invalidator,
            LiveSplitState state,
            float width,
            float height,
            LayoutMode mode)
        {
            controller.Update(Settings.GetConfiguration());
        }

        private void SettingsHotkeyCaptureChanged(object sender, EventArgs args)
        {
            controller.SetHotkeyCaptureActive(Settings.IsCapturingHotkey);
        }
    }
}
