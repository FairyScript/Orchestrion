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

        private string Path { get; set; }
        public string Name { get; private set; }
        public List<TrackChunk> Tracks { get; private set; }
        public List<string> TrackNames { get; private set; }

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

        public Playback GetPlayback()
        {
            return midiFile.GetPlayback(OutputDevice.GetByName("Microsoft GS Wavetable Synth"));//全部音轨的试听
        }

        public Playback GetPlayback(int index,bool isAccompanyMode)
        {
            Playback playback;
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
                playback = new Playback(events,tempoMap, OutputDevice.GetByName("Microsoft GS Wavetable Synth"));
            }
            else
            {
                //合奏模式
                playback = new Playback(Tracks.ElementAt(index).Events, tempoMap);
                playback.EventPlayed += Playback_EventPlayed;
            }
            return playback;
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
