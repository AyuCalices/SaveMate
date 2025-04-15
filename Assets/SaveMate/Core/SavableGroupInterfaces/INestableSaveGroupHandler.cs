using System.Collections.Generic;

namespace SaveMate.Core.SavableGroupInterfaces
{
    internal interface INestableSaveGroupHandler
    {
        List<ISavableGroupHandler> GetSavableGroupHandlers();
    }
}
