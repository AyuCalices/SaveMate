using System;
using System.Collections.Generic;
using SaveLoadSystem.Core.SerializableTypes;
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
        
        public void EnqueueReferenceBuilding(object obj, Action<object> onReferenceFound)
        {
            _actionList.Enqueue((createdObjectsLookup, guidPathReferenceLookup) =>
            {
                if (obj is GuidPath guidPath)
                {
                    if (createdObjectsLookup.TryGetValue(guidPath, out object value))
                    {
                        onReferenceFound.Invoke(value);
                    }
                    else if (guidPathReferenceLookup.TryGetValue(guidPath.ToString(), out value))
                    {
                        onReferenceFound.Invoke(value);
                    }
                    else
                    {
                        Debug.LogWarning("Wasn't able to find the created object!");
                    }
                }
                else
                {
                    onReferenceFound.Invoke(obj);
                }
            });
        }
        
        public void EnqueueReferenceBuilding(object[] objectGroup, Action<object[]> onReferenceFound)
        {
            _actionList.Enqueue((createdObjectsLookup, guidPathReferenceLookup) =>
            {
                var convertedGroup = new object[objectGroup.Length];
                
                for (var index = 0; index < objectGroup.Length; index++)
                {
                    if (objectGroup[index] is GuidPath guidPath)
                    {
                        if (createdObjectsLookup.TryGetValue(guidPath, out object value))
                        {
                            convertedGroup[index] = value;
                        }
                        else if (guidPathReferenceLookup.TryGetValue(guidPath.ToString(), out value))
                        {
                            convertedGroup[index] = value;
                        }
                        else
                        {
                            Debug.LogWarning("Wasn't able to find the created object!");
                        }
                    }
                    else
                    {
                        convertedGroup[index] = objectGroup[index];
                    }
                }

                onReferenceFound.Invoke(convertedGroup);
            });
        }
    }
}
