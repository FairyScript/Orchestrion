using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Orchestrion
{
    public class MidiFileObject
    {
        public string Name { get; private set; }
        private string Path { get; set; }

        private MidiFile midiFile;
        public List<TrackChunk> Tracks { get; private set; }
        public List<string> TrackNames { get; set; }

        public MidiFileObject(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
        }
        public void ReadFile()
        {
            if(Path != null)
            {
                midiFile = MidiFile.Read(Path);

                Tracks = new List<TrackChunk>();
                TrackNames = new List<string>();

                foreach (var track in midiFile.GetTrackChunks())
                    if (track.ManageNotes().Notes.Count() != 0)
                    {
                        Tracks.Add(track);

                        foreach (var trunkEvent in track.Events)
                            if (trunkEvent is SequenceTrackNameEvent)
                            {
                                TrackNames.Add((trunkEvent as SequenceTrackNameEvent).Text);
                            }
                    }
            }
        }
    }

}
