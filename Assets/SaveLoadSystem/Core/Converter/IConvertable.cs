using System;

namespace SaveLoadSystem.Core.Converter
{
    public interface IConvertable
    {
        bool TryGetConverter(Type type, out IConvertable convertable);
        void OnSave(object data, SaveDataHandler saveDataHandler);
        void OnLoad(LoadDataHandler loadDataHandler);
    }
}
