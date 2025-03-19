using SaveLoadSystem.Core.UnityComponent;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Editor
{
    [CustomEditor(typeof(Savable))]
    public class SavableEditor : UnityEditor.Editor
    {
        private SerializedProperty _sceneGuidProperty;
        private SerializedProperty _prefabPathProperty;
        private SerializedProperty _customSpawningProperty;
        private SerializedProperty _currentSavableListProperty;
        private SerializedProperty _savableReferenceListProperty;
        
        private static bool _showCurrentSavableList;
        private static bool _showRemovedSavableList;
        private static bool _showSavableReferenceList;

        private void OnEnable()
        {
            _sceneGuidProperty = serializedObject.FindProperty("savableGuid");
            _prefabPathProperty = serializedObject.FindProperty("prefabGuid");
            _customSpawningProperty = serializedObject.FindProperty("dynamicPrefabSpawningDisabled");
            _currentSavableListProperty = serializedObject.FindProperty("savableLookup");
            _savableReferenceListProperty = serializedObject.FindProperty("duplicateComponentLookup");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            if (_prefabPathProperty.FindPropertyRelative("value").stringValue != string.Empty)
            {
                EditorGUILayout.PropertyField(_customSpawningProperty);
            }
            
            // Disable editing
            GUI.enabled = false;
            
            // Display the fields
            EditorGUILayout.PropertyField(_prefabPathProperty);
            EditorGUILayout.PropertyField(_sceneGuidProperty);
            ComponentContainerListLayout(_currentSavableListProperty.FindPropertyRelative("values"), "Tracked ISavables", ref _showCurrentSavableList);
            ComponentContainerListLayout(_savableReferenceListProperty.FindPropertyRelative("values"), "Duplicate Components (No ISavables)", ref _showSavableReferenceList);
            
            // Enable editing back
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }

        private void ComponentContainerListLayout(SerializedProperty serializedProperty, string layoutName, ref bool foldout)
        {
            foldout = EditorGUILayout.Foldout(foldout, layoutName);
            if (!foldout) return;
            
            EditorGUI.indentLevel++;
                
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
            // Headers
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Unity Object", EditorStyles.boldLabel);
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
                
            EditorGUI.indentLevel--;
        }
    }
}
