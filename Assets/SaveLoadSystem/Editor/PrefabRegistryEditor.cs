using System;
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
            GUI.enabled = false;
            
            SavableReferenceListPropertyLayout(_savablesProperty);
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void SavableReferenceListPropertyLayout(SerializedProperty serializedProperty)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Headers
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Savable Prefab", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Guid", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            for (var i = 0; i < serializedProperty.arraySize; i++)
            {
                var elementProperty = serializedProperty.GetArrayElementAtIndex(i);
                var componentProperty = elementProperty.FindPropertyRelative("unityObject");
                var pathProperty = elementProperty.FindPropertyRelative("guid");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(componentProperty, GUIContent.none);
                EditorGUILayout.PropertyField(pathProperty, GUIContent.none);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
