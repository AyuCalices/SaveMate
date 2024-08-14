using System;
using System.Diagnostics.CodeAnalysis;
using SaveLoadSystem.Core.Attributes;
using UnityEngine;

namespace Usage
{
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    
    
    // This class demonstrates the use of Savable attributes within a MonoBehaviour.
    public class SaveAttributeExample : MonoBehaviour
    {
        // The 'position' field is marked as [SerializeField] to be editable in the Unity Inspector
        // and [Savable] to be included in the save process.
        [SerializeField, Savable] 
        private Vector3 position;

        // The 'ExampleObject' property is marked with [Savable] to ensure it is included with save and loading.
        [Savable] 
        private ExampleDataTransferObject ExampleObject { get; set; }
    }

    // This class is marked as a savable object using the [SavableObject] attribute.
    // All fields and properties within this class will be automatically marked for saving.
    [Serializable, SavableObject]
    public class ExampleDataTransferObject
    {
        // These public fields will be saved and loaded automatically due to the SavableObject attribute.
        public string Name;
        public int Health;

        // Since ExampleObject is a custom class, it needs to be instantiated during loading.
        // This is done using Activator.CreateInstance(), which requires a parameterless constructor.
        // The parameterless constructor allows the object to be instantiated without providing arguments.
        public ExampleDataTransferObject() {}

        public ExampleDataTransferObject(string name, int health)
        {
            Name = name;
            Health = health;
        }
    }
    
}
