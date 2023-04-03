using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.Tests;
using Senparc.CO2NET.RegisterServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.AI.Kernel.Tests
{
    public class KernelTestBase : BaseTest
    {
        static Action<IRegisterService> RegisterAction = r =>
        {
            r.UseSenparcAI(null);
        };

        public KernelTestBase() : base(RegisterAction)
        {

        }

    }
}
