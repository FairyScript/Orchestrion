using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows.Forms;

namespace Orchestrion
{
    /// <summary>
    /// 存储到配置文件的持久化的配置
    /// </summary>
    public class Config
    {
        public enum OpCodeEnum
        {
            COUNTDOWN,
            ENSEMBLE_RECEIVE
        }

        private static string configPath = Application.UserAppDataPath + "\\config.json";

        private static ConfigObject _config;
        public static ConfigObject config
        {
            get
            {
                if(_config == null) _config = ReadConfig();
                return _config;
            }
            set
            {
                if (value != _config)
                {
                    _config = value;
                    File.WriteAllText(configPath,JsonConvert.SerializeObject(value));
                }
            }
        }

        static ConfigObject ReadConfig()
        {
            try
            {
                string text = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<ConfigObject>(text);
            }
            catch (Exception)
            {
                return ConfigObject.GetDefaultConfig();
            }
        }

    }

    public class ConfigObject
    {
        public bool IsBeta { get; set; }
        public string NtpServer { get; set; }
        public Dictionary<int, int> KeyMap { get; set; }
        //public Dictionary<string, ModifierKeys>
        public Dictionary<Config.OpCodeEnum, uint> OpCode { get; set; }
        ConfigObject() { }
        public static ConfigObject GetDefaultConfig()
        {
            return new ConfigObject
            {
                IsBeta = false,
                NtpServer = "ntp.aliyun.com",
                KeyMap = new Dictionary<int, int>
                {
                    {48, 90},
                    {49, 88},
                    {50, 67},
                    {51, 86},
                    {52, 66},
                    {53, 78},
                    {54, 77},
                    {55, 188},
                    {56, 190},
                    {57, 191},
                    {58, 219},
                    {59, 221},
                    {60, 81},
                    {61, 50},
                    {62, 87},
                    {63, 51},
                    {64, 69},
                    {65, 82},
                    {66, 53},
                    {67, 84},
                    {68, 54},
                    {69, 89},
                    {70, 55},
                    {71, 85},
                    {72, 65},
                    {73, 83},
                    {74, 68},
                    {75, 70},
                    {76, 71},
                    {77, 72},
                    {78, 74},
                    {79, 75},
                    {80, 76},
                    {81, 186},
                    {82, 222},
                    {83, 189},
                    {84, 187}
                },
                OpCode = new Dictionary<Config.OpCodeEnum, uint>
                {
                    {Config.OpCodeEnum.COUNTDOWN,0x036b },
                    {Config.OpCodeEnum.ENSEMBLE_RECEIVE,0x02eb }
                }
            };
        }
    }
}
