using System.Collections.Generic;
using JetBrains.Annotations;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class RootSaveData
    {
        public const string GlobalSaveDataName = "GLOBAL";
        
        [UsedImplicitly] public BranchSaveData GlobalSaveData { get; set; } = new();
        [UsedImplicitly] public Dictionary<string, SceneData> SceneDataLookup { get; set; } = new();

        public void SetSceneData(string sceneName, SceneData sceneData)
        {
            SceneDataLookup[sceneName] = sceneData;
        }

        public bool TryGetSceneData(string sceneName, out SceneData sceneData)
        {
            return SceneDataLookup.TryGetValue(sceneName, out sceneData);
        }

        public void RemoveSceneData(string sceneName)
        {
            SceneDataLookup.Remove(sceneName);
        }
    }
}
