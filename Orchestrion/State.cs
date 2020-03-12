using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Orchestrion
{
    public class State : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private State() { }
        public static readonly State state = new State();

        public DateTime TimeWhenPlay { get; set; } = DateTime.MinValue;
        public TimeSpan SystemTimeOffset { get; set; } = TimeSpan.Zero;
        public TimeSpan NetTimeOffset { get; set; } = TimeSpan.Zero;

        private bool _playingFlag;
        public bool PlayingFlag
        {
            get => _playingFlag;
            set
            {
                if (value != _playingFlag)
                {
                    _playingFlag = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _readyFlag;
        public bool ReadyFlag
        {
            get => _readyFlag;
            set
            {
                if (value != _readyFlag)
                {
                    _readyFlag = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _isCaptureFlag;
        public bool IsCaptureFlag
        {
            get => _isCaptureFlag;
            set
            {
                if (value != _isCaptureFlag)
                {
                    _isCaptureFlag = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _autoPing;
        public bool AutoPing
        {
            get => _autoPing;
            set
            {
                if (value != _autoPing)
                {
                    _autoPing = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _isAccompanyMode;
        public bool IsAccompanyMode
        {
            get => _isAccompanyMode;
            set
            {
                if (value != _isAccompanyMode)
                {
                    _isAccompanyMode = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _midiDeviceConnected;
        public bool MidiDeviceConnected
        {
            get => _midiDeviceConnected;
            set
            {
                if (value != _midiDeviceConnected)
                {
                    _midiDeviceConnected = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }
}
