using System;
using System.Collections.Generic;
using SaveLoadSystem.Core.DataTransferObject;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    /// <summary>
    /// The <see cref="LoadDataHandler"/> class is responsible for managing the deserialization and retrieval of
    /// serialized data, as well as handling reference building for complex object graphs.
    /// </summary>
    public class LoadDataHandler : SimpleLoadDataHandler
    {
        private readonly Dictionary<string, object> _guidPathReferenceLookup;
        private readonly Dictionary<GuidPath, object> _pathToObjectReferenceLookup;

        public LoadDataHandler(SaveDataBuffer loadSaveDataBuffer, Dictionary<string, object> guidPathReferenceLookup, 
            Dictionary<GuidPath, object> pathToObjectReferenceLookup) : base(loadSaveDataBuffer)
        {
            _guidPathReferenceLookup = guidPathReferenceLookup;
            _pathToObjectReferenceLookup = pathToObjectReferenceLookup;
        }

        //TODO: enqueue value types as well -> by doing so, as soon as everything exists, queueing things will make sure it exists when the next is needed
        public bool TryLoad<T>(string identifier, out T value)
        {
            if (typeof(T).IsValueType)
            {
                return TryLoadValue(identifier, out value);
            }

            if (TryGetReference(identifier, out value))
            {
                return true;
            }

            Debug.LogWarning("Couldn't load, cause reference was not found");     //TODO: debug
            return false;
        }
        
        public bool TryLoad(Type type, string identifier, out object value)
        {
            if (type.IsValueType)
            {
                return TryLoadValue(type, identifier, out value);
            }

            if (TryGetReference(identifier, out value))
            {
                return true;
            }

            Debug.LogWarning("Couldn't load, cause reference was not found");     //TODO: debug
            return false;
        }

        private bool TryGetReference<T>(string identifier, out T value)
        {
            value = default;

            if (!LoadSaveDataBuffer.GuidPathSaveData.TryGetValue(identifier, out var guidPath))
            {
                Debug.LogWarning("Wasn't able to find the created object!");        //TODO: debug
                return false;
            }
            
            if (_pathToObjectReferenceLookup.TryGetValue(guidPath, out var match))
            {
                value = (T)match;
                return true;
            }

            if (_guidPathReferenceLookup.TryGetValue(guidPath.ToString(), out match))
            {
                value = (T)match;
                return true;
            }

            Debug.LogWarning("Wasn't able to find the created object!");        //TODO: debug
            return false;
        }
    }

    public class SimpleLoadDataHandler
    {
        protected readonly SaveDataBuffer LoadSaveDataBuffer;
        
        public SimpleLoadDataHandler(SaveDataBuffer loadSaveDataBuffer)
        {
            LoadSaveDataBuffer = loadSaveDataBuffer;
        }
        
        public bool TryLoadValue<T>(string identifier, out T value)
        {
            value = default;
            
            if (LoadSaveDataBuffer.JsonSerializableSaveData[identifier] == null)
            {
                return false;     //TODO: debug
            }

            value = LoadSaveDataBuffer.JsonSerializableSaveData[identifier].ToObject<T>();
            return true;
        }
        
        public bool TryLoadValue(Type type, string identifier, out object value)
        {
            value = default;
            
            if (LoadSaveDataBuffer.JsonSerializableSaveData[identifier] == null)
            {
                return false;     //TODO: debug
            }
            
            value = LoadSaveDataBuffer.JsonSerializableSaveData[identifier].ToObject(type);
            return true;
        }
    }
}
