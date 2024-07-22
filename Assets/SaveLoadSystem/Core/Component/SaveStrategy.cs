namespace SaveLoadSystem.Core.Component
{
    public enum SaveStrategy
    {
        NotSupported,
        UnityObject,
        AutomaticSavable,
        CustomSavable,
        CustomConvertable,
        Serializable
    }
}
