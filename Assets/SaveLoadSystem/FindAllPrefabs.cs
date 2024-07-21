using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem
{
    public class FindAllPrefabs : EditorWindow
    {
        [MenuItem("Window/Find All Prefabs")]
        public static void ShowWindow()
        {
            GetWindow(typeof(FindAllPrefabs), false, "Find All Prefabs");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Find and Instantiate Prefabs"))
            {
                string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
                string[] prefabPaths = new string[prefabGUIDs.Length];

                for (int i = 0; i < prefabGUIDs.Length; i++)
                {
                    prefabPaths[i] = AssetDatabase.GUIDToAssetPath(prefabGUIDs[i]);
                }

                foreach (string path in prefabPaths)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                        if (instance != null)
                        {
                            Debug.Log("Instantiated prefab: " + path);
                        }
                        else
                        {
                            Debug.LogWarning("Could not instantiate prefab at path: " + path);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Could not load prefab at path: " + path);
                    }
                }

                EditorUtility.DisplayDialog("Prefabs Instantiated", "Instantiated " + prefabPaths.Length + " prefabs.", "OK");
            }
        }
    }
}
