using System;
using SaveLoadCore;
using SaveLoadCore.Core.Attributes;
using UnityEngine;

namespace Usage
{
    public class SavableComponent : MonoBehaviour
    {
        [Savable] public Test test;
    }

    //TODO: savable attribute doesnt work for monobehaviours because it will also serialize the fields and properties of the monobehaviour
    [SavableSchema, Serializable]
    public class Test
    {
        public int value;
        public string text = "";
    }
}
