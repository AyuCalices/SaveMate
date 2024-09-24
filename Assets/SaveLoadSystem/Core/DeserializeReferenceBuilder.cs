using System;
using System.Collections.Generic;
using SaveLoadSystem.Core.DataTransferObject;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    public class DeserializeReferenceBuilder
    {
        private readonly Queue<Action<Dictionary<GuidPath, object>, Dictionary<string, object>>> _actionList = new();
        
        public void InvokeAll(Dictionary<GuidPath, object> createdObjectsLookup, Dictionary<string, object> guidPathReferenceLookup)
        {
            while (_actionList.Count != 0)
            {
                _actionList.Dequeue().Invoke(createdObjectsLookup, guidPathReferenceLookup);
            }
        }
        
        public void EnqueueReferenceBuilding(GuidPath path, Action<object> onReferenceFound)
        {
            _actionList.Enqueue((createdObjectsLookup, guidPathReferenceLookup) =>
            {
                if (createdObjectsLookup.TryGetValue(path, out object value))
                {
                    onReferenceFound.Invoke(value);
                }
                else if (guidPathReferenceLookup.TryGetValue(path.ToString(), out value))
                {
                    onReferenceFound.Invoke(value);
                }
                else
                {
                    Debug.LogWarning("Wasn't able to find the created object!");
                }
            });
        }
        
        public void EnqueueReferenceBuilding(GuidPath[] pathGroup, Action<object[]> onReferenceFound)
        {
            _actionList.Enqueue((createdObjectsLookup, guidPathReferenceLookup) =>
            {
                var convertedGroup = new object[pathGroup.Length];
                
                for (var index = 0; index < pathGroup.Length; index++)
                {
                     if (createdObjectsLookup.TryGetValue(pathGroup[index], out object value)) 
                     {
                        convertedGroup[index] = value; 
                     }
                     else if (guidPathReferenceLookup.TryGetValue(pathGroup[index].ToString(), out value)) 
                     { 
                         convertedGroup[index] = value; 
                     }
                     else 
                     { 
                         Debug.LogWarning("Wasn't able to find the created object!"); 
                     }
                }

                onReferenceFound.Invoke(convertedGroup);
            });
        }
    }
}
