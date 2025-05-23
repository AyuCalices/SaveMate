using SaveMate.Runtime.Core.SaveComponents.GameObjectScope;
using UnityEditor;
using UnityEngine;

namespace SaveMate.Editor
{
    [CustomEditor(typeof(Savable))]
    public class SavableEditor : UnityEditor.Editor
    {
        private SerializedProperty _sceneGuidProperty;
        private SerializedProperty _prefabPathProperty;
        private SerializedProperty _disablePrefabSpawningProperty;
        private SerializedProperty _saveStateHandlersProperty;
        private SerializedProperty _savableReferenceListProperty;
        
        private static bool _showCurrentSavableList;
        private static bool _showRemovedSavableList;
        private static bool _showSavableReferenceList;
        private static bool _isToggled;

        private void OnEnable()
        {
            _sceneGuidProperty = serializedObject.FindProperty("sceneGuid");
            _prefabPathProperty = serializedObject.FindProperty("prefabGuid");
            _disablePrefabSpawningProperty = serializedObject.FindProperty("disablePrefabSpawning");
            _saveStateHandlersProperty = serializedObject.FindProperty("saveStateHandlers");
            _savableReferenceListProperty = serializedObject.FindProperty("duplicateComponentLookup");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Toggle Button
            if (GUILayout.Button(_isToggled ? "Disable GUID Editing" : "Enable GUID Editing"))
            {
                _isToggled = !_isToggled;
            }
            
            // Show message when toggled
            if (_isToggled)
            {
                EditorGUILayout.HelpBox("Warning: Changing the GUID will break references in all existing save files that used the previous GUID!", MessageType.Warning);
            }
            
            EditorGUILayout.Space(20f);
            
            if (_prefabPathProperty.FindPropertyRelative("value").stringValue != string.Empty)
            {
                EditorGUILayout.PropertyField(_disablePrefabSpawningProperty);
            }
            
            // Disable editing
            GUI.enabled = _isToggled;
            EditorGUILayout.PropertyField(_prefabPathProperty);
            EditorGUILayout.PropertyField(_sceneGuidProperty);
            
            ComponentContainerListLayout(_saveStateHandlersProperty.FindPropertyRelative("values"), "Tracked ISaveStateHandlers", ref _showCurrentSavableList);
            ComponentContainerListLayout(_savableReferenceListProperty.FindPropertyRelative("values"), "Duplicate Components (No ISaveStateHandlers)", ref _showSavableReferenceList);

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
                GUI.enabled = false;
                EditorGUILayout.PropertyField(componentProperty, GUIContent.none);
                GUI.enabled = _isToggled;
                EditorGUILayout.PropertyField(pathProperty, GUIContent.none);
                GUI.enabled = false;
                EditorGUILayout.EndHorizontal();
            }
                
            EditorGUILayout.EndVertical();
                
            EditorGUI.indentLevel--;
        }
    }
}
