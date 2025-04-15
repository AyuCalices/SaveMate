using System.Collections.Generic;

namespace SaveLoadSystem.Core.Components.Groups.Interface
{
    internal interface INestableLoadGroupHandler
    {
        List<ILoadableGroupHandler> GetLoadableGroupHandlers();
    }
}
