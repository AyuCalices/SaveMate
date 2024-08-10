using SaveLoadSystem.Core.Attributes;
using SaveLoadSystem.Core.Component;
using UnityEngine;

namespace SaveLoadCore
{
    public class TransformReference : MonoBehaviour
    {
        [Savable] public Texture2D sprite;
        [Savable] public GameObject prefab;
        
        [Savable] public Transform TransformTest { get; set; }
        [Savable] public GameObject GameObjectTest { get; set; }

        private void Awake()
        {
            if (sprite != null)
            {
                foreach (TransformReference savableTest in GetComponents<TransformReference>())
                {
                    Debug.Log("o/");
                    //savableTest.sprite = sprite;
                }
            }
            
            if (prefab != null)
            {
                foreach (TransformReference savableTest in GetComponents<TransformReference>())
                {
                    Debug.Log("o/");
                    savableTest.prefab = prefab;
                }
            }
        }

        [ContextMenu("Add")]
        public void Add()
        {
            TransformTest.position += new Vector3(1, 1, 1);
        }

        [ContextMenu("Remove")]
        public void Remove()
        {
            TransformTest = null;
            GameObjectTest = null;
        }
    }
}
