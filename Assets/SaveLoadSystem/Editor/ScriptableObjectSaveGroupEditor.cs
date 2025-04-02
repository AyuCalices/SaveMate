using SaveLoadSystem.Core;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Editor
{
    [CustomEditor(typeof(ScriptableObjectSaveGroup))]
    public class ScriptableObjectSaveGroupEditor : UnityEditor.Editor
    {
        private SerializedProperty _searchInFolderProperty;
        private SerializedProperty _pathBasedScriptableObjectsProperty;
        private SerializedProperty _customAddedScriptableObjectsProperty;

        private void OnEnable()
        {
            _searchInFolderProperty = serializedObject.FindProperty("searchInFolders");
            _pathBasedScriptableObjectsProperty = serializedObject.FindProperty("pathBasedScriptableObjects");
            _customAddedScriptableObjectsProperty = serializedObject.FindProperty("customAddedScriptableObjects");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(_searchInFolderProperty);
            
            GUI.enabled = false;
            EditorGUILayout.PropertyField(_pathBasedScriptableObjectsProperty);
            GUI.enabled = true;
            
            EditorGUILayout.PropertyField(_customAddedScriptableObjectsProperty);
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
