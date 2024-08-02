using System;
using System.Collections.Generic;
using SaveLoadSystem.Core.DataTransferObject;

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
            if (_loadSaveDataBuffer.CustomSerializableSaveData[identifier] == null)
            {
                return default;
            }
            
            return _loadSaveDataBuffer.CustomSerializableSaveData[identifier].ToObject<T>();
        }
        
        public bool TryGetReferencable(string identifier, out GuidPath guidPath)
        {
            return _loadSaveDataBuffer.CustomGuidPathSaveData.TryGetValue(identifier, out guidPath);
        }

        public void InitializeInstance(object obj)
        {
            _createdObjectsLookup.Add(_guidPath, obj);
        }

        public void EnqueueReferenceBuilding(GuidPath path, Action<object> onReferenceFound)
        {
            _deserializeReferenceBuilder.EnqueueReferenceBuilding(path, onReferenceFound);
        }

        public void EnqueueReferenceBuilding(GuidPath[] pathGroup, Action<object[]> onReferenceFound)
        {
            _deserializeReferenceBuilder.EnqueueReferenceBuilding(pathGroup, onReferenceFound);
        }
    }
}
