using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveLoadSystem.Utility.PreventReset
{
    [Serializable]
    public class NonResetableList<T> : ISerializationCallbackReceiver
    {
        public List<T> values = new();
        private readonly List<T> _dumpList = new();

        public static implicit operator List<T>(NonResetableList<T> nonResetable) => nonResetable.values;
        public static implicit operator NonResetableList<T>(List<T> value) => new() { values = value };

        public void OnBeforeSerialize()
        {
            _dumpList.Clear();
            _dumpList.AddRange(values);
        }

        public void OnAfterDeserialize()
        {
            if (_dumpList == null || _dumpList.Count == 0) return;
            
            values.Clear();
            values.AddRange(_dumpList);
        }
    }
}
