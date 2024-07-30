using System;
using System.Collections.Generic;
using SaveLoadSystem.Core.Component;
using SaveLoadSystem.Core.SerializableTypes;
using UnityEngine;

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
            _objectSaveDataBuffer.CustomSaveData.Add(uniqueIdentifier, obj);
        }

        public object ToReferencableObject(string uniqueIdentifier, object obj)
        {
            if (obj == null)
            {
                return null;
            }
            
            if (_objectReferenceLookup.TryGetValue(obj, out GuidPath guidPath))
            {
                return guidPath;
            }
            
            guidPath = new GuidPath(_objectSaveDataBuffer.originGuidPath, uniqueIdentifier);
            if (!_savableElementLookup.ContainsElement(obj))
            {
                SaveSceneManager.ProcessSavableElement(_savableElementLookup, obj, guidPath, _currentIndex + 1);
            }
                
            if (_savableElementLookup.TryGetValue(obj, out SavableElement saveElement))
            {
                return saveElement.SaveStrategy == SaveStrategy.Serializable ? saveElement.Obj : saveElement.CreatorGuidPath;
            }
            
            Debug.LogWarning("The object could not be processed or retrieved from the save element lookup.");

            return null;
        }
        
        public void AddReferencable(string uniqueIdentifier, object obj)
        {
            AddSerializable(uniqueIdentifier, ToReferencableObject(uniqueIdentifier, obj));
        }

        public bool TryAddReferencable(string uniqueIdentifier, object obj)
        {
            var referencable = ToReferencableObject(uniqueIdentifier, obj);
            
            if (referencable != null)
            {
                AddSerializable(uniqueIdentifier, referencable);
            }

            return referencable != null;
        }
    }
}
