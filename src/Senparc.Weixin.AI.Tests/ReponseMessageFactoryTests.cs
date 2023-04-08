using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.CO2NET.Extensions;
using Senparc.Weixin.AI;
using Senparc.Weixin.AI.Tests.BaseSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Weixin.AI.Tests
{
    [TestClass()]
    public class ReponseMessageFactoryTests : KernelTestBase
    {
        [TestMethod()]
        public async Task GetResponseMessageTest()
        {
            var serviceProvider = BaseTest.serviceProvider;
            var handler = serviceProvider.GetRequiredService<IAiHandler>()
                            as SemanticAiHandler;

            var openId = "JeffreySu";

            var factory = new ReponseMessageFactory();

            var text = "I want to know which program language is the best one?";
            await Console.Out.WriteLineAsync("ASK: "+ text);
            var responseMessageType = await factory.GetResponseMessageAsync(handler, openId, text);
            //Assert.AreEqual("ResponseMessageText", responseMessageType.Trim());
            Assert.IsFalse(responseMessageType.IsNullOrEmpty());
            await Console.Out.WriteLineAsync();

            text = "Create a logo picture with black ground, looks like a plan, show it's power.";
            await Console.Out.WriteLineAsync("ASK: "+ text);
            responseMessageType = await factory.GetResponseMessageAsync(handler, openId, text);
            Assert.IsFalse(responseMessageType.IsNullOrEmpty());
            //Assert.AreEqual("ResponseMessageImage", responseMessageType.Trim());
            await Console.Out.WriteLineAsync();
        }
    }
}