using SaveMate.Runtime.Utility;
using UnityEditor;
using UnityEngine;

namespace SaveMate.Runtime.Core.ObjectChangeEvents
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
            
            if (gameObjectStructure == null || gameObjectStructure.IsDestroyed()) return;
            
            if (gameObjectStructure.TryGetComponent(out IChangeGameObjectStructure changeGameObjectStructureEvent))
            {
                changeGameObjectStructureEvent.OnChangeGameObjectStructure();
            }
        }
    }
    
#endif
}
