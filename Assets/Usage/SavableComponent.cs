using System;
using SaveLoadCore;
using UnityEngine;

namespace Usage
{
    public class SavableComponent : MonoBehaviour
    {
        [SavableMember] public Test test;
    }

    //TODO: savable attribute doesnt work for monobehaviours because it will also serialize the fields and properties of the monobehaviour
    [Savable, Serializable]
    public class Test
    {
        public int value;
        public string text = "";
    }
}
