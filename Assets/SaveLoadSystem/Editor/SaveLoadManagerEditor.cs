using System.IO;
using SaveLoadSystem.Core;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Editor
{
    [CustomEditor(typeof(SaveLoadManager))]
    public class SaveLoadManagerEditor : UnityEditor.Editor
    {
    
        public override void OnInspectorGUI()
        {
            var saveLoadManager = (SaveLoadManager)target;
        
            DrawDefaultInspector();

            GUILayout.Space(30);
            if (GUILayout.Button("Open persistent path"))
            {
                var path = Path.Combine(Application.persistentDataPath, saveLoadManager.SavePath) + Path.AltDirectorySeparatorChar;

                if (Directory.Exists(path))
                {
                    EditorUtility.RevealInFinder(path);
                }
                else
                {
                    Debug.LogWarning("The specified path does not exist: " + path);
                }
            }
        
            if (GUILayout.Button("Delete save data at persistent data path"))
            {
                var path = Path.Combine(Application.persistentDataPath, saveLoadManager.SavePath) + Path.AltDirectorySeparatorChar;

                if (Directory.Exists(path))
                {
                    DeleteFilesAtPath(path, saveLoadManager.ExtensionName);
                    DeleteFilesAtPath(path, saveLoadManager.MetaDataExtensionName);
                }
                else
                {
                    Debug.LogWarning("The specified path does not exist: " + path);
                }
            }
        }
    
        private void DeleteFilesAtPath(string path, string fileExtension)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(fileExtension))
            {
                Debug.LogWarning("Path or file extension is empty.");
                return;
            }

            if (!Directory.Exists(path))
            {
                Debug.LogWarning($"Path does not exist: {path}");
                return;
            }

            string[] files = Directory.GetFiles(path, $"*{fileExtension}");

            foreach (string file in files)
            {
                try
                {
                    File.Delete(file);
                    Debug.Log($"Deleted file: {file}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to delete file: {file}. Error: {ex.Message}");
                }
            }

            AssetDatabase.Refresh();
        }
    }
}
