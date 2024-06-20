using System;
using UnityEngine;

namespace SaveLoadCore
{
    public class SavableTest : MonoBehaviour
    {
        [Savable] public string position = Guid.NewGuid().ToString();
    }
}
