using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET;
using Senparc.Weixin.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Senparc.AI.Kernel;

namespace Senparc.Weixin.AI
{
    /// <summary>
    /// 项目启动注册
    /// </summary>
    public static class Register
    {

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="registerService"></param>
        /// <param name="senparcAiSetting"></param>
        /// <returns></returns>
        public static IRegisterService UseSenparcAI(this IRegisterService registerService, SenparcAiSettings senparcAiSetting = null)
        {
            if (senparcAiSetting == null)
            {
                global::Senparc.AI.Kernel.Config.SenparcAiSettings ??= new SenparcAiSettings();
            }
            else
            {
                global::Senparc.AI.Kernel.Config.SenparcAiSettings = senparcAiSetting;
            }

            return registerService;
        }
    }
}
