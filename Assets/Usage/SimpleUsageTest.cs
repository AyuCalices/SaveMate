using System;
using SaveLoadCore;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimpleUsageTest : MonoBehaviour
{
    [SavableMember] public ListSerializable[] test;

    private void Awake()
    {
        var addCount = Random.Range(3, 6);
        test = new ListSerializable[addCount];
        
        for (int index = 0; index < addCount; index++)
        {
            test[index] = new ListSerializable() { numeric = Random.Range(0f, 1f)};
        }
    }

    [ContextMenu("PrintList")]
    public void PrintList()
    {
        string combinedString = String.Empty;
        foreach (var value in test)
        {
            combinedString += " | " + value.numeric;
        }
        
        Debug.Log($"list is: {combinedString}");
    }
}

[Serializable]
public class ListSerializable
{
    [SavableMember] public float numeric;
}
