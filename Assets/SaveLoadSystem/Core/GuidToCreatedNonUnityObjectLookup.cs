using System;
using System.Collections.Generic;
using SaveLoadSystem.Core.DataTransferObject;

namespace SaveLoadSystem.Core
{
    public class GuidToCreatedNonUnityObjectLookup
    {
        private readonly Dictionary<GuidPath, WeakReference<object>> _guidToCreatedNonUnityObjectLookup = new();
        private readonly Dictionary<GuidPath, object> _hardResetLookup = new();

        public void PrepareLoading()
        {
            CleanupWeakReferences();
        }
        
        private void CleanupWeakReferences()
        {
            List<GuidPath> keysToRemove = new();
            foreach (var (guidPath, weakReference) in _guidToCreatedNonUnityObjectLookup)
            {
                if (!weakReference.TryGetTarget(out _))
                {
                    keysToRemove.Add(guidPath);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _guidToCreatedNonUnityObjectLookup.Remove(key);
            }
        }
        
        public void CompleteLoading()
        {
            UpsertCreatedNonUnityObjectLookup();
            
            _hardResetLookup.Clear();
        }

        private void UpsertCreatedNonUnityObjectLookup()
        {
            foreach (var (guidPath, obj) in _hardResetLookup)
            {
                _guidToCreatedNonUnityObjectLookup[guidPath] = new WeakReference<object>(obj);
            }
        }

        public bool TryGetValue(LoadType loadType, GuidPath guidPath, out object obj)
        {
            if (loadType == LoadType.Hard)
            {
                return _hardResetLookup.TryGetValue(guidPath, out obj);
            }

            if (_guidToCreatedNonUnityObjectLookup.TryGetValue(guidPath, out var weakObj))
            {
                return weakObj.TryGetTarget(out obj);
            }

            obj = default;
            return false;
        }

        public void Add(LoadType loadType, GuidPath guidPath, object obj)
        {
            if (loadType == LoadType.Hard)
            {
                _hardResetLookup.Add(guidPath, obj);
            }
            else
            {
                _guidToCreatedNonUnityObjectLookup.Add(guidPath, new WeakReference<object>(obj));
            }
        }

        public void Upsert(GuidPath guidPath, object obj)
        {
            _guidToCreatedNonUnityObjectLookup[guidPath] = new WeakReference<object>(obj);
        }

        public void CLear()
        {
            _guidToCreatedNonUnityObjectLookup.Clear();
            _hardResetLookup.Clear();
        }
    }
}
