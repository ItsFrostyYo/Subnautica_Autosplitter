using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.VoxSplitter {

    [TypeDescriptionProvider(typeof(AbstractControlDescriptionProvider<Settings, UserControl>))]
    public abstract partial class Settings : UserControl {

        protected readonly Dictionary<string, HashSet<string>> presetsDict;

        public abstract HashSet<string> Splits { get; }

        protected readonly int defStart, defReset;
        protected readonly string[] defOptions;

        protected const string CustomPreset = "Custom";
        protected string lastPreset = "";

        protected string version = "";

        protected readonly Control startControl;
        public int Start {
            get => GetCheckOrComboValue(startControl);
            protected set => SetCheckOrComboValue(startControl, value);
        }

        protected readonly Control resetControl;
        public int Reset {
            get => GetCheckOrComboValue(resetControl);
            protected set => SetCheckOrComboValue(resetControl, value);
        }

        protected readonly Control optionsControl;
        public string[] Options {
            get {
                if(optionsControl == null) {
                    return new string[0];
                } else if(optionsControl is OptionCheckBox check) {
                    return check.Checked ? new string[] { check.Name } : new string[0];
                } else if(optionsControl is OptionsButton options) {
                    return options.GetOptions();
                } else {
                    return new string[0];
                }
            }
            protected set {
                if(optionsControl == null) {
                    return;
                } else if(optionsControl is OptionCheckBox check) {
                    check.Checked = value.Length != 0;
                } else if(optionsControl is OptionsButton options) {
                    options.SetOptions(value);
                }
            }
        }

        public event OptionEventHandler OptionChanged {
            add => ((IOption)optionsControl).OnChanged += value;
            remove => ((IOption)optionsControl).OnChanged -= value;
        }

        protected int GetCheckOrComboValue(Control control) {
            if(control == null) {
                return 0;
            } else if(control is CheckBox check) {
                return check.Checked ? 1 : 0;
            } else {
                return ((ComboBox)control).SelectedIndex;
            }
        }

        protected void SetCheckOrComboValue(Control control, int value) {
            if(control == null) {
                return;
            } else if(control is CheckBox check) {
                check.Checked = value != 0;
            } else {
                ((ComboBox)control).SelectedIndex = value;
            }
        }

        public void SetGameVersion(string version) {
            this.version = version;
            Form form = FindForm();
            if(form != null) {
                SetFormVersion(form);
            }
        }

        protected void SetFormVersion(Form form) {
            form.Text = Factory.ExAssembly.FullComponentName() + (!String.IsNullOrEmpty(version) ? $" [{version}]" : "");
        }

        public Settings(SettingInfo? start, SettingInfo? reset, OptionsInfo? options) {
            InitializeComponent();

            Dock = DockStyle.Fill;

            startControl = AddCheckOrCombo(TableLayoutPanelStart, start);
            resetControl = AddCheckOrCombo(TableLayoutPanelReset, reset);
            optionsControl = AddOption(TableLayoutPanelOptions, options);

            Start = defStart = start?.def ?? 0;
            Reset = defReset = reset?.def ?? 0;
            Options = defOptions = options?.def ?? new string[0];

            presetsDict = new Dictionary<string, HashSet<string>>();

            XmlDocument presetsXML = new XmlDocument();
            presetsXML.Load(Factory.ExAssembly.GetManifestResourceStream(Factory.ExAssembly.GetName().Name + ".Splits.Presets.xml"));
            SetupPresets(presetsXML.SelectNodes("Presets/Preset"));

            ComboBoxPreset.Items.AddRange(presetsDict.Keys.ToArray());
        }

        protected Control AddCheckOrCombo(TableLayoutPanel parent, SettingInfo? tuple) {
            if(tuple == null) {
                parent.Dispose();
                return null;
            }

            string[] values = tuple?.values;

            Control control;
            if(values == null || values.Length == 1) {
                control = new CheckBox {
                    Text = values?[0] ?? parent.Controls[0].Text
                };
                parent.Controls[0].Dispose();
            } else {
                control = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                int maxWidth = 0;
                foreach(string str in values) {
                    int strWidth = TextRenderer.MeasureText(str, control.Font).Width;
                    if(maxWidth < strWidth) {
                        maxWidth = strWidth;
                    }
                }
                ((ComboBox)control).Items.AddRange(values);
            }
            control.Margin = new Padding(0, 3, 0, 0);
            control.Dock = DockStyle.Left;
            control.AutoSize = true;

            parent.Controls.Add(control);
            return control;
        }

        protected Control AddOption(TableLayoutPanel parent, OptionsInfo? tuple) {
            if(tuple == null) {
                parent.Dispose();
                return null;
            }

            Control[] values = tuple?.values;

            Control control;
            if(values == null || values.Length == 1) {
                control = values?[0];
            } else {
                control = new OptionsButton(values);
            }
            control.Margin = new Padding(0, 3, 0, 0);
            control.Dock = DockStyle.Left;
            control.AutoSize = true;

            parent.Controls.Add(control);
            return control;
        }

        protected void SetupPresets(XmlNodeList nodeList) {
            foreach(XmlNode preset in nodeList) {
                HashSet<string> settings = new HashSet<string>();
                foreach(XmlNode setting in preset.ChildNodes) {
                    settings.Add(setting.Attributes["name"].Value);
                }
                presetsDict.Add(preset.Attributes["name"].Value, settings);
            }
        }

        public virtual XmlNode GetSettings(XmlDocument doc) {
            XmlElement xmlElement = doc.CreateElement("Settings");
            xmlElement.AppendChild(ToElement(doc, "Start", Start));
            xmlElement.AppendChild(ToElement(doc, "Reset", Reset));
            XmlElement xmlOptions = doc.CreateElement("Options");
            foreach(string option in Options) {
                xmlOptions.AppendChild(ToElement(doc, "Option", option));
            }
            xmlElement.AppendChild(xmlOptions);
            return xmlElement;
        }

        public virtual void SetSettings(XmlNode settings) {
            if(settings["Start"] != null) { Start = Int32.Parse(settings["Start"].InnerText); }
            if(settings["Reset"] != null) { Reset = Int32.Parse(settings["Reset"].InnerText); }
            if(settings["Options"] != null) {
                List<string> optionList = new List<string>();
                foreach(XmlNode option in settings.SelectNodes("Options/Option")) {
                    optionList.Add(option.InnerText);
                }
                Options = optionList.ToArray();
            }
        }

        protected XmlElement ToElement<T>(XmlDocument document, string name, T value) {
            XmlElement xmlElement = document.CreateElement(name);
            xmlElement.InnerText = value.ToString();
            return xmlElement;
        }

        protected virtual void Settings_Load(object sender, EventArgs e) {
            Form form = FindForm();
            SetFormVersion(form);
            form.MaximumSize = new Size(10000, 10000);
            UpdatePreset();
        }

        protected void UpdatePreset() {
            string presetName = null;
            foreach(KeyValuePair<string, HashSet<string>> preset in presetsDict) {
                if(Splits.SetEquals(preset.Value)) {
                    presetName = preset.Key;
                    break;
                }
            }

            lastPreset = presetName ?? CustomPreset;

            if(lastPreset == CustomPreset && !ComboBoxPreset.Items.Contains(CustomPreset)) {
                ComboBoxPreset.Items.Add(CustomPreset);
            } else if(lastPreset != CustomPreset && ComboBoxPreset.Items.Contains(CustomPreset)) {
                ComboBoxPreset.Items.Remove(CustomPreset);
            }

            ComboBoxPreset.Text = lastPreset;
        }

        protected void ComboBoxPreset_DropDown(object sender, EventArgs e) {
            if(ComboBoxPreset.Items.Contains(CustomPreset)) {
                ComboBoxPreset.Items.Remove(CustomPreset);
            }
        }

        protected void ComboBoxPreset_DropDownClosed(object sender, EventArgs e) {
            if(String.IsNullOrEmpty(ComboBoxPreset.Text)) {
                ComboBoxPreset.Items.Add(CustomPreset);
                ComboBoxPreset.Text = CustomPreset;
            }
        }

        protected virtual void ComboBoxPreset_SelectedIndexChanged(object sender, EventArgs e) {
            if(ComboBoxPreset.Text.Equals(CustomPreset)) { return; }

            if(!ComboBoxPreset.Text.Equals(lastPreset) && lastPreset.Equals(CustomPreset) && Splits.Count > 0) {
                if(MessageBox.Show("You have a custom preset, do you want to overwrite it?", "Overwrite existing preset?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) {
                    if(!ComboBoxPreset.Items.Contains(CustomPreset)) {
                        ComboBoxPreset.Items.Add(CustomPreset);
                    }
                    ComboBoxPreset.Text = CustomPreset;
                    return;
                }
                ComboBoxPreset.Items.Remove(CustomPreset);
                lastPreset = ComboBoxPreset.Text;
            }
        }

        protected void ButtonUncheckAll_Click(object sender, EventArgs e) => CheckAll(false);

        protected void ButtonCheckAll_Click(object sender, EventArgs e) => CheckAll(true);

        protected abstract void CheckAll(bool value);

        protected void SuspendDrawing(object sender, TreeViewCancelEventArgs e) => SuspendDrawing();

        protected void ResumeDrawing(object sender, TreeViewEventArgs e) => ResumeDrawing();

        protected void SuspendDrawing(Control control = null) {
            NativeMethods.EnableDraw(control?.Handle ?? Handle, false);
        }

        protected void ResumeDrawing(Control control = null) {
            NativeMethods.EnableDraw(control?.Handle ?? Handle, true);
            (control ?? this).Refresh();
        }
    }

    public delegate void OptionEventHandler(object sender, OptionEventArgs e);

    public class OptionEventArgs : EventArgs {
        public string Name { get; set; }
        public int State { get; set; }
    }

    interface IOption {
        OptionEventHandler OnChanged { get; set; }
    }

    public class OptionCheckBox : CheckBox, IOption {
        public OptionEventHandler OnChanged { get; set; }
        public OptionCheckBox() : base() {
            AutoSize = true;
            CheckedChanged += (s, e) => {
                OnChanged?.Invoke(s, new OptionEventArgs {
                    Name = Name,
                    State = ((CheckBox)s).Checked ? 1 : 0
                });
            };
        }
    }

    public class OptionButton : Button, IOption {
        public OptionEventHandler OnChanged { get; set; }
        public OptionButton() : base() {
            AutoSize = true;
            Dock = DockStyle.Fill;
            Click += (s, e) => {
                OnChanged?.Invoke(s, new OptionEventArgs {
                    Name = Name,
                    State = 1
                });
            };
        }
    }

    public class OptionsButton : Button, IOption {

        protected readonly Form optionsForm;

        public OptionEventHandler OnChanged { get; set; }

        public OptionsButton(params Control[] controls) {
            Text = "Options";

            Click += (s, e) => {
                if(optionsForm.Visible) {
                    HideDropDown();
                } else {
                    ShowDropDown();
                }
            };

            optionsForm = new PopupForm {
                BackColor = Color.Black,
                Padding = new Padding(1)
            };

            FlowLayoutPanel flp = new FlowLayoutPanel {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                BackColor = SystemColors.Control
            };
            foreach(Control control in controls) {
                if(control is IOption optionControl) {
                    optionControl.OnChanged += (s, e) => OnChanged?.Invoke(s, e);
                }
                flp.Controls.Add(control);
            }
            optionsForm.Controls.Add(flp);
        }

        public string[] GetOptions() {
            List<string> optionsList = new List<string>();
            foreach(Control control in optionsForm.Controls[0].Controls) {
                if(control is OptionCheckBox optionCheckBox && optionCheckBox.Checked) {
                    optionsList.Add(optionCheckBox.Name);
                }
            }
            return optionsList.ToArray();
        }

        public void SetOptions(string[] options) {
            foreach(Control control in optionsForm.Controls[0].Controls) {
                if(control is OptionCheckBox optionCheckBox) {
                    optionCheckBox.Checked = options.Contains(optionCheckBox.Name);
                }
            }
        }

        private void ShowDropDown() {
            optionsForm.Location = PointToScreen(new Point(0, Height));
            optionsForm.Show();
        }

        private void HideDropDown() {
            optionsForm.Hide();
        }
    }

    public class AbstractControlDescriptionProvider<TAbstract, TBase> : TypeDescriptionProvider {
        public AbstractControlDescriptionProvider() : base(TypeDescriptor.GetProvider(typeof(TAbstract))) { }

        public override Type GetReflectionType(Type objectType, object instance) {
            return objectType == typeof(TAbstract) ? typeof(TBase) : base.GetReflectionType(objectType, instance);
        }

        public override object CreateInstance(IServiceProvider provider, Type objectType, Type[] argTypes, object[] args) {
            if(objectType == typeof(TAbstract)) { objectType = typeof(TBase); }

            return base.CreateInstance(provider, objectType, argTypes, args);
        }
    }
}