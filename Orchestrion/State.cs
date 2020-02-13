using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orchestrion
{
    public class State
    {
        private static State state;

        public bool ReadyFlag { get; set; }
        public bool RunningFlag { get; set; }
        public ObservableCollection<string> MidiFiles { get; set; }

        public static State GetInstance()
        {
            if (state == null)
            {
                state = new State
                {
                    MidiFiles = new ObservableCollection<string>()
                };
            }

            return state;
        }
    }
}
