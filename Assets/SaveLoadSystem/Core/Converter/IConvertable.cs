using System;

namespace SaveLoadSystem.Core.Converter
{
    public interface IConvertable
    {
        bool CanConvert(Type type, out IConvertable convertable);
        void OnSave(object data, SaveDataHandler saveDataHandler);
        object OnLoad(LoadDataHandler loadDataHandler);
    }
}
