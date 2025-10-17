using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.VoxSplitter {
    public abstract class Component : LogicComponent {
        
        protected enum EGameTime {
            None,
            Loading,
            GameTime
        }

        protected Settings settings;
        protected TimerModel timer;
        protected Memory memory;

        protected Logger logger;

        protected virtual SettingInfo? Start => new SettingInfo(1, null);
        protected virtual SettingInfo? Reset => new SettingInfo(1, null);
        protected virtual OptionsInfo? Options => null;
        protected virtual EGameTime GameTime => EGameTime.None;

        public Component(LiveSplitState state) {
            logger = new Logger();
            logger.StartLogger();
      
            timer = new TimerModel { CurrentState = state };
            timer.CurrentState.OnStart += OnStart;
            timer.CurrentState.OnSplit += OnSplit;
            timer.CurrentState.OnReset += OnReset;

            if(GameTime != EGameTime.None) {
                timer.InitializeGameTime();

                if(state.CurrentTimingMethod == TimingMethod.RealTime) {
                    string gameName = Factory.ExAssembly.Description().Substring(17);
                    DialogResult result = MessageBox.Show(
                        String.Concat(gameName, " uses Game Time as the main timing method.", Environment.NewLine,
                                      "LiveSplit is currently comparing against Real Time.", Environment.NewLine,
                                      "Would you like to set the timing method to Game Time?"),
                        gameName + " Timing Method",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if(result == DialogResult.Yes) {
                        state.CurrentTimingMethod = TimingMethod.GameTime;
                    }
                }
            }
        }

        public override string ComponentName => Factory.ExAssembly.FullComponentName();
        public override Control GetSettingsControl(LayoutMode mode) => settings;
        public override XmlNode GetSettings(XmlDocument document) => settings.GetSettings(document);
        public override void SetSettings(XmlNode settings) => this.settings.SetSettings(settings);
        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) {
            if(!memory.IsReady()) {
                return;
            }

            memory.IncreaseTick();
            if(!memory.Update()) {
                return;
            }

            if(GameTime == EGameTime.Loading) {
                timer.CurrentState.IsGameTimePaused = memory.Loading();
            } else if(GameTime == EGameTime.GameTime) {
                timer.CurrentState.SetGameTime(memory.GameTime());
            }

            if(timer.CurrentState.CurrentSplitIndex < 0) {
                if(settings.Start != 0 && memory.Start(settings.Start)) {
                    timer.Start();
                    logger.Log("Start");
                }
            } else {
                if(settings.Reset != 0 && memory.Reset(settings.Reset)) {
                    timer.Reset();
                    logger.Log("Reset");
                } else if(memory.Split()) {
                    timer.Split();
                    logger.Log("Split");
                }
            }
        }

        protected virtual void OnStart(object sender, EventArgs e) {
            timer.CurrentState.SetGameTime(TimeSpan.Zero);
            if(GameTime == EGameTime.GameTime) {
                timer.CurrentState.IsGameTimePaused = true;
            }
            memory.OnStart(settings.Splits);
        }
        protected virtual void OnSplit(object sender, EventArgs e) => memory.OnSplit();
        protected virtual void OnReset(object sender, TimerPhase e) => memory.OnReset();

        public override void Dispose() {
            logger.Log("Dispose");
            timer.CurrentState.OnStart -= OnStart;
            timer.CurrentState.OnSplit -= OnSplit;
            timer.CurrentState.OnReset -= OnReset;
            memory.Dispose();
            logger.StopLogger();
        }

        public static IEnumerable<A> GetEnumAttributes<E, A>() where E : Enum where A : Attribute {
            return typeof(E).GetMembers().SelectMany(m => m.GetCustomAttributes(typeof(A), false)).Cast<A>();
        }

        public static string[] GetEnumDescriptions<E>() where E : Enum {
            return GetEnumAttributes<E, DescriptionAttribute>().Select(a => a.Description).ToArray();
        }

        public static Type[] GetEnumTypes<E>() where E : Enum {
            return GetEnumAttributes<E, TypeAttribute>().Select(a => a.Type).ToArray();
        }

        public static Control[] CreateControlsFromEnum<E>() where E : Enum {
            string[] names = Enum.GetNames(typeof(E));
            string[] descs = GetEnumDescriptions<E>();
            Type[] types = GetEnumTypes<E>();
            Control[] controls = new Control[types.Length];
            for(int i = 0; i < types.Length; i++) {
                Control control = (Control)Activator.CreateInstance(Type.GetType(types[i].ToString()), new object[] { });
                control.Name = names[i];
                control.Text = descs[i];
                controls[i] = control;
            }
            return controls;
        }
    }

    public struct SettingInfo {
        public int def;
        public string[] values;

        public SettingInfo(int def, string[] values) {
            this.def = def;
            this.values = values;
        }
    }

    public struct OptionsInfo {
        public string[] def;
        public Control[] values;

        public OptionsInfo(string[] def, Control[] values) {
            this.def = def;
            this.values = values;
        }
    }

    public class TypeAttribute : Attribute {
        public Type Type { get; }
        public TypeAttribute(Type type) {
            Type = type;
        }
    }
}