using System;
using UnityEngine;

namespace SaveLoadCore
{
    public class SavableTest : MonoBehaviour
    {
        [Savable] public Vector3 position;

        /*
         * This reference may change
         * When is it important,
         */
        [Savable] public Transform reference;
    }
}
