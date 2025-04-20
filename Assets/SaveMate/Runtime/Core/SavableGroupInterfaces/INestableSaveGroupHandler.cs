using System.Collections.Generic;

namespace SaveMate.Runtime.Core.SavableGroupInterfaces
{
    internal interface INestableSaveGroupHandler
    {
        List<ISavableGroupHandler> GetSavableGroupHandlers();
    }
}
