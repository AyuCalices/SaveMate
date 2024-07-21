using SaveLoadSystem.Core.Component;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Editor
{
    [CustomEditor(typeof(PrefabRegistry))]
    public class PrefabRegistryEditor : UnityEditor.Editor
    {
        private SerializedProperty _savablesProperty;
        
        private void OnEnable()
        {
            _savablesProperty = serializedObject.FindProperty("savables");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Savable Prefabs", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
            for (var i = 0; i < _savablesProperty.arraySize; i++)
            {
                var elementProperty = _savablesProperty.GetArrayElementAtIndex(i);
                var savablePrefabProperty = elementProperty.FindPropertyRelative("savablePrefab");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(savablePrefabProperty, GUIContent.none);
                EditorGUILayout.EndHorizontal();
            }
                
            EditorGUILayout.EndVertical();
                
            EditorGUI.indentLevel--;
        }
    }
}
