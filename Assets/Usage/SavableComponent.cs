using SaveLoadCore;
using UnityEngine;

namespace Usage
{
    [Savable]
    public class SavableComponent : MonoBehaviour
    {
        public int value;
        public string text = "";
    }
}
