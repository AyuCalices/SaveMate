using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class RootSaveData
    {
        public const string GlobalSaveDataName = "GLOBAL";
        
        [UsedImplicitly] public List<string> ActiveScenes { get; set; } = new();
        [UsedImplicitly] public BranchSaveData GlobalSaveData { get; set; } = new();
        [UsedImplicitly] public Dictionary<string, SceneData> SceneDataLookup { get; set; } = new();

        public void SetGlobalSceneData(BranchSaveData branchSaveData)
        {
            GlobalSaveData = branchSaveData;
        }
        
        public void SetActiveScenes(List<string> activeScenes)
        {
            ActiveScenes = activeScenes;
        }

        public void SetSceneData(Scene scene, SceneData sceneData)
        {
            SceneDataLookup[scene.name] = new SceneData
            {
                ActivePrefabs = sceneData.ActivePrefabs, 
                ActiveSaveData = sceneData.ActiveSaveData
            };
        }

        public bool TryGetSceneData(Scene scene, out SceneData sceneData)
        {
            return SceneDataLookup.TryGetValue(scene.name, out sceneData);
        }

        public void RemoveSceneData(Scene scene)
        {
            SceneDataLookup.Remove(scene.name);
        }
    }
}
