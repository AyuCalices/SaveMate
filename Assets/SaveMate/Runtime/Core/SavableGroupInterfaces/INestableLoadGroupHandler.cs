using System.Collections.Generic;

namespace SaveMate.Runtime.Core.SavableGroupInterfaces
{
    internal interface INestableLoadGroupHandler
    {
        List<ILoadableGroupHandler> GetLoadableGroupHandlers();
    }
}
