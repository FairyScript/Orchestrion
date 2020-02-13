using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orchestrion
{
    /// <summary>
    /// 存储到配置文件的持久化的配置
    /// </summary>
    public class Config
    {
        private static Config config;



        public static Config GetInstance()
        {
            if(config == null)
            {
                config = new Config();
            }

            return config;
        }
    }
}
