using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Orchestrion.Utils
{
    static class Hotkey
    {
        #region 系统api
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, ModifierKeys fsModifiers, uint vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion

        static readonly Dictionary<string, KeyCombination> hotkeys = new Dictionary<string, KeyCombination>();
        static readonly Dictionary<int, string> keymap = new Dictionary<int, string>();
        static IntPtr handle;
        static int index = 108;
        const int WM_HOTKEY = 0x312;

        public static void Initial(IntPtr ptr)
        {
            handle = ptr;
            var _hwndSource = HwndSource.FromHwnd(handle);
            _hwndSource.AddHook(WndProc);
        }

        public static void Add(string name, KeyCombination combination)
        {
            if (handle == null) throw new Exception("Hotkey: 窗口句柄尚未初始化!");
            int i = index++;
            try
            {
                bool result = RegisterHotKey(handle, i, combination.ModifierKeys, (uint)KeyInterop.VirtualKeyFromKey(combination.Key));
                if (!result) throw new HotKeyAlreadyExistsException("快捷键已被占用");
                combination.ID = i;
                hotkeys.Add(name, combination);
                keymap.Add(i, name);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static void Add(string name, KeyCombination combination,Action action)
        {
            var newCombination = new KeyCombination(combination);
            newCombination.OnProcess += action;
            Add(name, newCombination);
        }
        public static void Add(string name,Key key,ModifierKeys modifier)
        {
            Add(name, new KeyCombination
            {
                Key = key,
                ModifierKeys = modifier
            });
        }
        public static void Add(string name, Key key, ModifierKeys modifier,Action action)
        {
            KeyCombination combination = new KeyCombination
            {
                Key = key,
                ModifierKeys = modifier
            };
            combination.OnProcess += action;

            Add(name, combination);
        }

        public static void Remove(string name)
        {
            KeyCombination val;
            if (hotkeys.TryGetValue(name, out val))
            {
                int id = val.ID;
                UnregisterHotKey(handle, id);
                hotkeys.Remove(name);
                keymap.Remove(id);
            }
            
        }

        public static void RemoveAll()
        {
            foreach (var item in keymap.Keys)
            {
                UnregisterHotKey(handle, item);
            }
            hotkeys.Clear();
            keymap.Clear();
        }

        /// <summary> 
        /// 快捷键消息处理 
        /// </summary> 
        static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (keymap.TryGetValue(id, out var name))
                {
                    hotkeys[name]?.Invoke();
                }
            }
            return IntPtr.Zero;
        }

        class HotKeyAlreadyExistsException : ApplicationException
        {
            public HotKeyAlreadyExistsException() { }

            public HotKeyAlreadyExistsException(string message)
            : base(message)
            { }

            public HotKeyAlreadyExistsException(string message, Exception inner)
            : base(message, inner)
            { }
        }
    }

    public class KeyCombination
    {
        public int ID { get; set; }
        public Key Key { get; set; }
        public ModifierKeys ModifierKeys { get; set; }
        public event Action OnProcess;
        public KeyCombination() { }
        public KeyCombination(KeyCombination value)
        {
            Key = value.Key;
            ModifierKeys = value.ModifierKeys;
        }
        public KeyCombination(Key key,ModifierKeys modifier)
        {
            Key = key;
            ModifierKeys = modifier;
        }
        public void Invoke()
        {
            OnProcess?.Invoke();
        }
    }
}
