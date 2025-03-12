using SaveLoadSystem.Core;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Editor
{
    [CustomEditor(typeof(AssetRegistry))]
    public class AssetRegistryEditor : UnityEditor.Editor
    {
        private SerializedProperty _searchInFolderProperty;
        private SerializedProperty _prefabSavablesProperty;
        private SerializedProperty _scriptableObjectSavablesProperty;
        
        private static bool _showPrefabSavablesList;
        private static bool _showScriptableObjectSavablesList;
        
        private void OnEnable()
        {
            _searchInFolderProperty = serializedObject.FindProperty("searchInFolders");
            _prefabSavablesProperty = serializedObject.FindProperty("prefabSavables");
            _scriptableObjectSavablesProperty = serializedObject.FindProperty("scriptableObjectSavables");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(_searchInFolderProperty);
            
            if (GUILayout.Button("Update Folder Filter"))
            {
                AssetRegistry assetRegistry = (AssetRegistry)target;
                assetRegistry.UpdateFolderSelect();
            }
            
            GUI.enabled = false;
            GUILayout.Space(20f);
            PrefabLayout(_prefabSavablesProperty, "Prefab Savables", ref _showPrefabSavablesList);
            ScriptableObjectLayout(_scriptableObjectSavablesProperty, "Scriptable Object Savables", ref _showScriptableObjectSavablesList);
            
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void PrefabLayout(SerializedProperty serializedProperty, string layoutName,
            ref bool foldout)
        {
            //draw label
            GUI.enabled = true;
            EditorGUILayout.BeginHorizontal();
            foldout = EditorGUILayout.Foldout(foldout, layoutName, new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            });
            
            GUI.enabled = false;
            EditorGUILayout.TextField(serializedProperty.arraySize.ToString(), GUILayout.MaxWidth(48));
            EditorGUILayout.EndHorizontal();
            if (!foldout) return;
            
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            for (var i = 0; i < serializedProperty.arraySize; i++)
            {
                var elementProperty = serializedProperty.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(elementProperty, new GUIContent("Element " + i));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            
            EditorGUI.indentLevel--;
        }
        
        private void ScriptableObjectLayout(SerializedProperty serializedProperty, string layoutName,
            ref bool foldout)
        {
            //draw label
            GUI.enabled = true;
            EditorGUILayout.BeginHorizontal();
            foldout = EditorGUILayout.Foldout(foldout, layoutName, new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            });
            
            GUI.enabled = false;
            EditorGUILayout.TextField(serializedProperty.arraySize.ToString(), GUILayout.MaxWidth(48));
            EditorGUILayout.EndHorizontal();
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
