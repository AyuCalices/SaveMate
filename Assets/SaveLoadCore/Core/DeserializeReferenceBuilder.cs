using System;
using System.Collections.Generic;
using SaveLoadCore.Core.Serializable;
using UnityEngine;

namespace SaveLoadCore.Core
{
    public class DeserializeReferenceBuilder
    {
        private readonly Queue<Action<Dictionary<GuidPath, object>>> _actionList = new();
        
        public void InvokeAll(Dictionary<GuidPath, object> createdObjectsLookup)
        {
            while (_actionList.Count != 0)
            {
                _actionList.Dequeue().Invoke(createdObjectsLookup);
            }
        }
        
        public void EnqueueReferenceBuilding(object obj, Action<object> onReferenceFound)
        {
            _actionList.Enqueue(createdObjectsLookup =>
            {
                if (obj is GuidPath guidPath)
                {
                    if (!createdObjectsLookup.TryGetValue(guidPath, out object value))
                    {
                        Debug.LogWarning("Wasn't able to find the created object!");
                        return;
                    }
                    
                    onReferenceFound.Invoke(value);
                }
                else
                {
                    onReferenceFound.Invoke(obj);
                }
            });
        }
        
        public void EnqueueReferenceBuilding(object[] objectGroup, Action<object[]> onReferenceFound)
        {
            _actionList.Enqueue(createdObjectsLookup =>
            {
                var convertedGroup = new object[objectGroup.Length];
                
                for (var index = 0; index < objectGroup.Length; index++)
                {
                    if (objectGroup[index] is GuidPath guidPath)
                    {
                        if (!createdObjectsLookup.TryGetValue(guidPath, out object value))
                        {
                            Debug.LogWarning("Wasn't able to find the created object!");
                            return;
                        }

                        convertedGroup[index] = value;
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
