using System.Collections.Generic;
using SaveLoadSystem.Core.Component;
using SaveLoadSystem.Core.DataTransferObject;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace SaveLoadSystem.Core
{
    public class SaveDataHandler
    {
        private readonly SaveDataBuffer _objectSaveDataBuffer;
        private readonly GuidPath _originGuidPath;
        private readonly Dictionary<GuidPath, SaveDataBuffer> _saveDataBuffer;
        private readonly SavableObjectsLookup _savableObjectsLookup;
        private readonly Dictionary<object, GuidPath> _objectReferenceLookup;
        private readonly int _currentIndex;

        public SaveDataHandler(Dictionary<GuidPath, SaveDataBuffer> saveDataBuffer, SaveDataBuffer objectSaveDataBuffer, GuidPath originGuidPath, 
            SavableObjectsLookup savableObjectsLookup, Dictionary<object, GuidPath> objectReferenceLookup, int currentIndex)
        {
            _objectSaveDataBuffer = objectSaveDataBuffer;
            _originGuidPath = originGuidPath;
            _saveDataBuffer = saveDataBuffer;
            _savableObjectsLookup = savableObjectsLookup;
            _objectReferenceLookup = objectReferenceLookup;
            _currentIndex = currentIndex;
        }

        public void AddSerializable(string uniqueIdentifier, object obj)
        {
            _objectSaveDataBuffer.CustomSerializableSaveData.Add(uniqueIdentifier, JToken.FromObject(obj));
        }

        public bool TryAddReferencable(string uniqueIdentifier, object obj)
        {
            if (TryConvertToPath(uniqueIdentifier, obj, out GuidPath guidPath))
            {
                _objectSaveDataBuffer.CustomGuidPathSaveData.Add(uniqueIdentifier, guidPath);
                return true;
            }

            return false;
        }

        private bool TryConvertToPath(string uniqueIdentifier, object obj, out GuidPath guidPath)
        {
            guidPath = default;
            
            if (obj == null)
            {
                return false;
            }
            
            if (_objectReferenceLookup.TryGetValue(obj, out guidPath)) return true;
            
            if (!_savableObjectsLookup.ContainsElement(obj))
            {
                guidPath = new GuidPath(_originGuidPath.FullPath, uniqueIdentifier);
                SaveSceneManager.ProcessSavableElement(_savableObjectsLookup, obj, guidPath, _currentIndex + 1);
            }
                
            if (_savableObjectsLookup.TryGetValue(obj, out SavableElement saveElement))
            {
                if (saveElement.SaveStrategy is SaveStrategy.Serializable)
                {
                    var componentDataBuffer = new SaveDataBuffer(saveElement.SaveStrategy, saveElement.Obj.GetType());
                    componentDataBuffer.CustomSerializableSaveData.Add("Serializable", JToken.FromObject(obj));
                    _saveDataBuffer.Add(saveElement.CreatorGuidPath, componentDataBuffer);
                }
                
                guidPath = saveElement.CreatorGuidPath;
                return true;
            }

            return false;
        }
    }
}
