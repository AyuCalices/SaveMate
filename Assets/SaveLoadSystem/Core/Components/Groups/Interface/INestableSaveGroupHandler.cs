using System.Collections.Generic;

namespace SaveLoadSystem.Core.Components.Groups.Interface
{
    internal interface INestableSaveGroupHandler
    {
        List<ISavableGroupHandler> GetSavableGroupHandlers();
    }
}
