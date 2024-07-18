using System;
using SaveLoadCore.Core.Component;
using SaveLoadCore.Core.Serializable;

namespace SaveLoadCore.Core
{
    public class SaveDataHandler
    {
        private readonly DataBuffer _objectDataBuffer;
        private readonly SavableElementLookup _savableElementLookup;
        private readonly int _currentIndex;

        public SaveDataHandler(DataBuffer objectDataBuffer, SavableElementLookup savableElementLookup, int currentIndex)
        {
            _objectDataBuffer = objectDataBuffer;
            _savableElementLookup = savableElementLookup;
            _currentIndex = currentIndex;
        }

        public void AddSerializable(string uniqueIdentifier, object obj)
        {
            _objectDataBuffer.CustomSaveData.Add(uniqueIdentifier, obj);
        }

        public object ToReferencableObject(string uniqueIdentifier, object obj)
        {
            var guidPath = new GuidPath(_objectDataBuffer.originGuidPath, uniqueIdentifier);
            if (!_savableElementLookup.ContainsElement(obj))
            {
                SaveSceneManager.ProcessSavableElement(_savableElementLookup, obj, guidPath, _currentIndex + 1);
            }
                
            if (_savableElementLookup.TryGetValue(obj, out SavableElement saveElement))
            {
                return saveElement.SaveStrategy == SaveStrategy.Serializable ? saveElement.Obj : saveElement.CreatorGuidPath;
            }

            throw new InvalidOperationException("The object could not be processed or retrieved from the save element lookup.");
        }
        
        public void AddReferencable(string uniqueIdentifier, object obj)
        {
            AddSerializable(uniqueIdentifier, ToReferencableObject(uniqueIdentifier, obj));
        }
    }
}
