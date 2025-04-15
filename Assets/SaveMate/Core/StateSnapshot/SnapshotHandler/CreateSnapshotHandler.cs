using Newtonsoft.Json.Linq;
using SaveMate.Core.DataTransferObject;
using SaveMate.Core.SaveComponents.ManagingScope;
using SaveMate.Core.StateSnapshot.Converter;
using SaveMate.Core.StateSnapshot.Interface;
using SaveMate.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaveMate.Core.StateSnapshot.SnapshotHandler
{
    /// <summary>
    /// The <see cref="CreateSnapshotHandler"/> class is responsible for managing the serialization and storage of data
    /// within the save/load system, specifically handling the addition and reference management of savable objects.
    /// </summary>
    public readonly struct CreateSnapshotHandler
    {
        private readonly BranchSaveData _branchSaveData;
        private readonly LeafSaveData _leafSaveData;
        private readonly GuidPath _guidPath;
        private readonly string _sceneName;

        private readonly SaveFileContext _saveFileContext;
        private readonly SaveMateManager _saveMateManager;

        public CreateSnapshotHandler(BranchSaveData branchSaveData, LeafSaveData leafSaveData, GuidPath guidPath, string sceneName, 
            SaveFileContext saveFileContext, SaveMateManager saveMateManagers)
        {
            _branchSaveData = branchSaveData;
            _leafSaveData = leafSaveData;
            _guidPath = guidPath;
            _sceneName = sceneName;

            _saveFileContext = saveFileContext;
            _saveMateManager = saveMateManagers;
        }

        public void Save<T>(string uniqueIdentifier, T obj)
        {
            if (typeof(T).IsValueType || obj is string)
            {
                SaveAsValue(uniqueIdentifier, obj);
            }
            else
            {
                SaveAsReferencable(uniqueIdentifier, obj);
            }
        }

        /// <summary>
        /// Adds an object to the save data buffer using a unique identifier.
        /// Supports all valid types for Newtonsoft Json. Uses less disk space and is faster than Referencable Saving and Loading.
        /// </summary>
        /// <param name="uniqueIdentifier">The unique identifier for the object to be serialized.</param>
        /// <param name="obj">The object to be serialized and added to the buffer.</param>
        public void SaveAsValue<T>(string uniqueIdentifier, T obj)
        {
            if (obj.IsUnityNull())
            {
                _leafSaveData.Values[uniqueIdentifier] = null;
            }
            else if (obj is UnityEngine.Object)
            {
                Debug.LogError($"You can't save an object of type {typeof(Object)} as a value!");
            }
            else if (obj is ISaveStateHandler savable)
            {
                var newPath = new GuidPath("", uniqueIdentifier);
                var leafSaveData = new LeafSaveData();
                var saveDataHandler = new CreateSnapshotHandler(_branchSaveData, leafSaveData, newPath, _sceneName, _saveFileContext, _saveMateManager);
                
                savable.OnCaptureState(saveDataHandler);
                
                _leafSaveData.Values[uniqueIdentifier] = JToken.FromObject(leafSaveData);
            }
            else if (ConverterServiceProvider.ExistsAndCreate(typeof(T)))
            {
                var newPath = new GuidPath("", uniqueIdentifier);
                var leafSaveData = new LeafSaveData();
                var saveDataHandler = new CreateSnapshotHandler(_branchSaveData, leafSaveData, newPath, _sceneName, 
                    _saveFileContext, _saveMateManager);

                ConverterServiceProvider.GetConverter(typeof(T)).OnCaptureState(obj, saveDataHandler);
                
                _leafSaveData.Values[uniqueIdentifier] = JToken.FromObject(leafSaveData);
            }
            else
            {
                _leafSaveData.Values[uniqueIdentifier] = JToken.FromObject(obj);
            }
        }
        
        public void SaveAsReferencable<T>(string uniqueIdentifier, T obj)
        {
            _leafSaveData.References[uniqueIdentifier] = ConvertToPath(uniqueIdentifier, obj);
        }
        
        /// <summary>
        /// Attempts to convert an object to a GUID path, so the reference can be identified at deserialization.
        /// </summary>
        /// <param name="uniqueIdentifier">The unique identifier for the object.</param>
        /// <param name="objectToSave">The object to convert to a GUID path.</param>
        /// <param name="guidPath">The resulting GUID path if the conversion is successful.</param>
        /// <returns><c>true</c> if the object was successfully converted to a GUID path; otherwise, <c>false</c>.</returns>
        private GuidPath? ConvertToPath<T>(string uniqueIdentifier, T objectToSave)
        {
            if (objectToSave.IsUnityNull())
            {
                return null;
            }
            
            //search for a unity reference
            GuidPath guidPath;
            if (objectToSave is Component component)
            {
                //components with a guid must be processed because: 1. prevent ambiguity between duplicates 2. clearly identify components that inherit from ISaveStateHandler
                if (GetComponentGuidPath(component, out var convertedToPath)) return convertedToPath;
                if (GetGameObjectGuidPath(component.gameObject, out var convertToPath)) return convertToPath;
                
                //TODO: debug
            }
            else if (objectToSave is GameObject gameObject)
            {
                if (GetGameObjectGuidPath(gameObject, out var convertToPath))
                {
                    return convertToPath;
                }
                
                //TODO: debug
            }
            else if (objectToSave is ScriptableObject scriptableObject)
            {
                if (_saveMateManager.ScriptableObjectToGuidLookup.TryGetValue(scriptableObject, out guidPath))
                {
                    return guidPath;
                }
                
                //TODO: debug
            }
            else
            {
                //make sure there is a unique id for each Non-Unity-Object
                if (!_saveFileContext.SavedNonUnityObjectToGuidLookup.TryGetValue(objectToSave, out var stringPath))
                {
                    guidPath = new GuidPath(_guidPath, uniqueIdentifier);
                
                    _saveFileContext.SavedNonUnityObjectToGuidLookup.Add(objectToSave, guidPath.ToString());
                    _saveFileContext.GuidToCreatedNonUnityObjectLookup.Upsert(guidPath, objectToSave);
                }
                else
                {
                    guidPath = GuidPath.FromString(stringPath);
                }

                if (guidPath.SceneName == _sceneName)
                {
                    UpsertNonUnityObject(objectToSave, guidPath);
                }
            
                return guidPath;
            }
            
            return null;
        }

        private bool GetGameObjectGuidPath(GameObject gameObject, out GuidPath convertToPath)
        {
            convertToPath = default;
            
            foreach (var saveSceneManager in _saveMateManager.GetTrackedSaveSceneManagers())
            {
                if (_sceneName != saveSceneManager.SceneName) continue;
                    
                if (saveSceneManager.SavableGameObjectToGuidLookup.TryGetValue(gameObject, out convertToPath))
                {
                    return true;
                }
                
                return false;
            }

            return false;
        }
        
        private bool GetComponentGuidPath(Component component, out GuidPath convertToPath)
        {
            convertToPath = default;
            
            foreach (var saveSceneManager in _saveMateManager.GetTrackedSaveSceneManagers())
            {
                if (_sceneName != saveSceneManager.SceneName) continue;
                    
                if (saveSceneManager.ComponentToGuidLookup.TryGetValue(component, out convertToPath))
                {
                    return true;
                }
                
                return false;
            }

            return false;
        }

        private void UpsertNonUnityObject<T>(T objectToSave, GuidPath guidPath)
        {
            if (objectToSave is ISaveStateHandler targetSavable)
            {
                var leafSaveData = new LeafSaveData();
                _branchSaveData.UpsertLeafSaveData(guidPath, leafSaveData);
            
                targetSavable.OnCaptureState(new CreateSnapshotHandler(_branchSaveData, leafSaveData, guidPath, _sceneName, 
                    _saveFileContext, _saveMateManager));
            }
            else if (ConverterServiceProvider.ExistsAndCreate(typeof(T)))
            {
                var leafSaveData = new LeafSaveData();
                
                _branchSaveData.UpsertLeafSaveData(guidPath, leafSaveData);
                        
                var saveDataHandler = new CreateSnapshotHandler(_branchSaveData, leafSaveData, guidPath, _sceneName, 
                    _saveFileContext, _saveMateManager);
                ConverterServiceProvider.GetConverter(typeof(T)).OnCaptureState(objectToSave, saveDataHandler);
            }
            else
            {
                var leafSaveData = new LeafSaveData();
                
                _branchSaveData.UpsertLeafSaveData(guidPath, leafSaveData);
                
                leafSaveData.Values.Add("SerializeRef", JToken.FromObject(objectToSave));
            }
        }
    }
}
