using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Orchestrion.Utils;
using static Orchestrion.Utils.ObservableProperties;

namespace Orchestrion.Components
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        public Dictionary<string, Prop<KeyCombination>> HotkeyBinding { get; set; } = new Dictionary<string, Prop<KeyCombination>>();
        public SettingWindow()
        {
            InitializeComponent();
        }

        private void hotKeyBind_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // The text box grabs all input.0
            e.Handled = true;

            // Fetch the actual shortcut key.
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

            // Ignore modifier keys.
            if (key == Key.LeftShift || key == Key.RightShift
                || key == Key.LeftCtrl || key == Key.RightCtrl
                || key == Key.LeftAlt || key == Key.RightAlt
                || key == Key.LWin || key == Key.RWin)
            {
                return;
            }
            if (Keyboard.Modifiers == ModifierKeys.None) return;

            HotkeyBinding[(string)(sender as TextBox).Tag].Value = new KeyCombination { Key = key, ModifierKeys = Keyboard.Modifiers };
            return;
        }

        private void Window_Initialized(object sender, System.EventArgs e)
        {
            Hotkey.RemoveAll();
            foreach (var item in Config.config.HotkeyBindings)
            {
                HotkeyBinding.Add(item.Key, new Prop<KeyCombination> { Value = item.Value });
            }
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            foreach (var item in HotkeyBinding)
            {
                Config.config.HotkeyBindings[item.Key] = item.Value.Value;
            }
            Config.Save();
        }
    }
}
