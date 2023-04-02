using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Senparc.AI.Kernel.Helpers
{
    /// <summary>
    /// SemanticKernel 帮助类
    /// </summary>
    public class SemanticKernelHelper
    {
        public SemanticKernelHelper() { }


        public string GetServiceId(string userId, string modelName)
        {
            return $"{userId}-{modelName}";
        }

        public IKernel GetKernel()
        {
            IKernel kernel = Microsoft.SemanticKernel.Kernel.Builder.Build();
            return kernel;
        }

        public IKernel Config(string userId, string modelName, IKernel kernel=null) {
        kernel ??= GetKernel();

            var serviceId = GetServiceId(userId, modelName);    
            var senparcAiSetting = Senparc.AI.Kernel.Config.SenparcAiSettings;
            switch (senparcAiSetting.AiPlatform)
            {
                case AiPlatform.OpenAI:
                    kernel.Config.AddAzureOpenAITextCompletion(serviceId, modelName, senparcAiSetting.AzureEndpoint, senparcAiSetting.ApiKey);
                    break;
                case AiPlatform.AzureOpenAI:
                    kernel.Config.AddOpenAITextCompletion(serviceId, modelName, senparcAiSetting.ApiKey, senparcAiSetting.OrgaizationId);
                    break;
                default:
                    throw new Senparc.AI.Kernel.Exceptions.SenparcAiException($"没有处理当前 {nameof(AiPlatform)} 类型：{senparcAiSetting.AiPlatform}");
            }

            return kernel;
        }
    }
}
