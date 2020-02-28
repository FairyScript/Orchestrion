using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using System.Collections.Generic;
using System.Linq;

namespace Orchestrion.Utils
{
    public class MidiFileObject
    {

        private MidiFile midiFile;
        private TempoMap tempoMap;
        private Playback playback;
        private OutputDevice outputDevice;
        private string Path { get; set; }
        public string Name { get; private set; }
        public List<TrackChunk> Tracks { get; private set; }
        public List<string> TrackNames { get; private set; }
        public int SelectedIndex { get; set; } = 0;
        public MidiFileObject(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
        }
        public void ReadFile()
        {
            if (Path != null)
            {
                midiFile = MidiFile.Read(Path);
                tempoMap = midiFile.GetTempoMap();

                Tracks = new List<TrackChunk>();
                TrackNames = new List<string>();
                foreach (var track in midiFile.GetTrackChunks())
                {
                    if (track.ManageNotes().Notes.Count() == 0) continue;

                    Tracks.Add(track);

                    //Add Track name
                    foreach (var trunkEvent in track.Events)
                    {
                        if (trunkEvent is SequenceTrackNameEvent)
                        {
                            TrackNames.Add((trunkEvent as SequenceTrackNameEvent).Text);
                            break;
                        }
                    }
                }
            }
        }

        public void StartPlayback()
        {
            if (!State.state.PlayingFlag)
            {
                State.state.ReadyFlag = false;
                GetPlayback();
                playback?.Start();
            }
        }

        public void StopPlayback()
        {
            playback?.Stop();
            playback?.Dispose();
            outputDevice?.Dispose();
            State.state.PlayingFlag = false;
        }

        public void GetPlayback()
        {
            GetPlayback(SelectedIndex, State.state.IsAccompanyMode);
        }
        public void GetPlayback(int index, bool isAccompanyMode)
        {
            Playback pb;
            if (isAccompanyMode)
            {
                //伴奏模式
                var accompanyTracks = new List<TrackChunk>(Tracks);
                accompanyTracks.RemoveAt(index);
                var events = new List<EventsCollection>();
                foreach (var e in accompanyTracks)
                {
                    events.Add(e.Events);
                }
                outputDevice = OutputDevice.GetByName("Microsoft GS Wavetable Synth");
                pb = new Playback(events, tempoMap, outputDevice);
            }
            else
            {
                //合奏模式
                pb = new Playback(Tracks.ElementAt(index).Events, tempoMap);
                pb.EventPlayed += Playback_EventPlayed;
            }
            pb.Finished += (_, e) => State.state.PlayingFlag = false;
            playback = pb;
        }

        /// <summary>
        /// 控制游戏按键的函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playback_EventPlayed(object sender, MidiEventPlayedEventArgs e)
        {
            switch (e.Event)
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
        }

    }
}
