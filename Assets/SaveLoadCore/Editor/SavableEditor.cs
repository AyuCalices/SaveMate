using UnityEditor;
using UnityEngine;

namespace SaveLoadCore.Editor
{
    [CustomEditor(typeof(Savable))]
    public class SavableEditor : UnityEditor.Editor
    {
        private SerializedProperty _sceneGuidProperty;
        private SerializedProperty _hierarchyPathProperty;
        private SerializedProperty _prefabSourceProperty;
        private SerializedProperty _currentSavableListProperty;
        private SerializedProperty _removedSavableListProperty;
        
        private static bool _showCurrentSavableList;
        private static bool _showRemovedSavableList;

        private void OnEnable()
        {
            _sceneGuidProperty = serializedObject.FindProperty("serializeFieldSceneGuid");
            _hierarchyPathProperty = serializedObject.FindProperty("hierarchyPath");
            _prefabSourceProperty = serializedObject.FindProperty("prefabSource");
            _currentSavableListProperty = serializedObject.FindProperty("serializeFieldCurrentSavableList");
            _removedSavableListProperty = serializedObject.FindProperty("serializeFieldRemovedSavableList");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Disable editing
            GUI.enabled = false;
            
            // Display the fields
            EditorGUILayout.PropertyField(_sceneGuidProperty);
            EditorGUILayout.PropertyField(_hierarchyPathProperty);
            EditorGUILayout.PropertyField(_prefabSourceProperty);
            ComponentContainerListLayout(_currentSavableListProperty, "Current Savable List", ref _showCurrentSavableList);
            ComponentContainerListLayout(_removedSavableListProperty, "Removed Savable List", ref _showRemovedSavableList);

            // Enable editing back
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }

        private void ComponentContainerListLayout(SerializedProperty serializedProperty, string layoutName, ref bool foldout)
        {
            foldout = EditorGUILayout.Foldout(foldout, layoutName);
            if (foldout)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Headers
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Component", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Identifier", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                
                for (int i = 0; i < serializedProperty.arraySize; i++)
                {
                    SerializedProperty elementProperty = serializedProperty.GetArrayElementAtIndex(i);
                    SerializedProperty componentProperty = elementProperty.FindPropertyRelative("component");
                    SerializedProperty pathProperty = elementProperty.FindPropertyRelative("identifier");

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
}
