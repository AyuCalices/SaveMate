using System;
using System.Collections.Generic;
using SaveLoadSystem.Core.Converter;
using SaveLoadSystem.Core.DataTransferObject;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using SaveLoadSystem.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaveLoadSystem.Core
{
    /// <summary>
    /// The <see cref="LoadDataHandler"/> class is responsible for managing the deserialization and retrieval of
    /// serialized data, as well as handling reference building for complex object graphs.
    /// </summary>
    public readonly struct LoadDataHandler
    {
        private readonly InstanceSaveData _instanceSaveData;
        private readonly SceneSaveData _sceneSaveData;
        private readonly Dictionary<GuidPath, object> _createdObjectsLookup;
        
        //unity object reference lookups
        private readonly Dictionary<GuidPath, GameObject> _guidToSavableGameObjectLookup;
        private readonly Dictionary<GuidPath, ScriptableObject> _guidToScriptableObjectLookup;
        private readonly Dictionary<GuidPath, Component> _guidToComponentLookup;

        public LoadDataHandler(SceneSaveData sceneSaveData, InstanceSaveData instanceSaveData, 
            Dictionary<GuidPath, object> createdObjectsLookup, Dictionary<GuidPath, GameObject> guidToSavableGameObjectLookup,
            Dictionary<GuidPath, ScriptableObject> guidToScriptableObjectLookup, Dictionary<GuidPath, Component> guidToComponentLookup)
        {
            _instanceSaveData = instanceSaveData;
            _sceneSaveData = sceneSaveData;
            _createdObjectsLookup = createdObjectsLookup;
            _guidToSavableGameObjectLookup = guidToSavableGameObjectLookup;
            _guidToScriptableObjectLookup = guidToScriptableObjectLookup;
            _guidToComponentLookup = guidToComponentLookup;
        }

        public bool TryLoad<T>(string identifier, out T obj)
        {
            var res = TryLoad(typeof(T), identifier, out var innerObj);

            if (res)
            {
                obj = (T)innerObj;
            }
            else
            {
                obj = default;
            }
            
            return res;
        }
        
        public bool TryLoad(Type type, string identifier, out object obj)
        {
            return type.IsValueType ? TryLoadValue(type, identifier, out obj) : TryLoadReference(type, identifier, out obj);
        }

        public bool TryLoadValue<T>(string identifier, out T value)
        {
            var res = TryLoadValue(typeof(T), identifier, out var obj);
            
            if (res)
            {
                value = (T)obj;
            }
            else
            {
                value = default;
            }
            
            return res;
        }

        private bool TryLoadValue(Type type, string identifier, out object value)
        {
            //unity object handling
            if (typeof(Object).IsAssignableFrom(type))
            {
                Debug.LogError($"You can't load an object of type {typeof(Object)} as a value!");
                value = default;
                return false;
            }
            
            //savable handling
            if (typeof(ISavable).IsAssignableFrom(type))
            {
                var res = TryLoadValueSavable(type, identifier, out value);
                return res;
            }

            //converter handling
            if (ConverterServiceProvider.ExistsAndCreate(type))
            {
                var res = TryLoadValueWithConverter(type, identifier, out value);
                return res;
            }
            
            //serialization handling
            if (_instanceSaveData.Values[identifier] == null)
            {
                value = default;
                return false;     //TODO: debug
            }

            value = _instanceSaveData.Values[identifier].ToObject(type);
            return true;
        }
        
        public bool TryLoadReference<T>(string identifier, out T reference)
        {
            var res = TryLoadValue(typeof(T), identifier, out var obj);
            
            if (res)
            {
                reference = (T)obj;
            }
            else
            {
                reference = default;
            }
            
            return res;
        }

        private bool TryLoadReference(Type type, string identifier, out object reference)
        {
            reference = default;

            if (!TryGetReferenceGuidPath(identifier, out var guidPath))
                return false;

            if (TryGetReferenceFromLookup(type, guidPath, out reference))
                return true;
            
            if (HandleSaveStrategy(type, guidPath, out reference))
                return true;

            return false;
        }
        
        private bool TryLoadValueSavable(Type type, string identifier, out object value)
        {
            if (_instanceSaveData == null || !_instanceSaveData.Values.TryGetValue(identifier, out var saveData))
            {
                Debug.LogWarning("There was no matching data!");
                value = null;
                return false;
            }
            
            var saveDataBuffer = saveData.ToObject<InstanceSaveData>();
            var loadDataHandler = new LoadDataHandler(_sceneSaveData, saveDataBuffer, 
                _createdObjectsLookup, _guidToSavableGameObjectLookup, _guidToScriptableObjectLookup, _guidToComponentLookup);

            value = Activator.CreateInstance(type);
            ((ISavable)value).OnLoad(loadDataHandler);

            return true;
        }
        
        private bool TryLoadValueWithConverter(Type type, string identifier, out object value)
        {
            if (_instanceSaveData == null || !_instanceSaveData.Values.TryGetValue(identifier, out var saveData))
            {
                Debug.LogWarning("There was no matching data!");
                value = null;
                return false;
            }
            
            var saveDataBuffer = saveData.ToObject<InstanceSaveData>();
            var loadDataHandler = new LoadDataHandler(_sceneSaveData, saveDataBuffer, 
                _createdObjectsLookup, _guidToSavableGameObjectLookup, _guidToScriptableObjectLookup, _guidToComponentLookup);

            var convertable = ConverterServiceProvider.GetConverter(type);
            value = convertable.CreateInstanceForLoad(loadDataHandler);
            convertable.Load(value, loadDataHandler);
            return true;
        }
        
        private bool TryGetReferenceGuidPath(string identifier, out GuidPath guidPath)
        {
            if (!_instanceSaveData.References.TryGetValue(identifier, out guidPath))
            {
                Debug.LogWarning("Wasn't able to find the created object!"); //TODO: debug
                return false;
            }
            
            return true;
        }

        private bool TryGetReferenceFromLookup(Type type, GuidPath guidPath, out object reference)
        {
            reference = default;

            //unity type reference handling
            if (typeof(ScriptableObject).IsAssignableFrom(type))
            {
                if (_guidToScriptableObjectLookup.TryGetValue(guidPath, out var scriptableObject))
                {
                    reference = scriptableObject;
                    return true;
                }
            }
            else if (type == typeof(GameObject))
            {
                if (_guidToSavableGameObjectLookup.TryGetValue(guidPath, out var savable))
                {
                    reference = savable.gameObject;
                    return true;
                }
            } 
            else if (type == typeof(Transform))
            {
                if (_guidToSavableGameObjectLookup.TryGetValue(guidPath, out var savable))
                {
                    reference = savable.transform;
                    return true;
                }
            }
            else if (type == typeof(RectTransform))
            {
                if (_guidToSavableGameObjectLookup.TryGetValue(guidPath, out var savable))
                {
                    reference = (RectTransform)savable.transform;
                    return true;
                }
            }
            else if (typeof(Component).IsAssignableFrom(type))
            {
                if (_guidToComponentLookup.TryGetValue(guidPath, out var duplicatedComponent))
                {
                    reference = duplicatedComponent;
                    return true;
                }
                
                if (_guidToSavableGameObjectLookup.TryGetValue(guidPath, out var savable))
                {
                    reference = savable.GetComponent(type);
                    return true;
                }
            }
            
            //custom type reference handling
            if (_createdObjectsLookup.TryGetValue(guidPath, out reference))
            {
                if (reference.GetType() != type)
                {
                    Debug.LogWarning($"The requested object was already created as type '{reference.GetType()}'. You tried to return it as type '{type}', which is not allowed. Please use matching types."); //TODO: debug
                    return false;
                }
                
                return true;
            }
            
            return false;
        }

        private bool HandleSaveStrategy(Type type, GuidPath guidPath, out object reference)
        {
            reference = null;
            
            if (!_sceneSaveData.InstanceSaveDataLookup.TryGetValue(guidPath, out InstanceSaveData saveDataBuffer))
            {
                Debug.LogWarning("Wasn't able to find the created object!"); //TODO: debug
                return false;
            }
            
            var loadDataHandler = new LoadDataHandler(_sceneSaveData, saveDataBuffer, 
                _createdObjectsLookup, _guidToSavableGameObjectLookup, _guidToScriptableObjectLookup, _guidToComponentLookup);
            
            //savable handling
            if (typeof(ISavable).IsAssignableFrom(type))
            {
                reference = Activator.CreateInstance(type);
                if (!TypeUtility.TryConvertTo(reference, out ISavable objectSavable))
                    return false;

                _createdObjectsLookup.Add(guidPath, reference);
                objectSavable.OnLoad(loadDataHandler);
                return true;
            }

            //converter handling
            if (ConverterServiceProvider.ExistsAndCreate(type))
            {
                if (!ConverterServiceProvider.ExistsAndCreate(type))
                    return false;

                var converter = ConverterServiceProvider.GetConverter(type);

                reference = converter.CreateInstanceForLoad(loadDataHandler);
                _createdObjectsLookup.Add(guidPath, reference);
                converter.Load(reference, loadDataHandler);
                return true;
            }

            //serializable handling
            var jObject = saveDataBuffer.Values["SerializeRef"];
            if (jObject != null)
            {
                reference = jObject.ToObject(type);
                _createdObjectsLookup.Add(guidPath, reference);
                return true;
            }
            
            //TODO: if UnityEngine.Object, then it might not be inside any registry -> create Debug.log
            Debug.LogError("Wasn't able to find the SerializeRef"); //TODO: debug
            return false;
        }
    }
}
