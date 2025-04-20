using System;
using Newtonsoft.Json.Linq;
using SaveMate.Core.DataTransferObject;
using SaveMate.Core.SaveComponents.AssetScope;
using SaveMate.Core.SaveComponents.GameObjectScope;
using SaveMate.Core.SaveComponents.ManagingScope;
using SaveMate.Core.StateSnapshot.Converter;
using SaveMate.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaveMate.Core.StateSnapshot
{
    //// <summary>
    /// The <see cref="RestoreSnapshotHandler"/> struct is responsible for restoring serialized snapshot data into
    /// in-memory objects. It supports loading both value types and reference types, resolving Unity components, 
    /// and applying custom converters or state handlers.
    /// </summary>
    public readonly struct RestoreSnapshotHandler
    {
        //save data container
        private readonly RootSaveData _rootSaveData;
        private readonly LeafSaveData _leafSaveData;
        
        private readonly LoadType _loadType;
        private readonly GuidPath _guidPath;

        //reference lookups
        private readonly SaveMateManager _saveMateManager;
        private readonly SaveFileContext _saveFileContext;

        internal RestoreSnapshotHandler(RootSaveData rootSaveData, LeafSaveData leafSaveData, LoadType loadType, GuidPath guidPath, 
            SaveFileContext saveFileContext, SaveMateManager saveMateManager)
        {
            _rootSaveData = rootSaveData;
            _leafSaveData = leafSaveData;

            _loadType = loadType;
            _guidPath = guidPath;

            _saveMateManager = saveMateManager;
            _saveFileContext = saveFileContext;
        }

        /// <summary>
        /// Tries to load a value of type <typeparamref name="T"/> associated with the given unique identifier.
        /// </summary>
        /// <typeparam name="T">The expected type of the loaded object.</typeparam>
        /// <param name="uniqueIdentifier">The identifier used to look up the object in the snapshot.</param>
        /// <param name="obj">The resulting object if found and successfully deserialized.</param>
        /// <returns>True if the object was successfully loaded; otherwise, false.</returns>
        public bool TryLoad<T>(string uniqueIdentifier, out T obj)
        {
            var res = TryLoad(typeof(T), uniqueIdentifier, out var innerObj);

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
        
        /// <summary>
        /// Tries to load a value of the specified type associated with the given unique identifier.
        /// </summary>
        /// <param name="type">The type of the object to load.</param>
        /// <param name="uniqueIdentifier">The unique identifier of the object to load.</param>
        /// <param name="obj">The resulting object if successful.</param>
        /// <returns>True if the object was found and successfully loaded; otherwise, false.</returns>
        public bool TryLoad(Type type, string uniqueIdentifier, out object obj)
        {
            return type.IsValueType || type ==  typeof(string) ? TryLoadValue(type, uniqueIdentifier, out obj) : TryLoadReference(type, uniqueIdentifier, out obj);
        }

        /// <summary>
        /// Tries to load a value type of type <typeparamref name="T"/> from the snapshot using the unique identifier.
        /// </summary>
        /// <typeparam name="T">The value type to retrieve.</typeparam>
        /// <param name="uniqueIdentifier">The identifier of the value to load.</param>
        /// <param name="value">The resulting value if successful.</param>
        /// <returns>True if the value was found and successfully loaded; otherwise, false.</returns>
        public bool TryLoadValue<T>(string uniqueIdentifier, out T value)
        {
            var res = TryLoadValue(typeof(T), uniqueIdentifier, out var obj);
            
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
        
        private bool TryLoadValue(Type type, string uniqueIdentifier, out object value)
        {
            if (_leafSaveData == null || !_leafSaveData.Values.TryGetValue(uniqueIdentifier, out var saveData))
            {
                Debug.LogWarning($"[SaveMate] Restore Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                                 $"Wasn't able to find save data for object type: '{type.FullName}'!");
                value = null;
                return false;
            }
            
            //null data content handling
            if (saveData.Type == JTokenType.Null)
            {
                value = default;
                return true;     
            }
            
            //unity object handling
            if (typeof(Object).IsAssignableFrom(type))
            {
                Debug.LogWarning($"[SaveMate] Restore Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                                 $"You tried to load an object of type {typeof(Object)} as a value! This is not allowed!");
                value = default;
                return false;
            }
            
            //savable handling
            if (typeof(ISaveStateHandler).IsAssignableFrom(type))
            {
                //TODO: exchange guidPath for string
                var res = TryLoadValueSavable(type, new GuidPath("", uniqueIdentifier), saveData, out value);
                return res;
            }

            //converter handling
            if (ConverterServiceProvider.ExistsAndCreate(type))
            {
                var res = TryLoadValueWithConverter(type, new GuidPath("", uniqueIdentifier), saveData, out value);
                return res;
            }

            //newtonsoft json conversion
            try
            {
                value = saveData.ToObject(type);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveMate] Restore Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                               $"Error serializing UnityEngine.Object '{type.Name}' using '{nameof(Newtonsoft.Json)}'. " +
                               $"Please implement the ISaveStateHandler or a create custom Converter for the object of type '{type.Name}'!" +
                               $"Exception: {e.GetType().Name} - {e.Message}\n{e.StackTrace}");
                throw;
            }
            
            return true;
        }
        
        private bool TryLoadValueSavable(Type type, GuidPath guidPath, JToken saveData, out object value)
        {
            var saveDataBuffer = saveData.ToObject<LeafSaveData>();
            var loadDataHandler = new RestoreSnapshotHandler(_rootSaveData, saveDataBuffer, _loadType, guidPath, 
                _saveFileContext, _saveMateManager);

            value = Activator.CreateInstance(type);
            ((ISaveStateHandler)value).OnRestoreState(loadDataHandler);

            return true;
        }
        
        private bool TryLoadValueWithConverter(Type type, GuidPath guidPath, JToken saveData, out object value)
        {
            var saveDataBuffer = saveData.ToObject<LeafSaveData>();
            var loadDataHandler = new RestoreSnapshotHandler(_rootSaveData, saveDataBuffer, _loadType, guidPath, 
                _saveFileContext, _saveMateManager);

            var convertable = ConverterServiceProvider.GetConverter(type);
            value = convertable.CreateStateObject(loadDataHandler);
            convertable.OnRestoreState(value, loadDataHandler);
            return true;
        }
        
        /// <summary>
        /// Tries to load a reference type of <typeparamref name="T"/> from the snapshot using the unique identifier.
        /// </summary>
        /// <typeparam name="T">The reference type to retrieve.</typeparam>
        /// <param name="uniqueIdentifier">The identifier of the reference to load.</param>
        /// <param name="reference">The resulting reference if successful.</param>
        /// <returns>True if the reference was found and successfully loaded; otherwise, false.</returns>
        public bool TryLoadReference<T>(string uniqueIdentifier, out T reference)
        {
            var res = TryLoadReference(typeof(T), uniqueIdentifier, out var obj);
            
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

        private bool TryLoadReference(Type type, string uniqueIdentifier, out object reference)
        {
            reference = default;

            if (!TryGetGuidPath(uniqueIdentifier, out var guidPath)) return false;

            if (!guidPath.HasValue)
            {
                reference = null;
                return true;
            }

            // Handle Unity type references
            if (typeof(ScriptableObject).IsAssignableFrom(type))
            {
                return TryGetScriptableObjectReference(guidPath.Value, uniqueIdentifier, out reference);
            }

            if (type == typeof(GameObject))
            {
                return TryGetGameObjectReference(guidPath.Value, uniqueIdentifier, obj => obj, out reference);
            }

            if (type == typeof(Transform))
            {
                return TryGetGameObjectReference(guidPath.Value, uniqueIdentifier, obj => obj.transform, out reference);
            }

            if (type == typeof(RectTransform))
            {
                return TryGetGameObjectReference(guidPath.Value, uniqueIdentifier, obj => (RectTransform)obj.transform, out reference);
            }

            if (typeof(Component).IsAssignableFrom(type))
            {
                return TryGetComponentReference(type, guidPath.Value, uniqueIdentifier, out reference);
            }

            // Try to load a created object or create a new one
            return TryGetCreatedObject(type, guidPath.Value, uniqueIdentifier, out reference) || 
                   CreateObject(type, guidPath.Value, uniqueIdentifier, out reference);
        }
        
        private bool TryGetGuidPath(string uniqueIdentifier, out GuidPath? guidPath)
        {
            if (!_leafSaveData.References.TryGetValue(uniqueIdentifier, out guidPath))
            {
                Debug.LogWarning($"[SaveMate] Restore Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                                 $"Wasn't able to find save data.");
                return false;
            }
            
            return true;
        }

        private bool TryGetScriptableObjectReference(GuidPath guidPath, string uniqueIdentifier, out object reference)
        {
            reference = null;
            
            if (_saveMateManager.GuidToScriptableObjectLookup.TryGetValue(guidPath, out var scriptableObject))
            {
                reference = scriptableObject;
                return true;
            }
            
            Debug.LogWarning($"[SaveMate] Restore Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                             $"Wasn't able to find the requested {nameof(ScriptableObject)}. " +
                             $"Please make sure your it is added to the {nameof(AssetRegistry)}!");
            return false;
        }

        private bool TryGetGameObjectReference<T>(GuidPath guidPath, string uniqueIdentifier, Func<GameObject, T> convertAction, out object reference)
        {
            reference = null;
            
            if (GetGuidPathGameObject(guidPath, out var savable))
            {
                reference = convertAction.Invoke(savable);
                return true;
            }
            
            Debug.LogWarning($"[SaveMate] Restore Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                             $"Wasn't able to find the object {nameof(T)}. Please make sure it is referencable by " +
                             $"adding the '{nameof(Savable)}' component to the related {nameof(GameObject)}!");
            return false;
        }

        private bool TryGetComponentReference(Type type, GuidPath guidPath, string uniqueIdentifier, out object reference)
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
            
            Debug.LogWarning($"[SaveMate] Restore Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                             $"Wasn't able to find the requested type '{type.Name}'. Please make sure it is referencable by " +
                             $"adding the '{nameof(Savable)}' component to the related {nameof(GameObject)}!");
            return false;
        }
        
        private bool GetGuidPathGameObject(GuidPath guidPath, out GameObject gameObject)
        {
            gameObject = default;
            
            foreach (var saveSceneManager in _saveMateManager.GetTrackedSaveSceneManagers())
            {
                if (guidPath.SceneName != saveSceneManager.SceneName) continue;
                    
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
            
            foreach (var saveSceneManager in _saveMateManager.GetTrackedSaveSceneManagers())
            {
                if (guidPath.SceneName != saveSceneManager.SceneName) continue;
                    
                if (saveSceneManager.GuidToComponentLookup.TryGetValue(guidPath, out component))
                {
                    return true;
                }
                
                return false;
            }

            return false;
        }

        private bool TryGetCreatedObject(Type type, GuidPath guidPath, string uniqueIdentifier, out object reference)
        {
            reference = default;
            
            if (_saveFileContext.GuidToCreatedNonUnityObjectLookup.TryGetValue(_loadType, guidPath, out reference))
            {
                if (reference.GetType() != type)
                {
                    Debug.LogWarning($"[SaveMate] Restore Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                                     $"The requested object was already created as type '{reference.GetType().Name}'. " +
                                     $"You tried to return it with a different type, which is not allowed!");
                    return false;
                }
                
                return true;
            }

            return false;
        }

        private bool CreateObject(Type type, GuidPath guidPath, string uniqueIdentifier, out object reference)
        {
            reference = null;

            if (!TryGetLeafSaveData(guidPath, out var leafSaveData))
            {
                Debug.LogWarning($"[SaveMate - Internal Error] Restore Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                                 $"Saved GUID references a missing object within the same save file!");
                return false;
            }
            
            var loadDataHandler = new RestoreSnapshotHandler(_rootSaveData, leafSaveData, _loadType, guidPath, 
                _saveFileContext, _saveMateManager);
            
            
            //savable handling
            if (typeof(ISaveStateHandler).IsAssignableFrom(type))
            {
                return CreateObjectISavable(loadDataHandler, type, guidPath, out reference);
            }

            //converter handling
            if (ConverterServiceProvider.ExistsAndCreate(type))
            {
                CreateObjectConverter(loadDataHandler, type, guidPath, out reference);
                return true;
            }

            return CreateObjectNewtonsoft(leafSaveData, type, guidPath, uniqueIdentifier, out reference);
        }

        private bool TryGetLeafSaveData(GuidPath guidPath, out LeafSaveData leafSaveData)
        {
            leafSaveData = default;
            if (guidPath.SceneName == SaveLoadUtility.ScriptableObjectDataName)
            {
                if (!_rootSaveData.ScriptableObjectSaveData.TryGetLeafSaveData(guidPath, out leafSaveData)) return false;
            }
            else
            {
                if (!_rootSaveData.TryGetSceneData(guidPath.SceneName, out var sceneData)) return false;

                if (!sceneData.ActiveSaveData.TryGetLeafSaveData(guidPath, out leafSaveData)) return false;
            }

            return true;
        }

        private bool CreateObjectISavable(RestoreSnapshotHandler restoreSnapshotHandler, Type type, GuidPath guidPath, out object reference)
        {
            reference = Activator.CreateInstance(type);
            if (reference is not ISaveStateHandler objectSavable)
                return false;

            _saveFileContext.GuidToCreatedNonUnityObjectLookup.Add(_loadType, guidPath, reference);
            objectSavable.OnRestoreState(restoreSnapshotHandler);
            return true;
        }

        private void CreateObjectConverter(RestoreSnapshotHandler restoreSnapshotHandler, Type type, GuidPath guidPath, out object reference)
        {
            var converter = ConverterServiceProvider.GetConverter(type);
            reference = converter.CreateStateObject(restoreSnapshotHandler);
            _saveFileContext.GuidToCreatedNonUnityObjectLookup.Add(_loadType, guidPath, reference);
            converter.OnRestoreState(reference, restoreSnapshotHandler);
        }

        private bool CreateObjectNewtonsoft(LeafSaveData leafSaveData, Type type, GuidPath guidPath, string uniqueIdentifier, out object reference)
        {
            reference = default;
            
            try
            {
                var jObject = leafSaveData.Values[uniqueIdentifier];
                if (jObject != null)
                {
                    reference = jObject.ToObject(type);
                    _saveFileContext.GuidToCreatedNonUnityObjectLookup.Add(_loadType, guidPath, reference);
                    return true;
                }

                Debug.LogWarning($"[SaveMate - Internal Error] Restore Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                                 $"Wasn't able to find the save data object. This is likely a bug!");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveMate] Restore Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                               $"Error serializing UnityEngine.Object '{type.Name}' using '{nameof(Newtonsoft.Json)}'. " +
                               $"Please implement the ISaveStateHandler or a create custom Converter for the object of type '{type.Name}'!" +
                               $"Exception: {e.GetType().Name} - {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
    }
}
