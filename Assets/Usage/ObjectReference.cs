using System;
using UnityEngine;

namespace SaveLoadCore
{
    public class ObjectReference : MonoBehaviour
    {
        [Savable] private ObjectTest Test { get; set; }

        private void Awake()
        {
            ObjectTest newTest;
            if (Test == null)
            {
                newTest = new ObjectTest();
            }
            else
            {
                return;
            }
            
            foreach (ObjectReference savableTest in GetComponents<ObjectReference>())
            {
                savableTest.Test = newTest;
            }
        }

        [ContextMenu("Add")]
        public void Add()
        {
            if (Test != null)
            {
                Test.thing++;
            }
        }

        [ContextMenu("Remove")]
        public void Remove()
        {
            Test = null;
        }
    }

    [Serializable]
    public class ObjectTest
    {
        public float thing;
    }
}
