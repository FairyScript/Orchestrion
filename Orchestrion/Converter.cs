﻿using Orchestrion.Utils;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;

namespace Orchestrion
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    [ValueConversion(typeof(KeyCombination), typeof(string))]
    public class HotKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Build the shortcut key name.
            var val = value as KeyCombination;
            StringBuilder shortcutText = new StringBuilder();
            if ((val.ModifierKeys & ModifierKeys.Control) != 0)
            {
                shortcutText.Append("Ctrl+");
            }
            if ((val.ModifierKeys & ModifierKeys.Shift) != 0)
            {
                shortcutText.Append("Shift+");
            }
            if ((val.ModifierKeys & ModifierKeys.Alt) != 0)
            {
                shortcutText.Append("Alt+");
            }
            shortcutText.Append(val.Key.ToString());

            // Update the text box.
            return shortcutText.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(Key), typeof(string))]
    public class KeyMapConverter : IValueConverter
    {

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int ToUnicode(
        uint virtualKeyCode,
        uint scanCode,
        byte[] keyboardState,
        StringBuilder receivingBuffer,
        int bufferSize,
        uint flags
        );
        private KeyConverter kc = new KeyConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Build the shortcut key name.


            // Update the text box.
            return kc.ConvertToString((Key)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
