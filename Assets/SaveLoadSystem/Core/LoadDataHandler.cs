using System;
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
        //save data container
        private readonly RootSaveData _rootSaveData;
        private readonly BranchSaveData _globalBranchSaveData;
        private readonly LeafSaveData _leafSaveData;

        private readonly LoadType _loadType;
        private readonly string _sceneName;

        //reference lookups
        private readonly SaveLoadManager _saveLoadManager;
        private readonly SaveFileContext _saveFileContext;

        public LoadDataHandler(RootSaveData rootSaveData, BranchSaveData globalBranchSaveData, LeafSaveData leafSaveData, 
            LoadType loadType, string sceneName, SaveFileContext saveFileContext, SaveLoadManager saveLoadManager)
        {
            _rootSaveData = rootSaveData;
            _globalBranchSaveData = globalBranchSaveData;
            _leafSaveData = leafSaveData;

            _loadType = loadType;
            _sceneName = sceneName;

            _saveLoadManager = saveLoadManager;
            _saveFileContext = saveFileContext;
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
            return type.IsValueType || type ==  typeof(string) ? TryLoadValue(type, identifier, out obj) : TryLoadReference(type, identifier, out obj);
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
                //TODO: check, if this is really the case
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
            
            //TODO: implement try catch and error messages
            //serialization handling
            if (_leafSaveData.Values[identifier] == null)
            {
                value = default;
                return false;     
            }

            value = _leafSaveData.Values[identifier].ToObject(type);
            return true;
        }
        
        private bool TryLoadValueSavable(Type type, string identifier, out object value)
        {
            if (_leafSaveData == null || !_leafSaveData.Values.TryGetValue(identifier, out var saveData))
            {
                Debug.LogError($"Wasn't able to find the save data for the identifier: '{identifier}'. Requested object type: '{type.FullName}'!");
                value = null;
                return false;
            }
            
            var saveDataBuffer = saveData.ToObject<LeafSaveData>();
            var loadDataHandler = new LoadDataHandler(_rootSaveData, _globalBranchSaveData, saveDataBuffer, _loadType, 
                _sceneName, _saveFileContext, _saveLoadManager);

            value = Activator.CreateInstance(type);
            ((ISavable)value).OnLoad(loadDataHandler);

            return true;
        }
        
        private bool TryLoadValueWithConverter(Type type, string identifier, out object value)
        {
            if (_leafSaveData == null || !_leafSaveData.Values.TryGetValue(identifier, out var saveData))
            {
                Debug.LogError($"Wasn't able to find the save data for the identifier: '{identifier}'. Requested object type: '{type.FullName}'!");
                value = null;
                return false;
            }
            
            var saveDataBuffer = saveData.ToObject<LeafSaveData>();
            var loadDataHandler = new LoadDataHandler(_rootSaveData, _globalBranchSaveData, saveDataBuffer, _loadType, 
                _sceneName, _saveFileContext, _saveLoadManager);

            var convertable = ConverterServiceProvider.GetConverter(type);
            value = convertable.CreateInstanceForLoad(loadDataHandler);
            convertable.Load(value, loadDataHandler);
            return true;
        }
        
        public bool TryLoadReference<T>(string identifier, out T reference)
        {
            var res = TryLoadReference(typeof(T), identifier, out var obj);
            
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

            if (!TryGetGuidPath(identifier, out var guidPath))
                return false;

            // Handle Unity type references
            if (typeof(ScriptableObject).IsAssignableFrom(type))
            {
                return TryGetScriptableObjectReference(guidPath, out reference);
            }

            if (type == typeof(GameObject))
            {
                return TryGetGameObjectReference(guidPath, out reference);
            }

            if (type == typeof(Transform))
            {
                return TryGetTransformReference(guidPath, out reference);
            }

            if (type == typeof(RectTransform))
            {
                return TryGetRectTransformReference(guidPath, out reference);
            }

            if (typeof(Component).IsAssignableFrom(type))
            {
                return TryGetComponentReference(type, guidPath, out reference);
            }

            // Try to load a created object or create a new one
            return TryGetCreatedObject(type, guidPath, out reference) || CreateObject(type, guidPath, out reference);
        }

        private bool TryGetScriptableObjectReference(GuidPath guidPath, out object reference)
        {
            reference = null;
            
            if (_saveLoadManager.GuidToScriptableObjectLookup.TryGetValue(guidPath, out var scriptableObject))
            {
                reference = scriptableObject;
                return true;
            }
            
            Debug.LogError($"Wasn't able to find the '{nameof(ScriptableObject)}' for the GUID: '{guidPath.ToString()}'!");
            return false;
        }

        private bool TryGetGameObjectReference(GuidPath guidPath, out object reference)
        {
            reference = null;
            
            if (GetGuidPathGameObject(guidPath, out var savable))
            {
                reference = savable.gameObject;
                return true;
            }
            
            Debug.LogError($"Wasn't able to find the '{nameof(GameObject)}' for the GUID: '{guidPath.ToString()}'!");
            return false;
        }

        private bool TryGetTransformReference(GuidPath guidPath, out object reference)
        {
            reference = null;
            
            if (GetGuidPathGameObject(guidPath, out var savable))
            {
                reference = savable.transform;
                return true;
            }
            
            Debug.LogError($"Wasn't able to find the '{nameof(Transform)}' for the GUID: '{guidPath.ToString()}'!");
            return false;
        }

        private bool TryGetRectTransformReference(GuidPath guidPath, out object reference)
        {
            reference = null;
            
            if (GetGuidPathGameObject(guidPath, out var savable))
            {
                reference = (RectTransform)savable.transform;
                return true;
            }
            
            Debug.LogError($"Wasn't able to find the '{nameof(RectTransform)}' for the GUID: '{guidPath.ToString()}'!");
            return false;
        }

        private bool TryGetComponentReference(Type type, GuidPath guidPath, out object reference)
        {
            reference = null;

            // Try to get the component with a unique identifier
            if (GetGuidPathComponent(guidPath, out var duplicatedComponent))
            {
                reference = duplicatedComponent;
                return true;
            }

            // Fallback: resolve the component from the GameObject
            if (GetGuidPathGameObject(guidPath, out var savable))
            {
                reference = savable.GetComponent(type);
                return true;
            }
            
            Debug.LogError($"Wasn't able to find the {nameof(Component)} of type '{type.FullName}' for the GUID: '{guidPath.ToString()}'!");
            return false;
        }
        
        private bool TryGetGuidPath(string identifier, out GuidPath guidPath)
        {
            if (!_leafSaveData.References.TryGetValue(identifier, out guidPath))
            {
                Debug.LogError($"Wasn't able to find save data for the identifier: '{identifier}'!");
                return false;
            }
            
            return true;
        }
        
        private bool GetGuidPathGameObject(GuidPath guidPath, out GameObject gameObject)
        {
            gameObject = default;
            
            foreach (var saveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
            {
                if (_sceneName != saveSceneManager.SceneName) continue;
                    
                if (saveSceneManager.GuidToSavableGameObjectLookup.TryGetValue(guidPath, out gameObject))
                {
                    return true;
                }
                
                return false;
            }

            return false;
        }
        
        private bool GetGuidPathComponent(GuidPath guidPath, out Component component)
        {
            component = default;
            
            foreach (var saveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
            {
                if (_sceneName != saveSceneManager.SceneName) continue;
                    
                if (saveSceneManager.GuidToComponentLookup.TryGetValue(guidPath, out component))
                {
                    return true;
                }
                
                return false;
            }

            return false;
        }

        private bool TryGetCreatedObject(Type type, GuidPath guidPath, out object reference)
        {
            reference = default;
            
            if (_saveFileContext.GuidToCreatedNonUnityObjectLookup.TryGetValue(_loadType, guidPath, out reference))
            {
                if (reference.GetType() != type)
                {
                    Debug.LogWarning($"The requested object was already created as type '{reference.GetType()}'. " +
                                     $"You tried to return it as type '{type}', which is not allowed. Please use matching types.");
                    return false;
                }
                
                return true;
            }

            return false;
        }

        private bool CreateObject(Type type, GuidPath guidPath, out object reference)
        {
            reference = null;
            
            if (!_globalBranchSaveData.TryGetLeafSaveData(guidPath, out LeafSaveData leafSaveData) && 
                !_rootSaveData.GlobalSaveData.TryGetLeafSaveData(guidPath, out leafSaveData))
            {
                Debug.LogError($"Wasn't able to find the save data for the GUID: '{guidPath.ToString()}'. Requested object type: '{type.FullName}'!");
                return false;
            }
            
            var loadDataHandler = new LoadDataHandler(_rootSaveData, _globalBranchSaveData, leafSaveData, _loadType, 
                _sceneName, _saveFileContext, _saveLoadManager);
            
            //savable handling
            if (typeof(ISavable).IsAssignableFrom(type))
            {
                reference = Activator.CreateInstance(type);
                if (!TypeUtility.TryConvertTo(reference, out ISavable objectSavable))
                    return false;

                _saveFileContext.GuidToCreatedNonUnityObjectLookup.Add(_loadType, guidPath, reference);
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
                _saveFileContext.GuidToCreatedNonUnityObjectLookup.Add(_loadType, guidPath, reference);
                converter.Load(reference, loadDataHandler);
                return true;
            }

            //serializable handling
            try
            {
                var jObject = leafSaveData.Values["SerializeRef"];
                if (jObject != null)
                {
                    reference = jObject.ToObject(type);
                    if (reference != null)
                    {
                        _saveFileContext.GuidToCreatedNonUnityObjectLookup.Add(_loadType, guidPath, reference);
                        return true;
                    }

                    Debug.LogError($"Failed to convert JObject to {type} for GUID path: {guidPath.ToString()}. The resulting object is null.");
                    return false;
                }

                Debug.LogError($"Wasn't able to find the object of type '{type.FullName}' for GUID path '{guidPath.ToString()}' inside the save data!");
                return false;
            }
            catch (Exception e)
            {
                string errorMessage;

                // Handle UnityEngine.Object serialization specifically
                if (typeof(Object).IsAssignableFrom(type))
                {
                    errorMessage = $"Error serializing UnityEngine.Object '{type.FullName}' using '{nameof(Newtonsoft.Json)}'. ";
                    errorMessage += "Ensure that the object implements ISavable or a custom SaveMateConverter is used. ";
                }
                else
                {
                    errorMessage = $"Error serializing object of type '{type.FullName}' using '{nameof(Newtonsoft.Json)}'. ";
                }
                
                errorMessage += $"Exception: {e.GetType().Name} - {e.Message}\n{e.StackTrace}";
                Debug.LogError(errorMessage);
                return false;
            }
        }
    }
}
