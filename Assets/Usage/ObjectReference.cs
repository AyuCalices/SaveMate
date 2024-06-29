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
                newTest.thing = transform;
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
                Test.value++;
            }
        }

        [ContextMenu("Remove")]
        public void Remove()
        {
            Test = null;
        }
    }

    public class BaseTest
    {
        [Savable] public float value;
    }

    [Serializable]
    public class ObjectTest : BaseTest
    {
        [Savable] public Transform thing;
        [Savable] public string helloWorld = "hello world";
    }
}
