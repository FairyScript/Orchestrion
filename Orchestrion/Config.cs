using Newtonsoft.Json;
using Orchestrion.Components;
using Orchestrion.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;

namespace Orchestrion
{
    /// <summary>
    /// 存储到配置文件的持久化的配置
    /// </summary>
    public class Config
    {


        private static readonly string configPath = Path.GetDirectoryName(Application.UserAppDataPath) + "\\config.json";

        private static ConfigObject _config;
        public static ConfigObject config
        {
            get
            {
                if (_config == null) _config = ReadConfig();
                return _config;
            }
            set
            {
                if (value != _config)
                {
                    _config = value;
                }
            }
        }

        static ConfigObject ReadConfig()
        {
            try
            {
                string text = File.ReadAllText(configPath);
                ConfigObject config = JsonConvert.DeserializeObject<ConfigObject>(text);
                if (config.ConfigVersion != ConfigObject.version) throw new Exception("ConfigFile version changed");
                return config;
            }
            catch (Exception e)
            {
                TopmostMessageBox.Show("配置文件已经初始化!");
                Logger.Warning(e.Message);
                config = ConfigObject.GetDefaultConfig();
                Save();
                return config;
            }
        }

        public static void Save()
        {
            File.WriteAllText(configPath, JsonConvert.SerializeObject(_config));
        }

    }

    public class ConfigObject
    {
        public enum OpCodeEnum
        {
            Countdown,
            EnsembleReceive
        }

        /// <summary>
        /// Config File version
        /// </summary>
        public const int version = 2;
        public int ConfigVersion { get; set; } = version;
        /* ------- */
        public bool IsBeta { get; set; }
        public string NtpServer { get; set; }
        public Dictionary<int, int> KeyMap { get; set; }
        public Dictionary<string, KeyCombination> HotkeyBindings { get; set; }
        public Dictionary<OpCodeEnum, uint> OpCode { get; set; }

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
                OpCode = new Dictionary<OpCodeEnum, uint>
                {
                    {OpCodeEnum.Countdown,0x01ac },
                    {OpCodeEnum.EnsembleReceive,0x02d2 }
                },
                HotkeyBindings = new Dictionary<string, KeyCombination>
                {
                    {"StartPlay",new KeyCombination{Key = Key.F10,ModifierKeys = ModifierKeys.Control } },
                    {"StopPlay",new KeyCombination{Key = Key.F11,ModifierKeys = ModifierKeys.Control } }
                }
            };
        }
    }

}
