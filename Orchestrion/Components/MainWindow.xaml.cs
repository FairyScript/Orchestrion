using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NHotkey.Wpf;
using Orchestrion.Utils;
namespace Orchestrion
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollectionEx<MidiFileObject> MidiFiles { get; set; } = new ObservableCollectionEx<MidiFileObject>();
        public ObservableCollectionEx<string> TrackNames { get; set; } = new ObservableCollectionEx<string>();

        private int lastSelectdTrackIndex = -1;//上一次选择的轨道编号
        public State state { get; set; } = State.state;

        public MainWindow()
        {
            InitializeComponent();
            InitializeHotKey();
            Network.RegisterToFirewall();
        }

        private void InitializeHotKey()
        {
            try
            {
                //HotkeyManager.Current.AddOrReplace()
            }
            catch (Exception)
            {

                MessageBox.Show("快捷键注册失败!");
            }
        }

        private void importMidiBtn_Click(object sender, RoutedEventArgs e)
        {
            var midiFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择MIDI文件",
                Filter = "MIDI文件|*.mid",
                Multiselect = true
            };
            if (midiFileDialog.ShowDialog() == true)
            {
                foreach (var midi in midiFileDialog.FileNames)
                {
                    MidiFiles.Add(new MidiFileObject(midi));
                }

            }
            Console.WriteLine("up");
            
        }

        private void midiListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TrackNames.Clear();

            MidiFileObject midi = (MidiFileObject)(sender as ListView).SelectedItem;
            if (midi == null) return;

            try
            {
                if (midi.Tracks == null) midi.ReadFile();
                TrackNames.AddRange(midi.TrackNames);

                trackListView.SelectedIndex = Math.Max(lastSelectdTrackIndex, trackListView.SelectedIndex);
            }
            catch (Exception error)
            {
                MessageBox.Show($"Midi文件读取出错！\r\n异常信息：{error.Message}\r\n 异常类型{error.GetType()}",
                    "读取错误", MessageBoxButton.OK);
            }

        }

        private void midiListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (MidiFiles.Count > 0)
            {
                //delete item
                if (e.Key == Key.Delete || e.Key == Key.Back)
                {
                    var delete = MidiFiles.FirstOrDefault(x => ((sender as ListView).SelectedItem as MidiFileObject).Name == x.Name);
                    if (delete != null) MidiFiles.Remove(delete);
                }
                
            }
            
        }

        private void trackListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //恢复记忆的轨道编号
            if((sender as ListView).SelectedIndex != -1) lastSelectdTrackIndex = (sender as ListView).SelectedIndex;
        }

        private void captureBtn_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^\d-]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
