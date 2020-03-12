using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using Orchestrion.Utils;

namespace Orchestrion.Components
{
    /// <summary>
    /// KeyBindingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class KeyBindingWindow : Window
    {
        public Dictionary<int, int> Keymap { get; set; } = Config.config.KeyMap;
        int[,] noteMap1 = new int[3, 7]
        {
            {72,74,76,77,79,81,83},
            {60,62,64,65,67,69,71},
            {48,50,52,53,55,57,59}
        };
        int[,] noteMap2 = new int[3, 5]
        {
            {73,75,78,80,82 },
            {61,63,66,68,70 },
            {49,51,54,56,58 }
        };
        public KeyBindingWindow()
        {
            InitializeComponent();
            InitializeInputBox();
        }

        private void InitializeInputBox()
        {
            MakeGrid(Grid1, noteMap1);
            MakeGrid(Grid2, noteMap2);
        }

        private void MakeGrid(Grid grid,int[,] keymap)
        {
            for (int i = 0; i < grid.RowDefinitions.Count - 1; i++)
            {
                for (int j = 0; j < grid.ColumnDefinitions.Count - 1; j++)
                {
                    var inputBox = new TextBox
                    {
                        Margin = new Thickness(5),
                        Tag = keymap[i, j],
                    };
                    inputBox.PreviewKeyDown += InputBox_PreviewKeyDown;
                    InputMethod.SetIsInputMethodEnabled(inputBox, false);

                    grid.Children.Add(inputBox);
                    Grid.SetColumn(inputBox, j + 1);
                    Grid.SetRow(inputBox, i + 1);
                }
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeContent();
        }

        private void InitializeContent()
        {
            foreach (var item in Utils.Utils.FindVisualChildren<TextBox>(this))
            {
                var tag = (int)(item.Tag);
                if (Keymap.ContainsKey(tag))
                {
                    item.Text = GetKeyChar(Keymap[tag]).ToString();
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
        public static char GetKeyChar(int k)
        {
            var nonVirtualKey = MapVirtualKey((uint)k, 2);
            var mappedChar = Convert.ToChar(nonVirtualKey);
            return mappedChar;
        }

        private void InputBox_Initialized(object sender, EventArgs e)
        {
            var tb = sender as TextBox;
            var tag = (int)(tb.Tag);
            if (Keymap.ContainsKey(tag))
            {
                tb.Text = GetKeyChar(Keymap[tag]).ToString();
            }
        }

        private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
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
            if (Keyboard.Modifiers != ModifierKeys.None) return;
            var keycode = KeyInterop.VirtualKeyFromKey(key);
            var tag = (sender as TextBox).Tag;
            Keymap[(int)tag] = keycode;
            (sender as TextBox).Text = GetKeyChar(keycode).ToString();
            return;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Config.config.KeyMap = Keymap;
            Config.Save();
        }

        private void importBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择配置文件",
                Filter = "配置文件|*.cfg",
                Multiselect = false
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string text = File.ReadAllText(openFileDialog.FileName);
                Keymap = JsonConvert.DeserializeObject<Dictionary<int, int>>(text);
                InitializeContent();
            }
        }

        private void exportBtn_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "保存至...",
                Filter = "配置文件|*.cfg",
                FileName = "setting"

            };
            if(saveFileDialog.ShowDialog() == true)
            {
                string text = JsonConvert.SerializeObject(Keymap);
                File.WriteAllText(saveFileDialog.FileName,text);
            }
        }


    }
}
