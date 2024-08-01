using System.Collections.Generic;
using SaveLoadSystem.Core.Component;
using SaveLoadSystem.Core.SerializableTypes;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace SaveLoadSystem.Core
{
    public class SaveDataHandler
    {
        private readonly SaveDataBuffer _objectSaveDataBuffer;
        private readonly SavableElementLookup _savableElementLookup;
        private readonly Dictionary<object, GuidPath> _objectReferenceLookup;
        private readonly int _currentIndex;

        public SaveDataHandler(SaveDataBuffer objectSaveDataBuffer, SavableElementLookup savableElementLookup, Dictionary<object, GuidPath> objectReferenceLookup, int currentIndex)
        {
            _objectSaveDataBuffer = objectSaveDataBuffer;
            _savableElementLookup = savableElementLookup;
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
            
            guidPath = new GuidPath(_objectSaveDataBuffer.originGuidPath.FullPath, uniqueIdentifier);
            if (!_savableElementLookup.ContainsElement(obj))
            {
                SaveSceneManager.ProcessSavableElement(_savableElementLookup, obj, guidPath, _currentIndex + 1);
            }
                
            if (_savableElementLookup.TryGetValue(obj, out SavableElement saveElement))
            {
                guidPath = saveElement.CreatorGuidPath;
                return true;
            }

            return false;
        }
    }
}
