namespace SaveLoadCore.Core.SavableConverter
{
    public interface ISavable
    {
        void OnSave(SaveDataHandler saveDataHandler);
        void OnLoad(LoadDataHandler loadDataHandler);
    }
}
