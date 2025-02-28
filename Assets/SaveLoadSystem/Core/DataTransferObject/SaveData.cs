using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class SaveData
    {
        [UsedImplicitly] public SaveDataContainer GlobalDataContainerLookup { get; set; }
        [UsedImplicitly] public Dictionary<string, SceneData> SceneDataLookup { get; set; }

        public void SetGlobalSceneData(SaveDataContainer saveDataContainer)
        {
            GlobalDataContainerLookup = saveDataContainer;
        }

        public void SetSceneData(Scene scene, SceneData sceneData)
        {
            SceneDataLookup ??= new Dictionary<string, SceneData>();
            
            SceneDataLookup[scene.name] = new SceneData
            {
                PrefabGuidGroup = sceneData.PrefabGuidGroup, 
                SaveDataContainer = sceneData.SaveDataContainer
            };
        }

        public bool TryGetSceneData(Scene scene, out SceneData sceneData)
        {
            SceneDataLookup ??= new Dictionary<string, SceneData>();
            
            return SceneDataLookup.TryGetValue(scene.name, out sceneData);
        }

        public void RemoveSceneData(Scene scene)
        {
            SceneDataLookup ??= new Dictionary<string, SceneData>();
            
            SceneDataLookup.Remove(scene.name);
        }
    }
}
