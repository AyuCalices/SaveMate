using UnityEditor;
using UnityEngine;

namespace SaveLoadCore.Utility
{
    [InitializeOnLoad]
    public class ObjectChangeEventListener
    {
        static ObjectChangeEventListener()    
        {
            ObjectChangeEvents.changesPublished += ChangesPublished;
        }

        static void ChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                var type = stream.GetEventType(i);
                switch (type)
                {
                    case ObjectChangeKind.ChangeScene:
                        stream.GetChangeSceneEvent(i, out var changeSceneEvent);
                        Debug.Log($"{type}: {changeSceneEvent.scene}");
                        break;

                    case ObjectChangeKind.CreateGameObjectHierarchy:                //interface
                        stream.GetCreateGameObjectHierarchyEvent(i, out var createGameObjectHierarchy);
                        var newGameObject = EditorUtility.InstanceIDToObject(createGameObjectHierarchy.instanceId) as GameObject;
                        Debug.Log($"{type}: {newGameObject} in scene {createGameObjectHierarchy.scene}.");

                        if (newGameObject.TryGetComponent(out ICreateGameObjectHierarchy createGameObjectHierarchyEvent))
                        {
                            createGameObjectHierarchyEvent.OnCreateGameObjectHierarchy();
                        }
                        break;

                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:                //interface
                        stream.GetChangeGameObjectStructureHierarchyEvent(i, out var changeGameObjectStructureHierarchy);
                        var gameObject = EditorUtility.InstanceIDToObject(changeGameObjectStructureHierarchy.instanceId) as GameObject;
                        if (gameObject.IsDestroyed()) return;
                        
                        Debug.Log($"{type}: {gameObject} in scene {changeGameObjectStructureHierarchy.scene}.");
                        foreach (var gameObjectStructureHierarchy in gameObject.GetComponents<IChangeGameObjectStructureHierarchy>())
                        {
                            gameObjectStructureHierarchy.OnChangeGameObjectStructureHierarchy();
                        }
                        break;

                    case ObjectChangeKind.ChangeGameObjectStructure:                //interface
                        stream.GetChangeGameObjectStructureEvent(i, out var changeGameObjectStructure);
                        var gameObjectStructure = EditorUtility.InstanceIDToObject(changeGameObjectStructure.instanceId) as GameObject;
                        if (gameObjectStructure.IsDestroyed()) return;
                        
                        Debug.Log($"{type}: {gameObjectStructure} in scene {changeGameObjectStructure.scene}.");
                        if (gameObjectStructure.TryGetComponent(out IChangeGameObjectStructure changeGameObjectStructureEvent))
                        {
                            changeGameObjectStructureEvent.OnChangeGameObjectStructure();
                        }
                        break;

                    case ObjectChangeKind.ChangeGameObjectParent:                //interface
                        stream.GetChangeGameObjectParentEvent(i, out var changeGameObjectParent);
                        var gameObjectChanged = EditorUtility.InstanceIDToObject(changeGameObjectParent.instanceId) as GameObject;
                        var newParentGo = EditorUtility.InstanceIDToObject(changeGameObjectParent.newParentInstanceId) as GameObject;
                        var previousParentGo = EditorUtility.InstanceIDToObject(changeGameObjectParent.previousParentInstanceId) as GameObject;
                        if (gameObjectChanged.IsDestroyed()) return;
                        
                        Debug.Log($"{type}: {gameObjectChanged} from {previousParentGo} to {newParentGo} from scene {changeGameObjectParent.previousScene} to scene {changeGameObjectParent.newScene}.");
                        if (gameObjectChanged.TryGetComponent(out IChangeGameObjectParent changeGameObjectParentEvent))
                        {
                            changeGameObjectParentEvent.OnChangeGameObjectParent(newParentGo, previousParentGo);
                        }
                        break;

                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties:                //interface
                        stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var changeGameObjectOrComponent);
                        var goOrComponent = EditorUtility.InstanceIDToObject(changeGameObjectOrComponent.instanceId);
                        
                        if (goOrComponent is GameObject go && !go.IsDestroyed())
                        {
                            Debug.Log($"{type}: GameObject {go} change properties in scene {changeGameObjectOrComponent.scene}.");
                            
                            if (go.TryGetComponent(out IChangeGameObjectProperties changeGameObjectPropertiesEvent))
                            {
                                changeGameObjectPropertiesEvent.OnChangeGameObjectProperties();
                            }
                        }
                        else if (goOrComponent is Component component && component.gameObject != null)
                        {
                            Debug.Log($"{type}: Component {component} change properties in scene {changeGameObjectOrComponent.scene}.");
                            
                            if (component.TryGetComponent(out IChangeComponentProperties changeComponentPropertiesEvent))
                            {
                                changeComponentPropertiesEvent.OnChangeComponentProperties();
                            }
                        }
                        break;

                    case ObjectChangeKind.DestroyGameObjectHierarchy:
                        stream.GetDestroyGameObjectHierarchyEvent(i, out var destroyGameObjectHierarchyEvent);
                        // The destroyed GameObject can not be converted with EditorUtility.InstanceIDToObject as it has already been destroyed.
                        var destroyParentGo = EditorUtility.InstanceIDToObject(destroyGameObjectHierarchyEvent.parentInstanceId) as GameObject;
                        Debug.Log($"{type}: {destroyGameObjectHierarchyEvent.instanceId} with parent {destroyParentGo} in scene {destroyGameObjectHierarchyEvent.scene}.");
                        break;

                    case ObjectChangeKind.CreateAssetObject:
                        stream.GetCreateAssetObjectEvent(i, out var createAssetObjectEvent);
                        var createdAsset = EditorUtility.InstanceIDToObject(createAssetObjectEvent.instanceId);
                        var createdAssetPath = AssetDatabase.GUIDToAssetPath(createAssetObjectEvent.guid);
                        Debug.Log($"{type}: {createdAsset} at {createdAssetPath} in scene {createAssetObjectEvent.scene}.");
                        break;

                    case ObjectChangeKind.DestroyAssetObject:
                        stream.GetDestroyAssetObjectEvent(i, out var destroyAssetObjectEvent);
                        // The destroyed asset can not be converted with EditorUtility.InstanceIDToObject as it has already been destroyed.
                        Debug.Log($"{type}: Instance Id {destroyAssetObjectEvent.instanceId} with Guid {destroyAssetObjectEvent.guid} in scene {destroyAssetObjectEvent.scene}.");
                        break;

                    case ObjectChangeKind.ChangeAssetObjectProperties:
                        stream.GetChangeAssetObjectPropertiesEvent(i, out var changeAssetObjectPropertiesEvent);
                        var changeAsset = EditorUtility.InstanceIDToObject(changeAssetObjectPropertiesEvent.instanceId);
                        var changeAssetPath = AssetDatabase.GUIDToAssetPath(changeAssetObjectPropertiesEvent.guid);
                        Debug.Log($"{type}: {changeAsset} at {changeAssetPath} in scene {changeAssetObjectPropertiesEvent.scene}.");
                        break;

                    case ObjectChangeKind.UpdatePrefabInstances:
                        stream.GetUpdatePrefabInstancesEvent(i, out var updatePrefabInstancesEvent);
                        string s = "";
                        s += $"{type}: scene {updatePrefabInstancesEvent.scene}. Instances ({updatePrefabInstancesEvent.instanceIds.Length}):\n";
                        foreach (var prefabId in updatePrefabInstancesEvent.instanceIds)
                        {
                            s += EditorUtility.InstanceIDToObject(prefabId).ToString() + "\n";
                        }
                        Debug.Log(s);
                        break;
                }
            }
        }
    }
}
