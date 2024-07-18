using System.Runtime.Serialization;

namespace SaveLoadSystem.Utility
{
    public static class TypeUtility
    {
        public static bool TryConvertTo<T>(object instance, out T convertedType) where T : class
        {
            if (instance is T validInstance)
            {
                convertedType = validInstance;
                return true;
            }

            convertedType = null;
            return false;
        }
        
        public static bool IsSerializable(this object obj)
        {
            var type = obj.GetType();
            
            // Check if the type is marked with the [Serializable] attribute
            if (type.IsSerializable)
            {
                return true;
            }

            // Check if the type implements the ISerializable interface
            if (typeof(ISerializable).IsAssignableFrom(type))
            {
                return true;
            }

            return false;
        }
    }
}
