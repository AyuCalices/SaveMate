using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SaveLoadSystem.Core.Component;
using SaveLoadSystem.Core.DataTransferObject;

namespace SaveLoadSystem.Core
{
    /// <summary>
    /// The <see cref="SaveDataHandler"/> class is responsible for managing the serialization and storage of data
    /// within the save/load system, specifically handling the addition and reference management of savable objects.
    /// </summary>
    public class SaveDataHandler
    {
        private readonly SaveDataBuffer _objectSaveDataBuffer;
        private readonly GuidPath _originGuidPath;
        private readonly Dictionary<GuidPath, SaveDataBuffer> _saveDataBufferLookup;
        private readonly Dictionary<object, GuidPath> _processedSavablesLookup;
        private readonly Dictionary<object, GuidPath> _objectReferenceLookup;

        public SaveDataHandler(SaveDataBuffer objectSaveDataBuffer, GuidPath originGuidPath, Dictionary<GuidPath, SaveDataBuffer> saveDataBufferLookup, 
            Dictionary<object, GuidPath> processedSavablesLookup, Dictionary<object, GuidPath> objectReferenceLookup)
        {
            _objectSaveDataBuffer = objectSaveDataBuffer;
            _originGuidPath = originGuidPath;
            _saveDataBufferLookup = saveDataBufferLookup;
            _processedSavablesLookup = processedSavablesLookup;
            _objectReferenceLookup = objectReferenceLookup;
        }
        
        public void Save(string uniqueIdentifier, object obj)
        {
            if (obj.GetType().IsValueType)
            {
                SaveAsValue(uniqueIdentifier, obj);
            }
            else
            {
                SaveAsReferencable(uniqueIdentifier, obj);
            }
        }

        /// <summary>
        /// Adds an object to the save data buffer using a unique identifier.
        /// Supports all valid types for Newtonsoft Json. Uses less disk space and is faster than Referencable Saving and Loading.
        /// </summary>
        /// <param name="uniqueIdentifier">The unique identifier for the object to be serialized.</param>
        /// <param name="obj">The object to be serialized and added to the buffer.</param>
        public void SaveAsValue(string uniqueIdentifier, object obj)
        {
            _objectSaveDataBuffer.JsonSerializableSaveData.Add(uniqueIdentifier, JToken.FromObject(obj));
        }

        /// <summary>
        /// Attempts to add a referencable object to the save data buffer using a unique identifier.
        /// Supported types:
        /// 1. All Objects that have a unique identifier
        /// 2. Non-MonoBehaviour Classes, that can be instantiated by Activator.CreateInstance() and have Savable Attributes on them or implement the ISavable interface
        /// 3. Serializable Types of types that are supported by <see cref="SaveLoadSystem.Core.Converter.IConvertable"/>
        /// </summary>
        /// <param name="uniqueIdentifier">The unique identifier for the object reference.</param>
        /// <param name="obj">The object to be referenced and added to the buffer.</param>
        /// <returns><c>true</c> if the object reference was successfully added; otherwise, <c>false</c>.</returns>
        public void SaveAsReferencable(string uniqueIdentifier, object obj)
        {
            _objectSaveDataBuffer.GuidPathSaveData.Add(uniqueIdentifier, ConvertToPath(uniqueIdentifier, obj));
        }

        /// <summary>
        /// Attempts to convert an object to a GUID path, so the reference can be identified at deserialization.
        /// </summary>
        /// <param name="uniqueIdentifier">The unique identifier for the object.</param>
        /// <param name="obj">The object to convert to a GUID path.</param>
        /// <param name="guidPath">The resulting GUID path if the conversion is successful.</param>
        /// <returns><c>true</c> if the object was successfully converted to a GUID path; otherwise, <c>false</c>.</returns>
        private GuidPath ConvertToPath(string uniqueIdentifier, object obj)
        {
            if (obj == null)
            {
                //TODO: debug
                return null;
            }
            
            if (_objectReferenceLookup.TryGetValue(obj, out var guidPath)) return guidPath;
            
            if (!_processedSavablesLookup.TryGetValue(obj, out guidPath))
            {
                guidPath = new GuidPath(_originGuidPath.FullPath, uniqueIdentifier);
                SaveSceneManager.ProcessAsSaveReferencable(obj, guidPath, _saveDataBufferLookup, _processedSavablesLookup, _objectReferenceLookup);
            }
            
            return guidPath;
        }
    }
}
