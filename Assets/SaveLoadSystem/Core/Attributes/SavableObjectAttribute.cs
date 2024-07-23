using System;
namespace SaveLoadSystem.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class SavableObjectAttribute : Attribute
    {
        public bool DeclaredOnly { get; }
        
        public SavableObjectAttribute(bool declaredOnly = true)
        {
            DeclaredOnly = declaredOnly;
        }
    }
}