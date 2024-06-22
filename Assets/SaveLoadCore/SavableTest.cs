using UnityEngine;

namespace SaveLoadCore
{
    public enum TestEnum { One, Two, Three, Four, Five }
    
    public class SavableTest : MonoBehaviour
    {
        [Savable] public Vector2 position;
        
    }
}
