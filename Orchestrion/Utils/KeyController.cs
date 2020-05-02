using Melanchall.DryWetMidi.Common;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Orchestrion.Utils
{
    class KeyController
    {
        #region DLL Import
        [DllImport("User32.dll")]
        private static extern void keybd_event(Keys bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);


        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, uint lParam);
        #endregion

        //PostMessage 方法
        public static IntPtr gameWindowHandle;
        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;
        internal static void SetHandle(Process p)
        {
            gameWindowHandle = p.MainWindowHandle;
        }
        internal static void PostPress(SevenBitNumber noteNumber)
        {
            Keys keycode = (Keys)Config.config.KeyMap[noteNumber];
            PostMessage(gameWindowHandle, WM_KEYDOWN, (int)keycode, 0x001F0001);
        }
        internal static void PostRelease(SevenBitNumber noteNumber)
        {
            Keys keycode = (Keys)Config.config.KeyMap[noteNumber];
            PostMessage(gameWindowHandle, WM_KEYUP, (int)keycode, 0x001F0001);
        }

        //keybd_event 方法
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
            foreach (var item in Config.config.KeyMap.Values)
            {
                keybd_event((Keys)item, 0, 2, 0);
            }
            //for (int i = 48; i <= 84; i++)
            //{
            //    Keys keycode = (Keys)Config.config.KeyMap[i];
            //    keybd_event(keycode, 0, 2, 0);
            //}
        }
    }
}
