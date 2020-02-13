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

namespace Orchestrion
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        State state = State.GetInstance();
        public ObservableCollection<string> midiFiles = new ObservableCollection<string>();
        public MainWindow()
        {
            InitializeComponent();
            InitializeDataBinding();

            midiFiles.Add("nii");

        }

        void InitializeDataBinding()
        {
            DataContext = this;
            midiListView.ItemsSource = midiFiles;
        }
        private void importMidiBtn_Click(object sender, RoutedEventArgs e)
        {
            midiFiles.Add("IDN");
            Console.WriteLine(midiFiles.Count);
            //var midiFileDialog = new Microsoft.Win32.OpenFileDialog {
            //    Title = "选择MIDI文件",
            //    Filter = "MIDI文件|*.mid",
            //    Multiselect = true
            //};
            //if(midiFileDialog.ShowDialog() == true)
            //{
            //    var midis = new ObservableCollection<string>(midiFileDialog.FileNames);
            //    if(midiFileSet == null)
            //    {
            //        midiFileSet = midis;
            //    }
            //    else
            //    {
            //        midiFileSet = new ObservableCollection<string>() midiFileSet.Union(midis);
            //    }
            //}
        }
    }
}
