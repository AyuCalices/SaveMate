using System;

namespace SaveLoadSystem.Core.Converter
{
    public abstract class BaseConverter<T> : IConvertable
    {
        public virtual bool CanConvert(Type type, out IConvertable convertable)
        {
            if (typeof(T).IsAssignableFrom(type) || type is T)
            {
                convertable = this;
                return true;
            }
            
            convertable = default;
            return false;
        }
        
        public void OnSave(object data, SaveDataHandler saveDataHandler)
        {
            OnSave((T)data, saveDataHandler);
        }

        protected abstract void OnSave(T data, SaveDataHandler saveDataHandler);


        public object CreateInstanceForLoad(SimpleLoadDataHandler loadDataHandler)
        {
            return OnCreateInstanceForLoading(loadDataHandler);
        }
        
        protected abstract T OnCreateInstanceForLoading(SimpleLoadDataHandler loadDataHandler);
        
        public void OnLoad(object data, LoadDataHandler loadDataHandler)
        {
            OnLoad((T)data, loadDataHandler);
        }

        protected abstract void OnLoad(T data, LoadDataHandler loadDataHandler);
    }
}
