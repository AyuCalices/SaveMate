using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace SaveLoadCore
{
    public static class SaveLoadManager
    {
        private static HashSet<(string, object)> _savables = new HashSet<(string, object)>();
        
        public static void Save(DataContainer dataContainer, string saveName = "/player.data", string savePath = "")
        {
            BinaryFormatter formatter = new BinaryFormatter();
            string path = Application.persistentDataPath + savePath + saveName;
            FileStream stream = new FileStream(path, FileMode.Create);

            formatter.Serialize(stream, dataContainer);
            stream.Close();
        }
    
        public static DataContainer Load(string saveName = "/player.data", string savePath = "")
        {
            string path = Application.persistentDataPath + savePath + saveName;
            if (File.Exists(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(path, FileMode.Open);

                DataContainer data = formatter.Deserialize(stream) as DataContainer;
                stream.Close();
                return data;
            }
            else
            {
                Debug.LogError("Save file not found in " + path);
                return null;
            }
        }

        public static bool SaveExists(string saveName = "/player.data", string savePath = "")
        {
            return File.Exists(Application.persistentDataPath + savePath + saveName);
        }

        public static void RegisterSavable(string identifier, object obj)
        {
            if (_savables.Add((nameof(obj), obj))) return;
            
            Debug.LogError($"Can't add {obj} twice!");
        }

        public static void UnregisterSavable(string identifier, object obj)
        {
            if (_savables.Remove((identifier, obj))) return;
            
            Debug.LogError($"Can't remove {obj}, because it is not registered!");
        }
        
        public static void RegisterSavable(string identifier, int obj)
        {
            IntegerConduit integerConduit = new IntegerConduit(() => obj, i => i = obj);
            
            
            if (_savables.Add((nameof(obj), obj))) return;
            
            Debug.LogError($"Can't add {obj} twice!");
        }

        public class IntegerConduit
        {
            public readonly System.Action<int> Set;
            public readonly System.Func<int> Get;

            public IntegerConduit(System.Func<int> get, System.Action<int> set)
            {
                Get = get;
                Set = set;
            }
        }
    }
}
