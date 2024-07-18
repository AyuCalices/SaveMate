using System;

namespace SaveLoadSystem.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SavableSchemaAttribute : Attribute
    {
    }
}