using Senparc.AI.Kernel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Weixin.AI.Senparc.AI.Kernel
{
    /// <summary>
    /// Senparc.AI.Kernel 配置
    /// </summary>
    public class Config
    {
        /// <summary>
        /// 当前配置
        /// </summary>
        public static SenparcAiSettings SenparcAiSettings { get; set; }

        static Config()
        {
            //初始化 SenaprcAiSettings
            SenparcAiSettings = new SenparcAiSettings();
        }
    }
}
