using System;

namespace SaveLoadCore.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SavableAttribute : Attribute
    {
    }
}
