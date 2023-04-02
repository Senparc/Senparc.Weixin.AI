using Senparc.AI.Kernel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.AI.Kernel
{
    /// <summary>
    /// Senparc.AI.Kernel 基础配置
    /// </summary>
    public class SenparcAiSetting
    {
        /// <summary>
        /// 是否处于调试状态
        /// </summary>
        public bool IsDebug { get; set; }

        /// <summary>
        /// 是否使用 Azure OpenAI
        /// </summary>
        public bool UseAzureOpenAI => AiPlatform == AiPlatform.AzureOpenAI;

        public AiPlatform AiPlatform { get; set; }

        /// <summary>
        /// Azure OpenAI Endpoint
        /// </summary>
        public string AzureEndpoint { get; set; }
        /// <summary>
        /// Azure OpenAI 或 OpenAI API Key
        /// </summary>
        public string ApiKey { get; set; }
        /// <summary>
        /// OpenAI API Orgaization ID
        /// </summary>
        public string OrgaizationId { get; set; }


        public SenparcAiSetting() { }
    }
}
