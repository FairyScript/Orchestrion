﻿using Melanchall.DryWetMidi.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Orchestrion.Utils
{
    class KeyController
    {
        [DllImport("User32.dll")]
        private static extern void keybd_event(Keys bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        internal static void KeyboardPress(SevenBitNumber noteNumber)
        {
            if (noteNumber <= 84 && noteNumber >= 48)
            {
                Keys keycode = (Keys)Config.config.KeyMap[noteNumber];
                keybd_event(keycode, 0, 0, 0);
            }
        }

        internal static void KeyboardRelease(SevenBitNumber noteNumber)
        {
            Keys keycode = (Keys)Config.config.KeyMap[noteNumber];
            keybd_event(keycode, 0, 2, 0);
        }
    }
}
