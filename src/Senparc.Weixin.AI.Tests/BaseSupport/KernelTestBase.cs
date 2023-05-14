using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI;
using Senparc.CO2NET.RegisterServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Weixin.AI.Tests.BaseSupport
{
    public class KernelTestBase:BaseTest
    {
        static Action<IRegisterService> RegisterAction = r =>
        {
            r.UseSenparcAI(BaseTest._senparcAiSetting);
        };

        static Func<IConfigurationRoot, SenparcAiSetting> getSenparcAiSettingFunc = config =>
        {
            var senparcAiSetting = new SenparcAiSetting() { IsDebug = true };
            config.GetSection("SenparcAiSetting").Bind(_senparcAiSetting);
            return senparcAiSetting;
        };

        static Action<ServiceCollection> serviceAction = services =>
        {
            services.AddScoped<IAiHandler, SemanticAiHandler>();
        };
        public KernelTestBase() : base(RegisterAction, getSenparcAiSettingFunc, serviceAction)
        {

        }
    }
}
