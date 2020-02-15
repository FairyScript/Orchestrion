using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Orchestrion.Utils.ObservableProperties;

namespace Orchestrion
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<MidiFileObject> MidiFiles { get; set; } = new ObservableCollection<MidiFileObject>();
        public ObservableCollection<string> TrackNames { get; set; } = new ObservableCollection<string>();

        private int lastSelectdTrackIndex = -1;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDataBinding();


        }

        void InitializeDataBinding()
        {
            //midiListView.ItemsSource = midiFiles;
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
            if(midi.Tracks == null) midi.ReadFile();
            foreach (var item in midi.TrackNames)
            {
                TrackNames.Add(item);
                
            }

            trackListView.SelectedIndex = Math.Max(lastSelectdTrackIndex,trackListView.SelectedIndex);
        }

        private void midiListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (MidiFiles.Count > 0)
            {
                if (e.Key == Key.Delete || e.Key == Key.Back)
                {
                    var delete = MidiFiles.FirstOrDefault(x => ((sender as ListView).SelectedItem as MidiFileObject).Name == x.Name);
                    if (delete != null) MidiFiles.Remove(delete);
                }
                
            }
            
        }

        private void trackListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if((sender as ListView).SelectedIndex != -1) lastSelectdTrackIndex = (sender as ListView).SelectedIndex;
        }
    }
}
