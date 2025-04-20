using System.Collections.Generic;

namespace SaveMate.Runtime.Core.DataTransferObject
{
    public class RootSaveData
    {
        public BranchSaveData ScriptableObjectSaveData { get; set; } = new();
        public Dictionary<string, SceneData> SceneDataLookup { get; set; } = new();

        public void UpsertSceneData(string sceneName, SceneData sceneData)
        {
            SceneDataLookup[sceneName] = sceneData;
        }
        
        public bool TryGetSceneData(string sceneName, out SceneData sceneData)
        {
            return SceneDataLookup.TryGetValue(sceneName, out sceneData);
        }

        public void Clear()
        {
            ScriptableObjectSaveData.Clear();
            SceneDataLookup.Clear();
        }
    }
}
