using Newtonsoft.Json;
using NLog;
using Orchestrion.Components;
using Orchestrion.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Orchestrion
{
    /// <summary>
    /// 存储到配置文件的持久化的配置
    /// </summary>
    public class Config
    {
        static Logger Logger = LogManager.GetCurrentClassLogger();

        public static readonly string configPath = Path.GetDirectoryName(System.Windows.Forms.Application.UserAppDataPath) + "\\config.json";

        public static Dictionary<string, Opcode> SupportOpcode = new Dictionary<string, Opcode>
        {
            
            { "2021.07.31.0000.0000",new Opcode
            {
                Countdown = 0x0189,
                EnsembleReceive = 0x02a5,
                Ping = 0x0
            }
            },
            { "2021.03.29.0000.0000",new Opcode
            {
                Countdown = 0x00b1,
                EnsembleReceive = 0x0369,
                Ping = 0x0
            }
            },
            { "2021.04.22.0000.0000",new Opcode
            {
                Countdown = 0x0106,
                EnsembleReceive = 0x0233,
                Ping = 0x0
            }
            },
            { "2021.05.28.0000.0000",new Opcode
            {
                Countdown = 0x0206,
                EnsembleReceive = 0x01bd,
                Ping = 0x0
            }
            },
        };
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
                return JsonConvert.DeserializeObject<ConfigObject>(text);
            }
            catch (Exception e)
            {
                Logger.Warn(e.Message);
                MessageBox.Show("配置读取失败!已恢复到初始设置");
                config = new ConfigObject();
                Save();
                return config;
            }
        }

        public static void Save()
        {
            File.WriteAllText(configPath, JsonConvert.SerializeObject(_config, Formatting.Indented));
        }

    }

    public class ConfigObject
    {
        public int ConfigVersion { get; set; } = 4;
        public double GameVersion { get; set; } = 5.15;
        /* ------- */
        public bool IsBeta { get; set; } = false;
        public string NtpServer { get; set; } = "ntp.aliyun.com";
        public Dictionary<int, int> KeyMap { get; set; } = new Dictionary<int, int>
        {
            { 48, 90 },
            { 49, 88 },
            { 50, 67 },
            { 51, 86 },
            { 52, 66 },
            { 53, 78 },
            { 54, 77 },
            { 55, 188 },
            { 56, 190 },
            { 57, 191 },
            { 58, 219 },
            { 59, 221 },
            { 60, 81 },
            { 61, 50 },
            { 62, 87 },
            { 63, 51 },
            { 64, 69 },
            { 65, 82 },
            { 66, 53 },
            { 67, 84 },
            { 68, 54 },
            { 69, 89 },
            { 70, 55 },
            { 71, 85 },
            { 72, 65 },
            { 73, 83 },
            { 74, 68 },
            { 75, 70 },
            { 76, 71 },
            { 77, 72 },
            { 78, 74 },
            { 79, 75 },
            { 80, 76 },
            { 81, 186 },
            { 82, 222 },
            { 83, 189 },
            { 84, 187 }
        };
        public Dictionary<string, KeyCombination> HotkeyBindings { get; set; } = new Dictionary<string, KeyCombination>
        {
            {"StartPlay",new KeyCombination{Key = Key.F10,ModifierKeys = ModifierKeys.Control } },
            {"StopPlay",new KeyCombination{Key = Key.F11,ModifierKeys = ModifierKeys.Control } },
        };

        public ConfigObject() { }
    }

}
