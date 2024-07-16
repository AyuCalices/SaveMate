using System;
using System.Collections.Generic;
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
                newTest.thing = new List<Reference>();
                var reference = new Reference();
                newTest.thing.Add(reference);
                newTest.thing.Add(reference);
                newTest.thing.Add(reference);
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
            Test.thing = null;
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
        [Savable] public List<Reference> thing;
        [Savable] public string helloWorld = "hello world";
    }

    //TODO: error when serializable is removed
    [Serializable]
    public class Reference
    {
        [Savable] public string element = "hi";
    }
}
