using System;
using Newtonsoft.Json.Linq;
using SaveMate.Runtime.Core.DataTransferObject;
using SaveMate.Runtime.Core.SaveComponents.AssetScope;
using SaveMate.Runtime.Core.SaveComponents.GameObjectScope;
using SaveMate.Runtime.Core.SaveComponents.ManagingScope;
using SaveMate.Runtime.Core.StateSnapshot.Converter;
using SaveMate.Runtime.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaveMate.Runtime.Core.StateSnapshot
{
    /// <summary>
    /// The <see cref="CreateSnapshotHandler"/> struct handles the creation of snapshots for savable objects in the SaveMate system.
    /// Responsible for serializing data into either direct values or by a referenceable GUID,
    /// depending on the type and context of the object.
    /// </summary>
    public readonly struct CreateSnapshotHandler
    {
        private readonly BranchSaveData _branchSaveData;
        private readonly LeafSaveData _leafSaveData;
        private readonly GuidPath _guidPath;
        private readonly string _sceneName;

        private readonly SaveFileContext _saveFileContext;
        private readonly SaveMateManager _saveMateManager;

        internal CreateSnapshotHandler(BranchSaveData branchSaveData, LeafSaveData leafSaveData, GuidPath guidPath, string sceneName, 
            SaveFileContext saveFileContext, SaveMateManager saveMateManagers)
        {
            _branchSaveData = branchSaveData;
            _leafSaveData = leafSaveData;
            _guidPath = guidPath;
            _sceneName = sceneName;

            _saveFileContext = saveFileContext;
            _saveMateManager = saveMateManagers;
        }

        /// <summary>
        /// Saves an object to the snapshot using a unique identifier. Chooses between
        /// value-based saving or referencable saving based on the object's type.
        /// </summary>
        /// <typeparam name="T">The type of the object to save.</typeparam>
        /// <param name="uniqueIdentifier">The unique key for saving the object.</param>
        /// <param name="obj">The object to save.</param>
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
        /// Saves an object as a direct value in the save data. This method is optimized for 
        /// primitive types, strings, and other directly serializable objects.
        /// </summary>
        /// <typeparam name="T">The type of the object to save.</typeparam>
        /// <param name="uniqueIdentifier">The unique key for saving the object.</param>
        /// <param name="obj">The object to serialize and store.</param>
        public void SaveAsValue<T>(string uniqueIdentifier, T obj)
        {
            if (obj.IsUnityNull())
            {
                _leafSaveData.Values[uniqueIdentifier] = null;
                return;
            }

            if (obj is Object)
            {
                Debug.LogWarning($"[SaveMate] Create Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                                 $"You tried to save an object of type {typeof(Object)} as a value! This is not allowed!");
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
                try
                {
                    _leafSaveData.Values[uniqueIdentifier] = JToken.FromObject(obj);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveMate] Create Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                                   $"Error serializing UnityEngine.Object '{typeof(T)}' using '{nameof(Newtonsoft.Json)}'. " +
                                   $"Please implement the ISaveStateHandler or a create custom Converter for the object of type '{typeof(T)}'!" +
                                   $"Exception: {e.GetType().Name} - {e.Message}\n{e.StackTrace}");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Saves an object as a reference by attempting to resolve it by a GUID.
        /// This method is typically used for Unity objects or other complex savables.
        /// </summary>
        /// <typeparam name="T">The type of the object to save.</typeparam>
        /// <param name="uniqueIdentifier">The unique key for referencing the object.</param>
        /// <param name="obj">The object to be referenced.</param>
        public void SaveAsReferencable<T>(string uniqueIdentifier, T obj)
        {
            _leafSaveData.References[uniqueIdentifier] = ConvertToPath(uniqueIdentifier, obj);
        }
        
        private GuidPath? ConvertToPath<T>(string uniqueIdentifier, T objectToSave)
        {
            if (objectToSave.IsUnityNull())
            {
                return null;
            }
            
            //search for a unity reference
            if (objectToSave is Component component)
            {
                //components with a guid must be processed because: 1. prevent ambiguity between duplicates 2. clearly identify components that inherit from ISaveStateHandler
                if (GetComponentGuidPath(component, uniqueIdentifier, out var convertedToPath)) return convertedToPath;
                if (GetGameObjectGuidPath(component.gameObject, uniqueIdentifier, out var convertToPath)) return convertToPath;
            }
            else if (objectToSave is GameObject gameObject)
            {
                if (GetGameObjectGuidPath(gameObject, uniqueIdentifier, out var convertToPath)) return convertToPath;
            }
            else if (objectToSave is ScriptableObject scriptableObject)
            {
                if (_saveMateManager.ScriptableObjectToGuidLookup.TryGetValue(scriptableObject, out var guidPath)) return guidPath;
                
                Debug.LogWarning($"[SaveMate] Create Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                                 $"Referencing for {nameof(ScriptableObject)} {scriptableObject.name} is not enabled. " +
                                 $"Please make sure it is added to a {nameof(AssetRegistry)}, which is added to the {nameof(_saveMateManager)}.");
            }
            else
            {
                //make sure there is a unique id for each Non-Unity-Object
                GuidPath guidPath;
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
                    UpsertNonUnityObject(objectToSave, uniqueIdentifier, guidPath);
                }
                
                return guidPath;
            }

            return null;
        }

        private bool GetGameObjectGuidPath(GameObject gameObject, string uniqueIdentifier, out GuidPath convertToPath)
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

            Debug.LogWarning($"[SaveMate] Create Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                             $"Referencing for {nameof(gameObject)} {gameObject.name} is not enabled. Please add a {nameof(Savable)} {nameof(Component)}.");
            return false;
        }
        
        private bool GetComponentGuidPath(Component component, string uniqueIdentifier, out GuidPath convertToPath)
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

            Debug.LogWarning($"[SaveMate] Create Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                             $"Referencing for GameObject {component.gameObject.name} is not enabled. Please add a {nameof(Savable)} {nameof(Component)}.");
            return false;
        }

        private void UpsertNonUnityObject<T>(T objectToSave, string uniqueIdentifier, GuidPath guidPath)
        {
            var leafSaveData = new LeafSaveData();
            _branchSaveData.UpsertLeafSaveData(guidPath, leafSaveData);
            
            if (objectToSave is ISaveStateHandler targetSavable)
            {
                targetSavable.OnCaptureState(new CreateSnapshotHandler(_branchSaveData, leafSaveData, guidPath, _sceneName, 
                    _saveFileContext, _saveMateManager));
            }
            else if (ConverterServiceProvider.ExistsAndCreate(typeof(T)))
            {
                var saveDataHandler = new CreateSnapshotHandler(_branchSaveData, leafSaveData, guidPath, _sceneName, 
                    _saveFileContext, _saveMateManager);
                ConverterServiceProvider.GetConverter(typeof(T)).OnCaptureState(objectToSave, saveDataHandler);
            }
            else
            {
                try
                {
                    leafSaveData.Values.Add(uniqueIdentifier, JToken.FromObject(objectToSave));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveMate] Create Snapshot Error at '{_guidPath.ToString()}' for identifier '{uniqueIdentifier}': " +
                                   $"Error serializing UnityEngine.Object '{typeof(T)}' using '{nameof(Newtonsoft.Json)}'. " +
                                   $"Please implement the ISaveStateHandler or a create custom Converter for the object of type '{typeof(T)}'!" +
                                   $"Exception: {e.GetType().Name} - {e.Message}\n{e.StackTrace}");
                    throw;
                }
            }
        }
    }
}
