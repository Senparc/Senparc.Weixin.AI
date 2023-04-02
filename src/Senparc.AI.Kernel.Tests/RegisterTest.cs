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
            Assert.AreEqual(true, senparcAiSetting.UseAzureOpenAI);
            Assert.AreEqual("https://xxx", senparcAiSetting.AzureEndpoint);
        }
    }
}
