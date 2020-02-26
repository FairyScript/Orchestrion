using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

        public ObservableCollectionEx<uint> FFProcessList { get; set; } = new ObservableCollectionEx<uint>();
        public State state { get; set; } = State.state;
        public ConfigObject config { get; set; } = Config.config;
        private Network network;
        private System.Timers.Timer timer;

        public MainWindow()
        {
            InitializeComponent();
            //InitializeHotKey();
            InitializeEvent();
            Network.RegisterToFirewall();
        }

        

        private void InitializeEvent()
        {
            //state event
            state.PropertyChanged += (object sender, PropertyChangedEventArgs e)=> 
            {
                if(e.PropertyName == nameof(state.IsCaptureFlag))
                {
                    if (state.IsCaptureFlag)
                    {
                        network?.Start();
                        captureBtn.Background = System.Windows.Media.Brushes.Pink;
                        captureBtn.Content = "停止同步";
                    }
                    else
                    {
                        network?.Stop();
                        captureBtn.Background = System.Windows.Media.Brushes.AliceBlue;
                        captureBtn.Content = "开始同步";
                    }
                }

                if (e.PropertyName == nameof(state.ReadyFlag))
                {
                    if (state.ReadyFlag)
                    {
                        readyBtn.Background = System.Windows.Media.Brushes.Orange;
                        readyBtn.Content = "取消准备";
                    }
                    else
                    {
                        readyBtn.Background = System.Windows.Media.Brushes.AliceBlue;
                        readyBtn.Content = "定时演奏";
                    }
                }
            };
        }

        private void InitializeHotKey()
        {
            var hotKeyEvents = new Dictionary<string, EventHandler<NHotkey.HotkeyEventArgs>>();
            hotKeyEvents.Add("StartPlay", (sender, e) =>StartPlay(1000));
            hotKeyEvents.Add("StopPlay", (sender, e) => StopPlay());

            try
            {
                foreach (var item in config.HotkeyBindings)
                {
                    HotkeyManager.Current.AddOrReplace(item.Key, item.Value.Key, item.Value.ModifierKeys, hotKeyEvents[item.Key]);
                }
                
            }
            catch (Exception)
            {
                MessageBox.Show("快捷键注册失败!");
            }
        }

        void StartPlay(int time)
        {
            try
            {
                if ((MidiFileObject)midiListView.SelectedValue == null) throw new Exception("没有MIDI文件!");

                timer = new System.Timers.Timer
                {
                    Interval = time,
                    AutoReset = false
                };
                timer.Elapsed += Timer_Elapsed;

                timer.Start();
                Logger.Info($"timer start,Interval:{time}ms");
            }
            catch (Exception e)
            {
                using (var form = new System.Windows.Forms.Form { TopMost = true })
                {
                    System.Windows.Forms.MessageBox.Show(form, e.Message);
                }
            }
            
        }
        private void StopPlay()
        {
            timer?.Dispose();
            (midiListView.SelectedItem as MidiFileObject).StopPlayback();
            Logger.Info($"stop play");
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() => {
                (midiListView.SelectedItem as MidiFileObject).StartPlayback();
            }));
            Logger.Info($"start play");
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
            Logger.Debug("track select change");
            var viewIndex = (sender as ListView).SelectedIndex;

            if (viewIndex == -1)
            {
                (sender as ListView).SelectedIndex = (midiListView.SelectedItem as MidiFileObject).SelectedIndex;
            }
            else
            {
                (midiListView.SelectedItem as MidiFileObject).SelectedIndex = (sender as ListView).SelectedIndex;
            }
        }

        private void captureBtn_Click(object sender, RoutedEventArgs e)
        {
            state.IsCaptureFlag = !state.IsCaptureFlag;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^\d-]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        
        private void ffxivProcessSelect_Initialized(object sender, EventArgs e)
        {
            FFProcessList.Clear();
            FFProcessList.AddRange(Utils.Utils.FindFFProcess());
            ffxivProcessSelect.SelectedIndex = 0;
        }

        private void ffxivProcessSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ffxivProcessSelect.SelectedItem == null) return;
            Console.WriteLine("net");
            network = new Network((uint)ffxivProcessSelect.SelectedItem);
        }

        private void readyBtn_Click(object sender, RoutedEventArgs e)
        {
            StartPlay(1000);
        }
    }
}
