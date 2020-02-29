using Melanchall.DryWetMidi.Core;
using Orchestrion.Components;
using Orchestrion.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using static Orchestrion.Utils.ObservableProperties;
using InputDevice = Melanchall.DryWetMidi.Devices.InputDevice;

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
        public ObservableCollectionEx<InputDevice> MidiDeviceList { get; set; } = new ObservableCollectionEx<InputDevice>();
        public State state { get; set; } = State.state;
        public ConfigObject config { get; set; } = Config.config;
        private MidiFileObject activeMidi;
        private Network network;
        private System.Timers.Timer timer;

        public MainWindow()
        {
            InitializeComponent();
            this.Title += $" Ver {Assembly.GetExecutingAssembly().GetName().Version} Alpha";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Hotkey.Initial(new WindowInteropHelper(this).Handle);
            InitializeHotKey();
            InitializeEvent();
            InitializeDevice();
            Network.RegisterToFirewall();
        }

        private void InitializeDevice()
        {
            foreach (var item in InputDevice.GetAll())
            {
                item.EventReceived += (sender, ee) =>
                {
                    switch (ee.Event)
                    {
                        case NoteOnEvent @event:
                            {
                                KeyController.KeyboardPress(@event.NoteNumber);
                                break;
                            }
                        case NoteOffEvent @event:
                            {
                                KeyController.KeyboardRelease(@event.NoteNumber);
                                break;
                            }
                    }
                };
                MidiDeviceList.Add(item);

            }
        }

        private void InitializeEvent()
        {
            //state event
            state.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(state.IsCaptureFlag))
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

                if(e.PropertyName == nameof(state.MidiDeviceConnected))
                {
                    if (state.MidiDeviceConnected)
                    {
                        deviceConnectBtn.Content = "断开连接";
                    }
                    else
                    {
                        deviceConnectBtn.Content = "开始连接";
                    }
                }
            };
        }

        private void InitializeHotKey()
        {
            var actions = new Dictionary<string, Action>
            {
                {"StartPlay",()=>StartPlay(1000) },
                {"StopPlay",StopPlay }
            };
            try
            {
                foreach (var item in config.HotkeyBindings)
                {
                    Hotkey.Add(item.Key, item.Value,actions[item.Key]);
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
                activeMidi.GetPlayback();
                Logger.Info($"timer start,Interval:{time}ms");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                
            }

        }
        private void StopPlay()
        {
            timer?.Dispose();
            activeMidi?.StopPlayback();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                activeMidi?.StartPlayback();
            });
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
                foreach (var path in midiFileDialog.FileNames)
                {
                    MidiFiles.Add(new MidiFileObject(path));
                }

            }
        }

        private void midiListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TrackNames.Clear();

            MidiFileObject midi = (MidiFileObject)(sender as ListView).SelectedItem;
            if (midi == null) return;
            activeMidi = midi;
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
            var listView = sender as ListView;
            if (MidiFiles.Count > 0 && listView.SelectedItems.Count > 0)
            {
                //delete item
                if (e.Key == Key.Delete || e.Key == Key.Back)
                {
                    var list = new List<MidiFileObject>();
                    foreach (MidiFileObject item in listView.SelectedItems)
                    {
                        list.Add(item);
                    }
                    foreach (MidiFileObject item in list)
                    {
                        MidiFiles.Remove(item);
                    }
                }
                if (listView.SelectedIndex == -1) listView.SelectedIndex = 0;//选中第一个
            }

        }

        private void trackListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (midiListView.SelectedValue == null) return;//空检测
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
            if(network == null)
            {
                Logger.Error("没有FFXIV进程!");
                return;
            }
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
            network = new Network((uint)ffxivProcessSelect.SelectedItem);
            network.OnReceived += (mode, interval,timestamp) =>
            {
                Dispatcher.Invoke(() => {
                    switch (mode)
                    {
                        case 0:
                            {
                                Logger.Debug("net stop");
                                StopPlay();
                                break;
                            }
                        case 1:
                            {
                                Logger.Debug("net start");
                                DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)); // 当地时区
                                DateTime dt = startTime.AddSeconds(timestamp + interval);

                                var msTime = (dt - DateTime.Now).TotalMilliseconds;
                                StartPlay((int)msTime);
                                break;
                            }
                    }
                });

            };
        }

        private void readyBtn_Click(object sender, RoutedEventArgs e)
        {
            StartPlay(1000);
        }

        private void settingBtn_Click(object sender, RoutedEventArgs e)
        {
            var setting = new SettingWindow();
            setting.Closed += (s, ee) =>
            {
                InitializeHotKey();
            };
            setting.Show();
        }

        private void midiListView_DragEnter(object sender, DragEventArgs e)
        {
            Console.WriteLine("enter");
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Move;
            else e.Effects = DragDropEffects.None;
        }

        private void midiListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                Regex midipattern = new Regex(@"\.midi?$");
                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                foreach (var filename in files)
                {
                    if(midipattern.IsMatch(filename)) MidiFiles.Add(new MidiFileObject(filename));
                }
            }
            e.Handled = true;
        }

        private void deviceConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!state.MidiDeviceConnected)
                {
                    (midiDeviceSelect.SelectedItem as InputDevice)?.StartEventsListening();
                    state.MidiDeviceConnected = true;
                }
                else
                {
                    (midiDeviceSelect.SelectedItem as InputDevice)?.StopEventsListening();
                    state.MidiDeviceConnected = false;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void refreshDevice_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in MidiDeviceList)
            {
                item.Dispose();
            }
            MidiDeviceList.Clear();
            InitializeDevice();
            midiDeviceSelect.SelectedIndex = 0;
        }
    }
}
