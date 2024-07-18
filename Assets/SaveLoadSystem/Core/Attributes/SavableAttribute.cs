using System;

namespace SaveLoadSystem.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SavableAttribute : Attribute
    {
    }
}
