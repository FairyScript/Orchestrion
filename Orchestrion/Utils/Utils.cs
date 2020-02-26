using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
