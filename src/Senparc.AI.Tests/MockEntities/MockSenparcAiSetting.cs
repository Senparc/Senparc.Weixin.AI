using Senparc.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.AI.Kernel.Tests.MockEntities
{
    public class MockSenparcAiSetting:ISenparcAiSetting
    {
        /// <inheritdoc/>
        public bool IsDebug { get; set; }

        /// <inheritdoc/>
        public bool UseAzureOpenAI => AiPlatform == AiPlatform.AzureOpenAI;

        /// <inheritdoc/>
        public AiPlatform AiPlatform { get; set; }

        /// <inheritdoc/>
        public string AzureEndpoint { get; set; }

        /// <inheritdoc/>
        public string ApiKey { get; set; }

        /// <inheritdoc/>
        public string OrgaizationId { get; set; }
    }
}
