using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            //Hotkey init
            foreach (var item in Config.config.HotkeyBindings)
            {
                if (HotkeyBinding.ContainsKey(item.Key))
                {
                    HotkeyBinding[item.Key].Value = item.Value;
                }
                else
                {
                    HotkeyBinding.Add(item.Key, new Prop<KeyCombination> { Value = item.Value });
                }
            }

            //ntp init
            ntpServerInput.Text = Config.config.NtpServer;
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            foreach (var item in HotkeyBinding)
            {
                Config.config.HotkeyBindings[item.Key] = item.Value.Value;
            }

            Config.config.NtpServer = ntpServerInput.Text;

            Config.Save();
        }

        private void restoreSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("您确定要重置设置吗？\n 包括键盘映射在内的所有设置都将恢复原始!", "警告：", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {

                Config.config = ConfigObject.GetDefaultConfig();
                Config.Save();
                Window_Initialized(this,new System.EventArgs());

            }
        }

        private void editConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("你确定知道你在干什么吗?", "警告：", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                Process p = new Process();
                p.StartInfo.FileName= Config.configPath;
                p.EnableRaisingEvents = true;
                p.Exited += P_Exited;
                p.Start();

            }
        }

        private void P_Exited(object sender, System.EventArgs e)
        {
            MessageBox.Show("软件将重启以重新加载配置", "提示：");
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = Process.GetCurrentProcess().MainModule.FileName;
            try
            {
                Process.Start(psi);
                Environment.Exit(0);
                //Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
