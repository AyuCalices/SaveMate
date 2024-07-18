namespace SaveLoadSystem.Core.Component.SavableConverter
{
    public interface ISavable
    {
        void OnSave(SaveDataHandler saveDataHandler);
        void OnLoad(LoadDataHandler loadDataHandler);
    }
}
