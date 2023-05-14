using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET;
using Senparc.Weixin.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Senparc.AI;
using Senparc.CO2NET.Cache;
using Microsoft.AspNetCore.Builder;
using Senparc.AI.Kernel;
using Microsoft.Extensions.Configuration;
using Senparc.AI.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Weixin.AI
{
    /// <summary>
    /// 项目启动注册
    /// </summary>
    public static class Register
    {
        public static IServiceCollection AddSenparcWeixinAI(this IServiceCollection services, IConfigurationRoot config, SenparcAiSetting? senparcAiSetting = null)
        {

            if (senparcAiSetting == null)
            {
                senparcAiSetting = new Senparc.AI.Kernel.SenparcAiSetting();
                config.GetSection("SenparcAiSetting").Bind(senparcAiSetting);
            }

            try
            {
                Senparc.AI.Register.UseSenparcAI(null, senparcAiSetting);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            services.AddScoped<IAiHandler, SemanticAiHandler>();

            return services;
        }

    }
}
