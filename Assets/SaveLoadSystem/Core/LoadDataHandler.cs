using System;
using System.Collections.Generic;
using SaveLoadSystem.Core.Serializable;

namespace SaveLoadSystem.Core
{
    public class LoadDataHandler
    {
        private readonly SaveDataBuffer _loadSaveDataBuffer;
        private readonly DeserializeReferenceBuilder _deserializeReferenceBuilder;
        private readonly Dictionary<GuidPath, object> _createdObjectsLookup;
        private readonly GuidPath _guidPath;

        public LoadDataHandler(SaveDataBuffer loadSaveDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder, Dictionary<GuidPath, object> createdObjectsLookup, GuidPath guidPath)
        {
            _loadSaveDataBuffer = loadSaveDataBuffer;
            _deserializeReferenceBuilder = deserializeReferenceBuilder;
            _createdObjectsLookup = createdObjectsLookup;
            _guidPath = guidPath;
        }
        
        public T GetSerializable<T>(string identifier)
        {
            return (T)_loadSaveDataBuffer.CustomSaveData[identifier];
        }
        
        public bool TryGetReferencable(string identifier, out object obj)
        {
            return _loadSaveDataBuffer.CustomSaveData.TryGetValue(identifier, out obj);
        }

        public object GetReferencable(string identifier)
        {
            return _loadSaveDataBuffer.CustomSaveData[identifier];
        }

        public void InitializeInstance(object obj)
        {
            _createdObjectsLookup.Add(_guidPath, obj);
        }

        public void EnqueueReferenceBuilding(object obj, Action<object> onReferenceFound)
        {
            _deserializeReferenceBuilder.EnqueueReferenceBuilding(obj, onReferenceFound);
        }

        public void EnqueueReferenceBuilding(object[] objectGroup, Action<object[]> onReferenceFound)
        {
            _deserializeReferenceBuilder.EnqueueReferenceBuilding(objectGroup, onReferenceFound);
        }
    }
}
