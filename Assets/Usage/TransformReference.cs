using SaveLoadSystem.Core.Attributes;
using UnityEngine;

namespace SaveLoadCore
{
    public class TransformReference : MonoBehaviour
    {
        public Texture2D sprite;
        
        [Savable] public Transform TransformTest { get; set; }
        [Savable] public GameObject GameObjectTest { get; set; }
        [Savable] public Texture2D SpriteTest { get; set; }

        private void Awake()
        {
            Transform newTransformTest;
            if (TransformTest == null)
            {
                newTransformTest = transform;
            }
            else
            {
                return;
            }
            
            foreach (TransformReference savableTest in GetComponents<TransformReference>())
            {
                savableTest.TransformTest = newTransformTest;
            }
            
            
            GameObject newGameObjectTest;
            if (GameObjectTest == null)
            {
                newGameObjectTest = gameObject;
            }
            else
            {
                return;
            }
            
            foreach (TransformReference savableTest in GetComponents<TransformReference>())
            {
                savableTest.GameObjectTest = newGameObjectTest;
            }
            
            
            Texture2D newSpriteTest;
            if (SpriteTest == null)
            {
                newSpriteTest = sprite;
            }
            else
            {
                return;
            }
            
            foreach (TransformReference savableTest in GetComponents<TransformReference>())
            {
                savableTest.SpriteTest = newSpriteTest;
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
