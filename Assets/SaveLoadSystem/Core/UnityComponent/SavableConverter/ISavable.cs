namespace SaveLoadSystem.Core.UnityComponent.SavableConverter
{
    public interface ISavable
    {
        void OnSave(SaveDataHandler saveDataHandler);
        void OnLoad(LoadDataHandler loadDataHandler);
    }
}
