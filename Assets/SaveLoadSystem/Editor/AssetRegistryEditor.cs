using SaveLoadSystem.Core;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Editor
{
    [CustomEditor(typeof(AssetRegistry))]
    public class AssetRegistryEditor : UnityEditor.Editor
    {
        private SerializedProperty _prefabSavablesProperty;
        private SerializedProperty _scriptableObjectSavablesProperty;
        
        private static bool _showPrefabSavablesList;
        private static bool _showScriptableObjectSavablesList;
        
        private void OnEnable()
        {
            _prefabSavablesProperty = serializedObject.FindProperty("prefabSavables");
            _scriptableObjectSavablesProperty = serializedObject.FindProperty("scriptableObjectSavables");
        }
        
        public override void OnInspectorGUI()
        {
            GUI.enabled = false;

            SavableReferenceListPropertyLayout(_prefabSavablesProperty, "Prefab Savables", ref _showPrefabSavablesList);
            SavableReferenceListPropertyLayout(_scriptableObjectSavablesProperty, "Scriptable Object Savables", ref _showScriptableObjectSavablesList);
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void SavableReferenceListPropertyLayout(SerializedProperty serializedProperty, string layoutName,
            ref bool foldout)
        {
            foldout = EditorGUILayout.Foldout(foldout, layoutName);
            if (!foldout) return;
            
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Headers
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Savable", EditorStyles.boldLabel);
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
