using GuerrillaNtp;
using Melanchall.DryWetMidi.Core;
using Orchestrion.Components;
using Orchestrion.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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
        private System.Timers.Timer playTimer;
        private System.Timers.Timer captureTimer;
        private DateTime TimeWhenPlay { get; set; } = DateTime.MinValue;
        private TimeSpan SystemTimeOffset { get; set; } = TimeSpan.Zero;
        private TimeSpan NetTimeOffset { get; set; } = TimeSpan.Zero;

        public MainWindow()
        {
            InitializeComponent();
            Title += $" Ver {Assembly.GetExecutingAssembly().GetName().Version} Alpha";
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
                                Logger.Debug($"keyboard: {@event.NoteNumber} pressed");
                                KeyController.KeyboardPress(@event.NoteNumber);
                                break;
                            }
                        case NoteOffEvent @event:
                            {
                                Logger.Debug($"keyboard: {@event.NoteNumber} release");
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
                Dispatcher.Invoke(() =>
                {
                    if (e.PropertyName == nameof(state.ReadyFlag))
                    {
                        if (state.ReadyFlag)
                        {
                            network?.Start();
                            readyBtn.Background = System.Windows.Media.Brushes.Pink;
                            readyBtn.Content = "取消准备";

                            try
                            {
                                using (var ntp = new NtpClient(Dns.GetHostAddresses(config.NtpServer)[0]))
                                    SystemTimeOffset = ntp.GetCorrectionOffset();
                            }
                            catch (Exception ex)
                            {
                                // timeout or bad SNTP reply
                                SystemTimeOffset = TimeSpan.Zero;
                                MessageBox.Show($"更新系统时间失败!\n{ex.Message}");
                            }
                        }
                        else
                        {
                            network?.Stop();
                            readyBtn.Background = System.Windows.Media.Brushes.AliceBlue;
                            readyBtn.Content = "准备好了";
                        }
                    }

                    if (e.PropertyName == nameof(state.PlayingFlag))
                    {
                        if (state.PlayingFlag)
                        {
                            playBtn.Background = System.Windows.Media.Brushes.Orange;
                            playBtn.Content = "停止演奏";
                        }
                        else
                        {
                            TimeWhenPlay = DateTime.MinValue;

                            //UI
                            playBtn.Background = System.Windows.Media.Brushes.AliceBlue;
                            playBtn.Content = "开始演奏";
                        }
                    }

                    if (e.PropertyName == nameof(state.MidiDeviceConnected))
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
                });
            };
        }

        private void InitializeHotKey()
        {
            var actions = new Dictionary<string, Action>
            {
                {"StartPlay",()=>{
                    if (Utils.Utils.IsGameActived())
                    {
                        if(TimeWhenPlay == DateTime.MinValue)
                        {
                            StartPlay(1000);
                        }
                        else
                        {
                            StartPlay();
                        }
                    }
                } },
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
            TimeWhenPlay = DateTime.Now.AddMilliseconds(time);
            StartPlay();
        }
        void StartPlay()
        {
            if (state.PlayingFlag) return;

            try
            {
                if ((MidiFileObject)midiListView.SelectedValue == null) throw new Exception("没有MIDI文件!");
                TimeSpan time = (TimeWhenPlay - DateTime.Now) + SystemTimeOffset + NetTimeOffset;
                if (time > TimeSpan.Zero)
                {
                    double timeMs = time.TotalMilliseconds;
                    playTimer = new System.Timers.Timer
                    {
                        Interval = timeMs,
                        AutoReset = false
                    };
                    playTimer.Elapsed += Timer_Elapsed;

                    playTimer.Start();
                    state.PlayingFlag = true;
                    activeMidi?.GetPlayback();
                    Logger.Info($"timer start,Interval:{timeMs}ms");
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        activeMidi?.StartPlayback(time);
                    });
                }
                
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                
            }
        }
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                activeMidi?.StartPlayback();
            });
        }

        public void StopPlay()
        {
            playTimer?.Dispose();
            activeMidi?.StopPlayback();
            state.PlayingFlag = false;
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


        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^\d-]+");
            e.Handled = regex.IsMatch(e.Text);
        }


        private void ffxivProcessSelect_Initialized(object sender, EventArgs e)
        {
            FFProcessList.Clear();
            var processList = new List<uint>();
            foreach (var item in Utils.Utils.FindFFProcess())
            {
                processList.Add((uint)item.Id);
            }
            FFProcessList.AddRange(processList);
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
                                StopPlay();
                                Logger.Debug("net stop");
                                break;
                            }
                        case 1:
                            {
                                DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)); // 当地时区
                                TimeWhenPlay = startTime.AddSeconds(timestamp + interval);
                                //var msTime = (dt - (DateTime.Now + SystemTimeOffset)).TotalMilliseconds;
                                if(state.ReadyFlag) StartPlay();
                                Logger.Debug("net start");
                                break;
                            }
                    }
                });

            };
        }

        private void readyBtn_Click(object sender, RoutedEventArgs e)
        {
            state.ReadyFlag = !state.ReadyFlag;
        }

        private void playBtn_Click(object sender, RoutedEventArgs e)
        {

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

        private void KeyBindingBtn_Click(object sender, RoutedEventArgs e)
        {
            var bind = new KeyBindingWindow();
            bind.Show();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

    }
}
