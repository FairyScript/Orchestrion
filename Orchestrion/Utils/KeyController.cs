using Melanchall.DryWetMidi.Common;
using NLog;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Orchestrion.Utils
{
    public class KeyController
    {
        #region DLL Import
        [DllImport("User32.dll")]
        private static extern void keybd_event(Keys bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);


        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, uint lParam);
        #endregion
        Logger Logger = LogManager.GetCurrentClassLogger();

        //PostMessage 方法
        public IntPtr gameWindowHandle;//TODO:  这是危险的,需要更好的解决方案
        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;

        public Action<SevenBitNumber> Press;
        public Action<SevenBitNumber> Release;
        public Action Reset;
        public KeyController()
        {
            Press += KeyboardPress;
            Release += KeyboardRelease;
            Reset += KeyboardReset;
        }
        public KeyController(Process p)
        {
            if (p == null) throw new Exception("传入的Process无效!");
            gameWindowHandle = p.MainWindowHandle;

            Press += PostPress;
            Release += PostRelease;
            Reset += PostReset;
        }

        private void PostPress(SevenBitNumber noteNumber)
        {
            if (noteNumber <= 84 && noteNumber >= 48)
            {
                Keys keycode = (Keys)Config.config.KeyMap[noteNumber];
                PostMessage(gameWindowHandle, WM_KEYDOWN, (int)keycode, 0x001F0001);
            }
            else
            {
                Logger.Warn($"非法的Note! {noteNumber}");
            }
        }
        private void PostRelease(SevenBitNumber noteNumber)
        {
            if (noteNumber <= 84 && noteNumber >= 48)
            {
                Keys keycode = (Keys)Config.config.KeyMap[noteNumber];
                PostMessage(gameWindowHandle, WM_KEYUP, (int)keycode, 0x001F0001);
            }
                      }
        private void PostReset()
        {
            foreach (var keycode in Config.config.KeyMap.Values)
            {
                PostMessage(gameWindowHandle, WM_KEYUP, (int)keycode, 0x001F0001);
            }
        }

        //keybd_event 方法
        private void KeyboardPress(SevenBitNumber noteNumber)
        {
            if (noteNumber <= 84 && noteNumber >= 48)
            {
                Keys keycode = (Keys)Config.config.KeyMap[noteNumber];
                keybd_event(keycode, 0, 0, 0);
            }
        }

        private void KeyboardRelease(SevenBitNumber noteNumber)
        {
            if (noteNumber <= 84 && noteNumber >= 48)
            {
                Keys keycode = (Keys)Config.config.KeyMap[noteNumber];
                keybd_event(keycode, 0, 2, 0);
            }
        }

        private void KeyboardReset()
        {
            foreach (var item in Config.config.KeyMap.Values)
            {
                keybd_event((Keys)item, 0, 2, 0);
            }
        }
    }
}
