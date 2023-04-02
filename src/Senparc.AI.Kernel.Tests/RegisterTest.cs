using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.AI.Kernel.Tests
{
    [TestClass]
    public class RegisterTest:BaseTest
    {
        [TestMethod]
        public void GlobalRegisterTest() {
            var senparcAiSetting = BaseTest._senparcAiSetting; // Senparc.AI.Kernel.Config.SenparcAiSettings;
            Assert.IsNotNull(senparcAiSetting);
            Assert.AreEqual(AiPlatform.AzureOpenAI, senparcAiSetting.AiPlatform);
            Assert.AreEqual(true, senparcAiSetting.UseAzureOpenAI);
            Assert.IsFalse(senparcAiSetting.AzureEndpoint.IsNullOrEmpty());
            Assert.IsFalse(senparcAiSetting.ApiKey.IsNullOrEmpty());
            Assert.IsFalse(senparcAiSetting.OrgaizationId.IsNullOrEmpty());
        }
    }
}
