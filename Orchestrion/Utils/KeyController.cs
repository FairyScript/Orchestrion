﻿using Melanchall.DryWetMidi.Common;
using System.Runtime.InteropServices;
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
            if (noteNumber <= 84 && noteNumber >= 48)
            {
                Keys keycode = (Keys)Config.config.KeyMap[noteNumber];
                keybd_event(keycode, 0, 2, 0);
            }
        }

        internal static void Reset()
        {
            for (int i = 48; i <= 84; i++)
            {
                keybd_event((Keys)i, 0, 2, 0);
            }
        }
    }
}
