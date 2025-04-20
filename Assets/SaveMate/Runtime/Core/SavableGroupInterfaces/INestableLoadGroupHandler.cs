using System.Collections.Generic;

namespace SaveMate.Core.SavableGroupInterfaces
{
    internal interface INestableLoadGroupHandler
    {
        List<ILoadableGroupHandler> GetLoadableGroupHandlers();
    }
}
