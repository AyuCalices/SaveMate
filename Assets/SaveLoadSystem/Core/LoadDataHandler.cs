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
            value = default;
            
            return TryLoad(typeof(T), identifier, out var obj) && (value = (T)obj) is not null;
        }

        public bool TryLoad(Type type, string identifier, out object value)
        {
            if (type.IsValueType)
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                {
                    Debug.LogError($"You can't load an object of type {typeof(UnityEngine.Object)} as a value!");
                    value = default;
                    return false;
                }
                
                if (typeof(ISavable).IsAssignableFrom(type))
                {
                    return TryLoadSavable(type, identifier, out value);
                }

                if (TypeConverterRegistry.HasConverter(type))
                {
                    return TryLoadWithConverter(type, identifier, out value);
                }

                return TryLoadValue(type, identifier, out value);
            }

            return TryGetReference(type, identifier, out value);
        }

        public bool TryGetReference<T>(string identifier, out T reference)
        {
            reference = default;

            if (!TryGetGuidPath(identifier, out var guidPath))
                return false;

            if (TryGetReferenceFromLookup(guidPath, typeof(T), out var match))
            {
                reference = (T)match;
                return true;
            }
            
            if (HandleSaveStrategy(guidPath, typeof(T), out var result))
            {
                reference = (T)result;
                return true;
            }

            return false;
        }

        public bool TryGetReference(Type type, string identifier, out object reference)
        {
            reference = default;

            if (!TryGetGuidPath(identifier, out var guidPath))
                return false;

            if (TryGetReferenceFromLookup(guidPath, type, out var match))
            {
                reference = match;
                return true;
            }
            
            return HandleSaveStrategy(guidPath, type, out reference);
        }
        
        private bool TryLoadSavable(Type type, string identifier, out object value)
        {
            var saveDataBuffer = SaveDataBuffer.JsonSerializableSaveData[identifier].ToObject<SaveDataBuffer>();
            var loadDataHandler = new LoadDataHandler(_sceneDataContainer, saveDataBuffer, _pathToObjectReferenceLookup, _createdObjectsLookup);

            value = Activator.CreateInstance(type);
            ((ISavable)value).OnLoad(loadDataHandler);

            return true;
        }

        private bool TryLoadWithConverter(Type type, string identifier, out object value)
        {
            var convertable = TypeConverterRegistry.GetConverter(type);
            var saveDataBuffer = SaveDataBuffer.JsonSerializableSaveData[identifier].ToObject<SaveDataBuffer>();
            var loadDataHandler = new LoadDataHandler(_sceneDataContainer, saveDataBuffer, _pathToObjectReferenceLookup, _createdObjectsLookup);

            value = convertable.CreateInstanceForLoad(loadDataHandler);
            convertable.OnLoad(value, loadDataHandler);

            return true;
        }
        
        private bool TryGetGuidPath(string identifier, out GuidPath guidPath)
        {
            if (!SaveDataBuffer.GuidPathSaveData.TryGetValue(identifier, out guidPath))
            {
                Debug.LogWarning("Wasn't able to find the created object!"); //TODO: debug
                return false;
            }
            return true;
        }

        private bool TryGetReferenceFromLookup(GuidPath guidPath, Type type, out object match)
        {
            if (_pathToObjectReferenceLookup.TryGetValue(guidPath.ToString(), out match))
                return true;

            if (_createdObjectsLookup.TryGetValue(guidPath, out match))
            {
                if (match.GetType() != type)
                {
                    Debug.LogWarning($"The requested object was already created as type '{match.GetType()}'. You tried to return it as type '{type}', which is not allowed. Please use matching types."); //TODO: debug
                    return false;
                }
                return true;
            }
            return false;
        }

        private bool HandleSaveStrategy(GuidPath guidPath, Type type, out object reference)
        {
            reference = null;
            
            if (!_sceneDataContainer.SaveObjectLookup.TryGetValue(guidPath, out SaveDataBuffer saveDataBuffer))
            {
                Debug.LogWarning("Wasn't able to find the created object!"); //TODO: debug
                return false;
            }
            
            var loadDataHandler = new LoadDataHandler(_sceneDataContainer, saveDataBuffer, _pathToObjectReferenceLookup, _createdObjectsLookup);

            switch (saveDataBuffer.SaveStrategy)
            {
                case SaveStrategy.ScriptableObject:
                case SaveStrategy.GameObject:
                    Debug.LogError("The searched object wasn't found. Please use the Savable Component or Asset Registry!"); //TODO: debug
                    return false;

                case SaveStrategy.Savable:
                    reference = Activator.CreateInstance(type);
                    if (!TypeUtility.TryConvertTo(reference, out ISavable objectSavable))
                        return false;

                    _createdObjectsLookup.Add(guidPath, reference);
                    objectSavable.OnLoad(loadDataHandler);
                    return true;

                case SaveStrategy.Convertable:
                    if (!TypeConverterRegistry.TryGetConverter(type, out IConvertable convertable))
                        return false;

                    reference = convertable.CreateInstanceForLoad(loadDataHandler);
                    _createdObjectsLookup.Add(guidPath, reference);
                    convertable.OnLoad(reference, loadDataHandler);
                    return true;

                case SaveStrategy.Serializable:
                    var jObject = saveDataBuffer.JsonSerializableSaveData["SerializeRef"];
                    if (jObject != null)
                    {
                        reference = jObject.ToObject(type);
                        _createdObjectsLookup.Add(guidPath, reference);
                        return true;
                    }

                    Debug.LogError("Wasn't able to find the SerializeRef"); //TODO: debug
                    return false;

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
