using LiveSplit.Model;
using LiveSplit.Options;
using LiveSplit.UI;
using LiveSplit.UI.Components.AutoSplit;
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
using static System.Windows.Forms.AxHost;

namespace Livesplit.Subnautica
{
    public class SubnauticaComponent : AutoSplitComponent
    {
        private static SubnauticaSettings settings = new SubnauticaSettings();
        static SubnauticaSplitter splitter = new SubnauticaSplitter(settings);
        private LiveSplitState _state;
        internal SubnauticaComponent(LiveSplitState state) : base(splitter, state)
        {
            state.OnReset += OnReset;
            settings.SetState(state);
            _state = state;            
        }       

        public override string ComponentName => "Subnautica Autosplitter";

        public override void Dispose()
        {
        }
        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            splitter.Update();
            settings.UpdateExploBtnContent();

            TryResetOnMainMenu();
            splitter.isInMainMenuOld = splitter.isInMainMenu;
            base.Update(invalidator, state, width, height, mode);
        }

        public void OnReset(object sender, TimerPhase t) => splitter.OnReset(t);
        private void TryResetOnMainMenu()
        {
            if (!settings.reset)
                return;
            if (!(splitter.isInMainMenu && !splitter.isInMainMenuOld))
                return;
            if (_state.CurrentPhase == TimerPhase.NotRunning)
                return;
            
            Form ui = _state.Form;
            Action doReset = () =>
            {
                bool save = true;
                bool warnOnReset = settings.askForGoldSave;
                if (warnOnReset && _state.Run.HasChanged)
                {
                    DialogResult r = MessageBox.Show(
                        ui,
                        "Save splits before resetting?",
                        "Reset",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (r == DialogResult.Cancel)
                        return;

                    save = (r == DialogResult.Yes);
                }

                Model.Reset(save);
            };

            if (ui.InvokeRequired)
                ui.BeginInvoke(doReset);
            else
                doReset();
        }
        public override XmlNode GetSettings(XmlDocument document) { return settings.UpdateSettings(document); }
        public override void SetSettings(XmlNode document) { settings.SetSettings(document); }
        public override Control GetSettingsControl(LayoutMode mode) { return settings; }

        private void WriteDebug(string message)
        {
            Debug.WriteLine($"[Subnautica Component] {message}");
        }
    }
}
