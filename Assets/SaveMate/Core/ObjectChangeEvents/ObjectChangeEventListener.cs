using SaveMate.Utility;
using UnityEditor;
using UnityEngine;

namespace SaveMate.Core.ObjectChangeEvents
{
    
#if UNITY_EDITOR
    
    [InitializeOnLoad]
    internal class ObjectChangeEventListener
    {
        static ObjectChangeEventListener()    
        {
            UnityEditor.ObjectChangeEvents.changesPublished += ChangesPublished;
        }

        private static void ChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                var type = stream.GetEventType(i);
                if (type == ObjectChangeKind.ChangeGameObjectStructure) ChangeGameObjectStructure(i, type, ref stream, false);
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
    }
    
#endif
}
