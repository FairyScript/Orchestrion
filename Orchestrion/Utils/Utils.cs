using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace Orchestrion.Utils
{
    public static class Utils
    {
        public static T GetInstance<T>(string typeName)
        {
            return (T)Activator.CreateInstance(Type.GetTypeFromProgID(typeName));
        }

        public static List<uint> FindFFProcess()
        {
            var processes = new List<Process>();
            var idList = new List<uint>();
            processes.AddRange(Process.GetProcessesByName("ffxiv"));
            processes.AddRange(Process.GetProcessesByName("ffxiv_dx11"));
            foreach (var item in processes)
            {
                idList.Add((uint)item.Id);
            }
            return idList;
        }

        public static List<T> GetChildObjects<T>(DependencyObject obj, Type typename) where T : FrameworkElement
        {
            DependencyObject child;
            List<T> childList = new List<T>();
            for (int i = 0; i <= VisualTreeHelper.GetChildrenCount(obj) - 1; i++)
            {
                child = VisualTreeHelper.GetChild(obj, i);
                if (child is T && (((T)child).GetType() == typename))
                {
                    childList.Add((T)child);
                }
                childList.AddRange(GetChildObjects<T>(child, typename));
            }
            return childList;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}
