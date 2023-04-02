using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.AI.Kernel
{
    /// <summary>
    /// Senparc.AI.Kernel 基础配置
    /// </summary>
    public class SenparcAiSettings
    {
        /// <summary>
        /// 是否使用 Azure
        /// </summary>
        public bool UserAzure { get; set; }
        /// <summary>
        /// 是否使用 Azure OpenAI
        /// </summary>
        public string UseAzureOpenAI { get; set; }
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

        public SenparcAiSettings() { }
    }
}
