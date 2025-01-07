using System;
using System.Collections.Generic;
using SaveLoadSystem.Core.Component;
using SaveLoadSystem.Core.Component.SavableConverter;
using SaveLoadSystem.Core.Converter;
using SaveLoadSystem.Core.DataTransferObject;
using SaveLoadSystem.Utility;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    /// <summary>
    /// The <see cref="LoadDataHandler"/> class is responsible for managing the deserialization and retrieval of
    /// serialized data, as well as handling reference building for complex object graphs.
    /// </summary>
    //TODO: LoadDataHandler must be renamed, so it makes it clear, it is already correctly connected with the current object -> the SaveDataBuffer
    public class LoadDataHandler : SimpleLoadDataHandler
    {
        private readonly SceneDataContainer _sceneDataContainer;
        private readonly Dictionary<string, object> _pathToObjectReferenceLookup;
        private readonly Dictionary<GuidPath, object> _createdObjectsLookup;

        public LoadDataHandler(SceneDataContainer sceneDataContainer, SaveDataBuffer saveDataBuffer, Dictionary<string, object> pathToObjectReferenceLookup, 
            Dictionary<GuidPath, object> createdObjectsLookup) : base(saveDataBuffer)
        {
            _sceneDataContainer = sceneDataContainer;
            _pathToObjectReferenceLookup = pathToObjectReferenceLookup;
            _createdObjectsLookup = createdObjectsLookup;
        }

        public bool TryLoad<T>(string identifier, out T value)
        {
            return typeof(T).IsValueType ? TryLoadValue(identifier, out value) : TryGetReference(identifier, out value);
        }
        
        public bool TryLoad(Type type, string identifier, out object value)
        {
            return type.IsValueType ? TryLoadValue(type, identifier, out value) : TryGetReference(identifier, out value);
        }

        public bool TryGetReference<T>(string identifier, out T reference)
        {
            reference = default;

            //get the identifier for the object
            if (!SaveDataBuffer.GuidPathSaveData.TryGetValue(identifier, out var guidPath))
            {
                Debug.LogWarning("Wasn't able to find the created object!");        //TODO: debug
                return false;
            }
            
            //try to resolve by savable component and asset registry
            if (_pathToObjectReferenceLookup.TryGetValue(guidPath.ToString(), out var match))
            {
                reference = (T)match;
                return true;
            }
            
            //check if the object was already recreated
            if (_createdObjectsLookup.TryGetValue(guidPath, out match))
            {
                if (match.GetType() != typeof(T))
                {
                    Debug.LogWarning($"The requested object was already created as type '{match.GetType()}'. You tried to return it as type '{typeof(T)}', which is not allowed. Please change it to matching types!");        //TODO: debug
                    return false;
                }
                
                reference = (T)match;
                return true;
            }

            //get the SaveDataBuffer for creating the object
            if (!_sceneDataContainer.SaveObjectLookup.TryGetValue(guidPath, out SaveDataBuffer saveDataBuffer))
            {
                Debug.LogWarning("Wasn't able to find the created object!");        //TODO: debug
                return false;
            }

            switch (saveDataBuffer.SaveStrategy)
            {
                case SaveStrategy.UnityObject:
                {
                    Debug.LogError("The searched Unity Object wasn't found. Please use the Savable Component or Asset Registry!");        //TODO: debug
                    return false;
                }

                case SaveStrategy.Savable:
                {
                    var loadDataHandler = new LoadDataHandler(_sceneDataContainer, saveDataBuffer, _pathToObjectReferenceLookup, _createdObjectsLookup);
                    reference = Activator.CreateInstance<T>();
                    
                    if (!TypeUtility.TryConvertTo(reference, out ISavable objectSavable)) 
                        return false;
                        
                    _createdObjectsLookup.Add(guidPath, reference);
                    objectSavable.OnLoad(loadDataHandler);
                    return true;
                }
                
                case SaveStrategy.Convertable:
                {
                    if (!TypeConverterRegistry.TryGetConverter(typeof(T), out IConvertable convertable)) 
                        return false;

                    var loadDataHandler = new LoadDataHandler(_sceneDataContainer, saveDataBuffer, _pathToObjectReferenceLookup, _createdObjectsLookup);
                    reference = (T)convertable.CreateInstanceForLoad(loadDataHandler);
                    _createdObjectsLookup.Add(guidPath, reference);
                    convertable.OnLoad(reference, loadDataHandler);
                    return true;
                }
                
                case SaveStrategy.Serializable:
                {
                    var jObject = saveDataBuffer.JsonSerializableSaveData["SerializeRef"];
                    if (jObject != null)
                    {
                        reference = jObject.ToObject<T>();
                        _createdObjectsLookup.Add(guidPath, reference);
                        return true;
                    }

                    Debug.LogError("Wasn't able to find the SerializeRef");        //TODO: debug
                    return false;
                }
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class SimpleLoadDataHandler
    {
        protected readonly SaveDataBuffer SaveDataBuffer;
        
        public SimpleLoadDataHandler(SaveDataBuffer saveDataBuffer)
        {
            SaveDataBuffer = saveDataBuffer;
        }
        
        public bool TryLoadValue<T>(string identifier, out T value)
        {
            value = default;
            
            if (SaveDataBuffer.JsonSerializableSaveData[identifier] == null)
            {
                return false;     //TODO: debug
            }

            value = SaveDataBuffer.JsonSerializableSaveData[identifier].ToObject<T>();
            return true;
        }
        
        public bool TryLoadValue(Type type, string identifier, out object value)
        {
            value = default;
            
            if (SaveDataBuffer.JsonSerializableSaveData[identifier] == null)
            {
                return false;     //TODO: debug
            }
            
            value = SaveDataBuffer.JsonSerializableSaveData[identifier].ToObject(type);
            return true;
        }
    }
}
