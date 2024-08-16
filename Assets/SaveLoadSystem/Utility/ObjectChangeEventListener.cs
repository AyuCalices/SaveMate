using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Utility
{
    [InitializeOnLoad]
    public class ObjectChangeEventListener
    {
        static ObjectChangeEventListener()    
        {
            ObjectChangeEvents.changesPublished += ChangesPublished;
        }

        private static void ChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                var type = stream.GetEventType(i);
                switch (type)
                {
                    case ObjectChangeKind.CreateGameObjectHierarchy:
                        CreateGameObjectHierarchy(i, type, ref stream, false);
                        break;

                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                        ChangeGameObjectStructureHierarchy(i, type, ref stream, false);
                        break;

                    case ObjectChangeKind.ChangeGameObjectStructure:
                        ChangeGameObjectStructure(i, type, ref stream, false);
                        break;

                    case ObjectChangeKind.ChangeGameObjectParent:
                        ChangeGameObjectParent(i, type, ref stream, false);
                        break;

                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                        ChangeGameObjectOrComponentProperties(i, type, ref stream, false);
                        break;
        
                    /*
                    case ObjectChangeKind.ChangeScene:
                        ChangeScene(i, type, ref stream, false);
                        break;
                    
                    case ObjectChangeKind.DestroyGameObjectHierarchy:
                        DestroyGameObjectHierarchy(i, type, ref stream, false);
                        break;

                    case ObjectChangeKind.CreateAssetObject:
                        CreateAssetObject(i, type, ref stream, false);
                        break;

                    case ObjectChangeKind.DestroyAssetObject:
                        DestroyAssetObject(i, type, ref stream, false);
                        break;

                    case ObjectChangeKind.ChangeAssetObjectProperties:
                        ChangeAssetObjectProperties(i, type, ref stream, false);
                        break;

                    case ObjectChangeKind.UpdatePrefabInstances:
                        UpdatePrefabInstances(i, type, ref stream, false);
                        break;
                    */
                }
            }
        }
        
        private static void CreateGameObjectHierarchy(int i, ObjectChangeKind type, ref ObjectChangeEventStream stream, bool debug)
        {
            stream.GetCreateGameObjectHierarchyEvent(i, out var createGameObjectHierarchy);
            var newGameObject = EditorUtility.InstanceIDToObject(createGameObjectHierarchy.instanceId) as GameObject;

            if (debug)
            {
                Debug.Log($"{type}: {newGameObject} in scene {createGameObjectHierarchy.scene}.");
            }
            
            if (newGameObject.TryGetComponent(out ICreateGameObjectHierarchy createGameObjectHierarchyEvent))
            {
                createGameObjectHierarchyEvent.OnCreateGameObjectHierarchy();
            }
        }
        
        private static void ChangeGameObjectStructureHierarchy(int i, ObjectChangeKind type, ref ObjectChangeEventStream stream, bool debug)
        {
            stream.GetChangeGameObjectStructureHierarchyEvent(i, out var changeGameObjectStructureHierarchy);
            var gameObject = EditorUtility.InstanceIDToObject(changeGameObjectStructureHierarchy.instanceId) as GameObject;
            if (gameObject.IsDestroyed()) return;

            if (debug)
            {
                Debug.Log($"{type}: {gameObject} in scene {changeGameObjectStructureHierarchy.scene}.");
            }
            
            foreach (var gameObjectStructureHierarchy in gameObject.GetComponents<IChangeGameObjectStructureHierarchy>())
            {
                gameObjectStructureHierarchy.OnChangeGameObjectStructureHierarchy();
            }
        }
        
        private static void ChangeGameObjectStructure(int i, ObjectChangeKind type, ref ObjectChangeEventStream stream, bool debug)
        {
            stream.GetChangeGameObjectStructureEvent(i, out var changeGameObjectStructure);
            var gameObjectStructure = EditorUtility.InstanceIDToObject(changeGameObjectStructure.instanceId) as GameObject;
            if (gameObjectStructure.IsDestroyed()) return;

            if (debug)
            {
                Debug.Log($"{type}: {gameObjectStructure} in scene {changeGameObjectStructure.scene}.");
            }
            
            if (gameObjectStructure.TryGetComponent(out IChangeGameObjectStructure changeGameObjectStructureEvent))
            {
                changeGameObjectStructureEvent.OnChangeGameObjectStructure();
            }
        }
        
        private static void ChangeGameObjectParent(int i, ObjectChangeKind type, ref ObjectChangeEventStream stream, bool debug)
        {
            stream.GetChangeGameObjectParentEvent(i, out var changeGameObjectParent);
            var gameObjectChanged = EditorUtility.InstanceIDToObject(changeGameObjectParent.instanceId) as GameObject;
            var newParentGo = EditorUtility.InstanceIDToObject(changeGameObjectParent.newParentInstanceId) as GameObject;
            var previousParentGo = EditorUtility.InstanceIDToObject(changeGameObjectParent.previousParentInstanceId) as GameObject;
            if (gameObjectChanged.IsDestroyed()) return;

            if (debug)
            {
                Debug.Log($"{type}: {gameObjectChanged} from {previousParentGo} to {newParentGo} from scene {changeGameObjectParent.previousScene} to scene {changeGameObjectParent.newScene}.");
            }
            
            if (gameObjectChanged.TryGetComponent(out IChangeGameObjectParent changeGameObjectParentEvent))
            {
                changeGameObjectParentEvent.OnChangeGameObjectParent(newParentGo, previousParentGo);
            }
        }
        
        private static void ChangeGameObjectOrComponentProperties(int i, ObjectChangeKind type, ref ObjectChangeEventStream stream, bool debug)
        {
            stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var changeGameObjectOrComponent);
            var goOrComponent = EditorUtility.InstanceIDToObject(changeGameObjectOrComponent.instanceId);
                        
            if (goOrComponent is GameObject go && !go.IsDestroyed())
            {
                if (debug)
                {
                    Debug.Log($"{type}: GameObject {go} change properties in scene {changeGameObjectOrComponent.scene}.");
                }
                            
                if (go.TryGetComponent(out IChangeGameObjectProperties changeGameObjectPropertiesEvent))
                {
                    changeGameObjectPropertiesEvent.OnChangeGameObjectProperties();
                }
            }
            else if (goOrComponent is Component component && component.gameObject != null)
            {
                if (debug)
                {
                    Debug.Log($"{type}: Component {component} change properties in scene {changeGameObjectOrComponent.scene}.");
                }
                            
                if (component.TryGetComponent(out IChangeComponentProperties changeComponentPropertiesEvent))
                {
                    changeComponentPropertiesEvent.OnChangeComponentProperties();
                }
            }
        }
        
        /*
        private static void ChangeScene(int i, ObjectChangeKind type, ref ObjectChangeEventStream stream, bool debug)
        {
            stream.GetChangeSceneEvent(i, out var changeSceneEvent);
            
            if (debug)
            {
                Debug.Log($"{type}: {changeSceneEvent.scene}");
            }
        }
        
        private static void DestroyGameObjectHierarchy(int i, ObjectChangeKind type, ref ObjectChangeEventStream stream, bool debug)
        {
            stream.GetDestroyGameObjectHierarchyEvent(i, out var destroyGameObjectHierarchyEvent);
            // The destroyed GameObject can not be converted with EditorUtility.InstanceIDToObject as it has already been destroyed.
            var destroyParentGo = EditorUtility.InstanceIDToObject(destroyGameObjectHierarchyEvent.parentInstanceId) as GameObject;

            if (debug)
            {
                Debug.Log($"{type}: {destroyGameObjectHierarchyEvent.instanceId} with parent {destroyParentGo} in scene {destroyGameObjectHierarchyEvent.scene}.");
            }
        }
        
        private static void CreateAssetObject(int i, ObjectChangeKind type, ref ObjectChangeEventStream stream, bool debug)
        {
            stream.GetCreateAssetObjectEvent(i, out var createAssetObjectEvent);
            var createdAsset = EditorUtility.InstanceIDToObject(createAssetObjectEvent.instanceId);
            var createdAssetPath = AssetDatabase.GUIDToAssetPath(createAssetObjectEvent.guid);

            if (debug)
            {
                Debug.Log($"{type}: {createdAsset} at {createdAssetPath} in scene {createAssetObjectEvent.scene}.");
            }
        }
        
        private static void DestroyAssetObject(int i, ObjectChangeKind type, ref ObjectChangeEventStream stream, bool debug)
        {
            stream.GetDestroyAssetObjectEvent(i, out var destroyAssetObjectEvent);
            // The destroyed asset can not be converted with EditorUtility.InstanceIDToObject as it has already been destroyed.

            if (debug)
            {
                Debug.Log($"{type}: Instance Id {destroyAssetObjectEvent.instanceId} with Guid {destroyAssetObjectEvent.guid} in scene {destroyAssetObjectEvent.scene}.");
            }
        }
        
        private static void ChangeAssetObjectProperties(int i, ObjectChangeKind type, ref ObjectChangeEventStream stream, bool debug)
        {
            stream.GetChangeAssetObjectPropertiesEvent(i, out var changeAssetObjectPropertiesEvent);
            var changeAsset = EditorUtility.InstanceIDToObject(changeAssetObjectPropertiesEvent.instanceId);
            var changeAssetPath = AssetDatabase.GUIDToAssetPath(changeAssetObjectPropertiesEvent.guid);

            if (debug)
            {
                Debug.Log($"{type}: {changeAsset} at {changeAssetPath} in scene {changeAssetObjectPropertiesEvent.scene}.");
            }
        }
        
        private static void UpdatePrefabInstances(int i, ObjectChangeKind type, ref ObjectChangeEventStream stream, bool debug)
        {
            stream.GetUpdatePrefabInstancesEvent(i, out var updatePrefabInstancesEvent);
            string s = "";
            s += $"{type}: scene {updatePrefabInstancesEvent.scene}. Instances ({updatePrefabInstancesEvent.instanceIds.Length}):\n";
            foreach (var prefabId in updatePrefabInstancesEvent.instanceIds)
            {
                s += EditorUtility.InstanceIDToObject(prefabId) + "\n";
            }

            if (debug)
            {
                Debug.Log(s);
            }
        }
        */
    }
}
