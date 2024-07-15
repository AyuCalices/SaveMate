using System;
using System.Collections.Generic;
using SaveLoadCore;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimpleUsageTest : MonoBehaviour
{
    [Savable] public List<List<Vector3>> test;

    private void Awake()
    {
        test = new List<List<Vector3>>();
        
        var listCount = Random.Range(0, 3);
        for (int l = 0; l < listCount; l++)
        {
            var list = new List<Vector3>();
            var addCount = Random.Range(0, 3);
            for (int a = 0; a < addCount; a++)
            {
                list.Add(new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
            }
            test.Add(list);
        }
    }

    [ContextMenu("PrintList")]
    public void PrintList()
    {
        for (var index = 0; index < test.Count; index++)
        {
            var vector3List = test[index];
            string combinedString = String.Empty;
            foreach (var vector3 in vector3List)
            {
                combinedString += " | " + vector3;
            }

            Debug.Log($"list at {index} is: {combinedString}");
        }
    }
}
