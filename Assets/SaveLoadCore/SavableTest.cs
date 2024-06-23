using System;
using UnityEngine;

namespace SaveLoadCore
{
    public enum TestEnum { One, Two, Three, Four, Five }
    
    public class SavableTest : MonoBehaviour
    {
        [Savable] public Transform Test { get; set; }

        private void Awake()
        {
            Transform newTest;
            if (Test == null)
            {
                newTest = transform;
            }
            else
            {
                return;
            }
            
            foreach (SavableTest savableTest in GetComponents<SavableTest>())
            {
                savableTest.Test = newTest;
            }
        }

        [ContextMenu("Add")]
        public void Add()
        {
            Test.position += new Vector3(1, 1, 1);
        }
    }

    [Serializable]
    public class Test
    {
        public float thing;
    }
}
