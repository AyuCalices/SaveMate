using System;
using System.Collections.Generic;
using SaveLoadCore.Core.Serializable;

namespace SaveLoadCore.Core
{
    public class LoadDataHandler
    {
        private readonly DataBuffer _loadDataBuffer;
        private readonly DeserializeReferenceBuilder _deserializeReferenceBuilder;
        private readonly Dictionary<GuidPath, object> _createdObjectsLookup;
        private readonly GuidPath _guidPath;

        public LoadDataHandler(DataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder, Dictionary<GuidPath, object> createdObjectsLookup, GuidPath guidPath)
        {
            _loadDataBuffer = loadDataBuffer;
            _deserializeReferenceBuilder = deserializeReferenceBuilder;
            _createdObjectsLookup = createdObjectsLookup;
            _guidPath = guidPath;
        }

        public T GetSaveElement<T>(string identifier)
        {
            return (T)_loadDataBuffer.CustomSaveData[identifier];
        }

        public object GetSaveElement(string identifier)
        {
            return _loadDataBuffer.CustomSaveData[identifier];
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
