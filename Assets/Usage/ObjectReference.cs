using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveLoadCore
{
    public class ObjectReference : MonoBehaviour
    {
        [SavableMember] private ObjectTest Test { get; set; }

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
        [SavableMember] public float value;
    }

    [Serializable]
    public class ObjectTest : BaseTest
    {
        //TODO: array currently only for serializable things
        [SavableMember] public List<Reference> thing;
        [SavableMember] public string helloWorld = "hello world";
    }

    //TODO: error when serializable is removed
    [Serializable]
    public class Reference
    {
        [SavableMember] public string element = "hi";
    }
}
