using System;
using System.Collections.Generic;
using SaveLoadCore;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimpleUsageTest : MonoBehaviour
{
    [Savable] public List<BaseTest> test;

    private void Awake()
    {
        var addCount = Random.Range(1, 1);
        for (int a = 0; a < addCount; a++)
        {
            test.Add(new BaseTest() {value = Random.Range(0f, 1f)});
        }
    }

    [ContextMenu("PrintList")]
    public void PrintList()
    {
        string combinedString = String.Empty;
        for (var index = 0; index < test.Count; index++)
        {
            var element = test[index];
            combinedString += " | " + element;
        }
        Debug.Log($"list is: {combinedString}");
    }
}

[Serializable]
public class ListSerializable
{
    [Savable] public float numeric = 0f;
}
