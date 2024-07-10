using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveLoadCore
{
    public class ObjectReference : MonoBehaviour
    {
        [Savable] private ObjectTest Test { get; set; }
        [Savable] public Vector3 list;

        private void Awake()
        {
            ObjectTest newTest;
            if (Test == null)
            {
                newTest = new ObjectTest();
                newTest.thing = new Reference[3];
                newTest.thing[0] = new Reference();
                newTest.thing[1] = new Reference();
                newTest.thing[2] = new Reference();
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

    [Serializable]
    public class BaseTest
    {
        [Savable] public float value;
    }

    [Serializable]
    public class ObjectTest : BaseTest
    {
        //TODO: array currently only for serializable things
        [Savable] public Reference[] thing;
        [Savable] public string helloWorld = "hello world";
    }

    //TODO: error when serializable is removed
    [Serializable]
    public class Reference
    {
        public string element = "hi";
    }
}
