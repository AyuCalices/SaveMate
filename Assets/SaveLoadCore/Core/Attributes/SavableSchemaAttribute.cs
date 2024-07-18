using System;

namespace SaveLoadCore.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SavableSchemaAttribute : Attribute
    {
    }
}