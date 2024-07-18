using System.Collections.Generic;

namespace SaveLoadSystem.Core
{
    public class SavableElementLookup
    {
        private readonly Dictionary<object, SavableElement> _objectLookup = new();
        private readonly List<SavableElement> _saveElementList = new();

        public bool ContainsElement(object saveObject)
        {
            return _objectLookup.ContainsKey(saveObject);
        }
        
        public void InsertElement(int index, SavableElement savableElement)
        {
            _objectLookup.Add(savableElement.Obj, savableElement);
            _saveElementList.Insert(index, savableElement);
        }

        public bool TryGetValue(object saveObject, out SavableElement savableElement)
        {
            return _objectLookup.TryGetValue(saveObject, out savableElement);
        }

        public int Count()
        {
            return _saveElementList.Count;
        }

        public SavableElement GetAt(int index)
        {
            return _saveElementList[index];
        }
    }
}
