using System;
using UnityEngine;

namespace SaveLoadCore
{
    public class TransformReference : MonoBehaviour
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
            
            foreach (TransformReference savableTest in GetComponents<TransformReference>())
            {
                savableTest.Test = newTest;
            }
        }

        [ContextMenu("Add")]
        public void Add()
        {
            Test.position += new Vector3(1, 1, 1);
        }

        [ContextMenu("Remove")]
        public void Remove()
        {
            Test = null;
        }
    }

    [Serializable]
    public class Test
    {
        public float thing;
    }
}
