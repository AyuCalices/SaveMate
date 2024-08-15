using System;
using SaveLoadSystem.Core.DataTransferObject;

namespace SaveLoadSystem.Core
{
    /// <summary>
    /// The <see cref="LoadDataHandler"/> class is responsible for managing the deserialization and retrieval of
    /// serialized data, as well as handling reference building for complex object graphs.
    /// </summary>
    public class LoadDataHandler
    {
        private readonly SaveDataBuffer _loadSaveDataBuffer;
        private readonly DeserializeReferenceBuilder _deserializeReferenceBuilder;

        public LoadDataHandler(SaveDataBuffer loadSaveDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            _loadSaveDataBuffer = loadSaveDataBuffer;
            _deserializeReferenceBuilder = deserializeReferenceBuilder;
        }

        /// <summary>
        /// Retrieves an object of type <typeparamref name="T"/> associated with the specified identifier.
        /// Supports all valid types for Newtonsoft Json. Uses less disk space and is faster than Referencable Saving and Loading.
        /// </summary>
        /// <typeparam name="T">The type to which the JSON object should be deserialized.</typeparam>
        /// <param name="identifier">The unique identifier for the JSON object in the save data buffer.</param>
        /// <returns>
        /// The object of type <typeparamref name="T"/> if found and successfully deserialized;
        /// otherwise, returns the default value of <typeparamref name="T"/>.
        /// </returns>
        public T LoadValue<T>(string identifier)
        {
            if (_loadSaveDataBuffer.CustomSerializableSaveData[identifier] == null)
            {
                return default;
            }
            
            return _loadSaveDataBuffer.CustomSerializableSaveData[identifier].ToObject<T>();
        }
        
        /// <summary>
        /// Attempts to retrieve a <see cref="GuidPath"/> associated with the specified identifier.
        /// </summary>
        /// <param name="identifier">The unique identifier for the <see cref="GuidPath"/>.</param>
        /// <param name="guidPath">
        /// When this method returns, contains the <see cref="GuidPath"/> associated with the specified identifier,
        /// if the identifier is found; otherwise, the default value for the type of the guidPath parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <see cref="GuidPath"/> is found; otherwise, <c>false</c>.
        /// </returns>
        public bool TryLoadReferencable(string identifier, out GuidPath guidPath)
        {
            return _loadSaveDataBuffer.CustomGuidPathSaveData.TryGetValue(identifier, out guidPath);
        }

        /// <summary>
        /// Enqueues an action to be executed when a reference corresponding to the specified path is found.
        /// </summary>
        /// <param name="path">The path used to identify the reference.</param>
        /// <param name="onReferenceFound">
        /// The action to be executed when the reference is found. It takes the found object as a parameter.
        /// </param>
        public void EnqueueReferenceBuilding(GuidPath path, Action<object> onReferenceFound)
        {
            _deserializeReferenceBuilder.EnqueueReferenceBuilding(path, onReferenceFound);
        }

        /// <summary>
        /// Enqueues an action to be executed when references corresponding to the specified path group are found.
        /// </summary>
        /// <param name="pathGroup">An array of paths used to identify the references.</param>
        /// <param name="onReferenceFound">
        /// The action to be executed when all references are found. It takes an array of the found objects as a parameter.
        /// </param>
        public void EnqueueReferenceBuilding(GuidPath[] pathGroup, Action<object[]> onReferenceFound)
        {
            _deserializeReferenceBuilder.EnqueueReferenceBuilding(pathGroup, onReferenceFound);
        }
    }
}
