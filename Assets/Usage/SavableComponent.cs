using System;
using SaveLoadSystem.Core.Attributes;
using UnityEngine;

namespace Usage
{
    [SavableObject]
    public class SavableComponent : MonoBehaviour
    {
        public PlayerDataV1 migrationTestV1;
    }

    [Serializable]
    public class PlayerDataV1
    {
        public string playerName;
        public int score;
    }
}
