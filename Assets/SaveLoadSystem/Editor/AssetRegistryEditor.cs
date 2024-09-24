using System;
using SaveLoadSystem.Core;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Editor
{
    [CustomEditor(typeof(AssetRegistry))]
    public class AssetRegistryEditor : UnityEditor.Editor
    {
        private SerializedProperty _prefabLookupProperty;
        private SerializedProperty _unityObjectListProperty;
        
        private static bool _showUnityObjectList;
        
        private void OnEnable()
        {
            _prefabLookupProperty = serializedObject.FindProperty("prefabLookup");
            _unityObjectListProperty = serializedObject.FindProperty("unityObjectList");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(_prefabLookupProperty);
            
            // Disable editing
            GUI.enabled = false;
            
            // Display the fields
            SavableReferenceListPropertyLayout(_unityObjectListProperty, "Unity Objects", ref _showUnityObjectList, true);
            
            // Enable editing back
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }
        
        private void SavableReferenceListPropertyLayout(SerializedProperty serializedProperty, string layoutName,
            ref bool foldout, bool componentEditable = false, bool guidEditable = false)
        {
            GUI.enabled = true;
            
            foldout = EditorGUILayout.Foldout(foldout, layoutName);
            if (!foldout) return;
            
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Headers
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Unity Object", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Guid", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            GUI.enabled = false;

            for (var i = 0; i < serializedProperty.arraySize; i++)
            {
                var elementProperty = serializedProperty.GetArrayElementAtIndex(i);
                var componentProperty = elementProperty.FindPropertyRelative("unityObject");
                var pathProperty = elementProperty.FindPropertyRelative("guid");

                EditorGUILayout.BeginHorizontal();
                EditableGUILayoutAction(componentEditable, () => EditorGUILayout.PropertyField(componentProperty, GUIContent.none));
                EditableGUILayoutAction(guidEditable, () => EditorGUILayout.PropertyField(pathProperty, GUIContent.none));

                GUI.enabled = true;
                // Add a button to remove the element
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    serializedProperty.DeleteArrayElementAtIndex(i);
                }
                GUI.enabled = false;

                EditorGUILayout.EndHorizontal();
            }
            
            GUI.enabled = true;

            // Add button to add new element
            if (GUILayout.Button("Add Object"))
            {
                var newIndex = serializedProperty.arraySize;
                serializedProperty.InsertArrayElementAtIndex(newIndex);

                var newElementProperty = serializedProperty.GetArrayElementAtIndex(newIndex);
                var newComponentProperty = newElementProperty.FindPropertyRelative("component");
                var newPathProperty = newElementProperty.FindPropertyRelative("guid");

                // Initialize new element properties if necessary
                if (newComponentProperty != null) newComponentProperty.objectReferenceValue = null;
                if (newPathProperty != null) newPathProperty.stringValue = Guid.NewGuid().ToString();
            }
            
            GUI.enabled = false;

            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--;
        }
        
        private void EditableGUILayoutAction(bool isEditable, Action action)
        {
            var currentlyEditable = GUI.enabled;
            GUI.enabled = isEditable;
            action.Invoke();
            GUI.enabled = currentlyEditable;
        }
    }
}
