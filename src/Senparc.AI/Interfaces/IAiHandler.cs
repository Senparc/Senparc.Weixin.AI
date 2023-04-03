using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.AI.Interfaces
{
    public interface IAiHandler<T>
        where T : IAiResult
    {
        public T Run(IAiRequest request); 
    }
}
