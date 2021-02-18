
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using GuerrillaNtp;
using Melanchall.DryWetMidi.Core;
using InputDevice = Melanchall.DryWetMidi.Devices.InputDevice;
using Orchestrion.Components;
using Orchestrion.Utils;
using System.Linq;
using NLog;
using System.Threading.Tasks;
using System.IO;

namespace Orchestrion
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollectionEx<MidiFileObject> MidiFiles { get; set; } = new ObservableCollectionEx<MidiFileObject>();
        public ObservableCollectionEx<string> TrackNames { get; set; } = new ObservableCollectionEx<string>();
        public ObservableCollectionEx<Process> FFProcessList { get; set; } = new ObservableCollectionEx<Process>();
        public ObservableCollectionEx<InputDevice> MidiDeviceList { get; set; } = new ObservableCollectionEx<InputDevice>();
        public State state { get; set; } = State.state;
        public ConfigObject config { get; set; } = Config.config;
        Logger Logger = LogManager.GetCurrentClassLogger();
        private MidiFileObject activeMidi;
        private KeyController kc;
        private Network network;
        private System.Timers.Timer playTimer;
        private System.Timers.Timer captureTimer;
        

        public MainWindow()
        {
            InitializeComponent();
            Title += $" Ver {Assembly.GetExecutingAssembly().GetName().Version}";

#if DEBUG
            Title += $" Alpha";
#endif
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Hotkey.Initial(new WindowInteropHelper(this).Handle);//初始化快捷键窗口句柄
            InitializeHotKey();
            InitializeEvent();
            InitializeDevice();
            InitializeNetwork();
        }



        /* === 初始化 === */
        private void InitializeHotKey()
        {
            var actions = new Dictionary<string, Action>
            {
                {"StartPlay",TogglePlay},
                {"StopPlay",()=>StopPlay(true)},
            };
            try
            {
                foreach (var item in config.HotkeyBindings)
                {
                    Hotkey.Add(item.Key, item.Value, actions[item.Key]);
                }

            }
            catch (Exception)
            {
                MessageBox.Show("快捷键注册失败!");
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
                            readyBtn.Background = System.Windows.Media.Brushes.Pink;
                            readyBtn.Content = "取消准备";
                            Task.Run(()=> SyncTime());
                        }
                        else
                        {
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

        private void InitializeDevice()
        {
            foreach (var item in InputDevice.GetAll())
            {
                MidiDeviceList.Add(item);
            }
        }
        private void InitializeNetwork()
        {
            Network.RegisterToFirewall();
            captureTimer = new System.Timers.Timer
            {
                AutoReset = true,
                Interval = 60000
            };
            captureTimer.Elapsed += (sender, e) => SyncTask();

            captureTimer.Start();
            SyncTask();//马上执行一次
        }
        private void SyncTask()
        {
            var list = Utils.Utils.FindFFProcess();
            if (network == null || !network.IsListening)
            {
                if (list.Count > 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        FFProcessList.Clear();
                        FFProcessList.AddRange(list);
                        ffxivProcessSelect.SelectedIndex = 0;
                        captureTimer?.Stop();
                    });
                }
            }
            else
            {
                try
                {
                    var gameProcess = Process.GetProcessById((int)network.ProcessID);
                    gameProcess.Dispose();
                }
                catch (ArgumentException e)
                {
                    Logger.Warn(e.Message);
                    network.Stop();
                    state.ReadyFlag = false;
                    Dispatcher.Invoke(() => FFProcessList.Clear());
                }
            }

            if (state.ReadyFlag)
            {
                SyncTime();
            }
        }

        //同步NTP时间
        void SyncTime()
        {
      
            try
            {
                using (var ntp = new NtpClient(Dns.GetHostAddresses(config.NtpServer)[0]))
                {
                    var offset = ntp.GetCorrectionOffset();
                    state.SystemTimeOffset = offset;
                    Logger.Trace($"update ntp offset: {offset}");
                }
            }
            catch (Exception ex)
            {
                // timeout or bad SNTP reply
                //state.SystemTimeOffset = TimeSpan.Zero;
                MessageBox.Show($"更新时间失败!\n{ex.Message}");
                Logger.Warn($"更新时间失败! {ex.Message}");
            }
        }

        /* === 播放控制 === */
        void StartPlay(int time)
        {
            state.TimeWhenPlay = DateTime.Now.AddMilliseconds(time);
            StartPlay();
        }
        void StartPlay()
        {
            if (state.PlayingFlag)
            {
                Logger.Warn("在演奏期间受到了合奏准备信号,返回");
                return;
            }

            try
            {
                activeMidi = midiListView.SelectedItem as MidiFileObject;
                if (activeMidi == null) throw new Exception("没有MIDI文件!");
                activeMidi?.GetPlayback();
                activeMidi.controller = kc;
                TimeSpan time = state.TimeWhenPlay - (DateTime.Now + state.SystemTimeOffset + state.NetTimeOffset);
                if (time > TimeSpan.Zero)
                {
                    double timeMs = time.TotalMilliseconds;
                    playTimer = new System.Timers.Timer
                    {
                        Interval = timeMs,
                        AutoReset = false
                    };
                    playTimer.Elapsed += (sender, e) => activeMidi?.StartPlayback();

                    playTimer.Start();
                    state.PlayingFlag = true;
                    Logger.Info($"timer start,Interval:{timeMs}ms");
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        activeMidi?.StartPlayback(time);
                        state.PlayingFlag = true;
                    });
                }
                //Utils.Utils.SwitchToGameWindow((Process)ffxivProcessSelect.SelectedItem);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
#if DEBUG
                throw;
#endif
            }
        }

        public void StopPlay(bool needReset = false)
        {
            playTimer?.Dispose();
            activeMidi?.StopPlayback();
            state.PlayingFlag = false;
            if (needReset) state.TimeWhenPlay = DateTime.MinValue;
        }

        /// <summary>
        /// 播放/暂停
        /// </summary>
        private void TogglePlay()
        {
            if (state.PlayingFlag)
            {
                StopPlay(false);
            }
            else
            {
                if (state.TimeWhenPlay == DateTime.MinValue)
                {
                    StartPlay(1000);
                }
                else
                {
                    StartPlay();
                }
            }
        }

        /* === MIDI导入 === */
        private void importMidiBtn_Click(object sender, RoutedEventArgs e)
        {
            var beforeCount = MidiFiles.Count;
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
                if(beforeCount != 0) midiListView.SelectedItem = MidiFiles.LastOrDefault();
            }
        }

        private void midiListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            var listView = sender as ListView;
            var selectedMidi = (MidiFileObject)listView.SelectedItem;
            TrackNames.Clear();
            if (selectedMidi == null) return;
            try
            {
                if (selectedMidi.Tracks == null) selectedMidi.ReadFile();
                TrackNames.AddRange(selectedMidi.TrackNames);

                listView.ScrollIntoView(listView.SelectedItem);
                listView.Focus();
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
                if (listView.SelectedIndex == -1) listView.SelectedItem = MidiFiles.LastOrDefault();//选中最后一个
            }

        }

        private void trackListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedMidi = (MidiFileObject)midiListView.SelectedItem;

            if (selectedMidi == null) return;
            var viewIndex = (sender as ListView).SelectedIndex;
            if (viewIndex == -1)
            {
                (sender as ListView).SelectedIndex = selectedMidi.SelectedIndex;
            }
            else if(viewIndex != selectedMidi.SelectedIndex)
            {
                selectedMidi.SelectedIndex = (sender as ListView).SelectedIndex;
            }
        }

        //受限于权限问题无法工作
        //private void midiListView_DragEnter(object sender, DragEventArgs e)
        //{
        //    Console.WriteLine("enter");
        //    if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Move;
        //    else e.Effects = DragDropEffects.None;
        //}

        //private void midiListView_Drop(object sender, DragEventArgs e)
        //{
        //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
        //    {
        //        // Note that you can have more than one file.
        //        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        //        Regex midipattern = new Regex(@"\.midi?$");
        //        // Assuming you have one file that you care about, pass it off to whatever
        //        // handling code you have defined.
        //        foreach (var filename in files)
        //        {
        //            if (midipattern.IsMatch(filename)) MidiFiles.Add(new MidiFileObject(filename));
        //        }
        //    }
        //    e.Handled = true;
        //}

        /* === FFXIV 进程 === */
        private void ffxivProcessSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            Logger.Trace($"ffxivProcessSelect_SelectionChanged: {cb.SelectedIndex}");
            var gameProcess = cb.SelectedItem as Process;

            if (cb.SelectedItem != null)
            {
                //setup keycontroller
                kc = new KeyController(gameProcess);
                //listen network
                var pid = (uint)gameProcess.Id;

                try
                {
                    //check game version to select opcode
                    var version = Utils.Utils.GetGameVersion(gameProcess);
                    Opcode opcode;
                    if (Config.SupportOpcode.TryGetValue(version, out opcode))
                    {
                        ListenProcess(pid, opcode);
                    }
                    else
                    {
                        //TODO: 读取外部opcode配置
                        TopmostMessageBox.Show("游戏版本不被支持!网络合奏功能将不会工作");
                        return;
                    }
                }
                catch (Exception)
                {
                    TopmostMessageBox.Show("游戏版本读取失败!网络合奏功能将不会工作");
                    return;

                }

                //addon exit hook
                gameProcess.EnableRaisingEvents = true;
                gameProcess.Exited += (s, ee) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (gameProcess == cb.SelectedItem)
                        {
                            network?.Stop();
                        }
                        Logger.Info($"进程 {gameProcess.Id} 已退出");
                        FFProcessList.Remove(gameProcess);
                        captureTimer.Start();
                    });
                };

            }
            else if(FFProcessList.Count == 0)
            {
                network?.Stop();
                kc = new KeyController();
            }
        }

        private void ListenProcess(uint pid, Opcode opc)
        {
            if(network == null)
            {
                network = new Network()
                {
                    ProcessID = pid,
                    opcode = opc
                };

                network.OnReceived += (mode, interval, timestamp) =>
                {
                    Dispatcher.Invoke(() => {
                        switch (mode)
                        {
                            case 0:
                                {
                                    StopPlay(true);
                                    Logger.Debug("net stop");
                                    break;
                                }
                            case 1:
                                {
                                    DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)); // 当地时区
                                    state.TimeWhenPlay = startTime.AddSeconds(timestamp + interval);
                                    //var msTime = (dt - (DateTime.Now + SystemTimeOffset)).TotalMilliseconds;
                                    if (state.ReadyFlag)
                                    {
                                        StartPlay();
                                        Logger.Info($"net: TimeWhenPlay[{state.TimeWhenPlay}]");
                                    }
                                    else
                                    {
                                        Logger.Warn("收到了网络开始的指令,但是没有准备,已忽略.");
                                    }

                                    break;
                                }
                        }
                    });
                };

                network.OnStatusChanged += status =>
                {
                    Dispatcher.Invoke(() =>
                    {

                        if (status)
                        {
                            captureStatusLabel.Content = "状态:已同步";
                            captureStatusLabel.Foreground = System.Windows.Media.Brushes.BlueViolet;
                        }
                        else
                        {
                            captureStatusLabel.Content = "状态:未同步";
                            captureStatusLabel.Foreground = System.Windows.Media.Brushes.Red;

                        }
                    });
                };
            }
            else if(pid != network.ProcessID)
            {
                network.Stop();
                network.ProcessID = pid;
            }
            
            network.Start();
        }
        private void refreshProcessBtn_Click(object sender, RoutedEventArgs e)
        {
            SyncTask();
            
            //防止激情连打
            var btn = sender as Button;
            btn.IsEnabled = false;
            var timer = new System.Timers.Timer
            {
                Interval = 1000,
                AutoReset = false
            };

            timer.Elapsed += (s, ee) =>
            {
                Dispatcher.Invoke(() => btn.IsEnabled = true);
                timer.Dispose();
            };
            timer.Start();
        }
        
        private void readyBtn_Click(object sender, RoutedEventArgs e)
        {
            if(network!=null && network.IsListening)
            {
                state.ReadyFlag = !state.ReadyFlag;
                Logger.Trace($"ReadyClick! now: {state.ReadyFlag}");
            }
            else
            {
                Logger.Warn("没有检测到FFXIV进程");
                TopmostMessageBox.Show("没有检测到FFXIV进程!请检查同步情况");
            }
        }

        private void playBtn_Click(object sender, RoutedEventArgs e)
        {
            TogglePlay();
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



        private void deviceConnect_Click(object sender, RoutedEventArgs e)
        {
            var device = (midiDeviceSelect.SelectedItem as InputDevice);
            if (!state.MidiDeviceConnected)
            {
                device.EventReceived += (se, ee) =>
                {
#if DEBUG
                    Console.WriteLine(ee.Event);
#endif
                    switch (ee.Event)
                    {
                        case NoteOnEvent @event:
                            {
                                Logger.Trace($"keyboard: {@event.NoteNumber} pressed");
                                kc.Press(@event.NoteNumber);
                                break;
                            }
                        case NoteOffEvent @event:
                            {
                                Logger.Trace($"keyboard: {@event.NoteNumber} release");
                                kc.Release(@event.NoteNumber);
                                break;
                            }
                    }
                };
                device?.StartEventsListening();

                state.MidiDeviceConnected = true;
            }
            else
            {
                device?.StopEventsListening();
                state.MidiDeviceConnected = false;
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

        private void netLatency_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var input = sender as TextBox;
            if(e.Text == "-")
            {
                e.Handled = input.SelectionStart != 0;//开头
            }
            else
            {
                e.Handled = !int.TryParse(e.Text,out _);
            }
        }

        private void netLatency_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = (sender as TextBox).Text;
            int result;
            if(int.TryParse(text,out result))
            {
                state.NetTimeOffset = TimeSpan.FromMilliseconds(result);
            }
        }


    }
}
