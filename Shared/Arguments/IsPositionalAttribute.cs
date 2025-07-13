using System;

namespace NLang.DevelopmentKit.Shared.Arguments;

[AttributeUsage(AttributeTargets.Field)]
public class IsPositionalAttribute : Attribute
{
    public string? Name { get; private set; }
    public string? Description { get; private set; }
    public int Index { get; private set; }

    public IsPositionalAttribute(int index)
    {
        Index = index;
        Name = null;
        Description = null;
    }
    public IsPositionalAttribute(int index, string name)
    {
        Index = index;
        Name = name;
        Description = null;
    }
    public IsPositionalAttribute(int index, string name, string description)
    {
        Index = index;
        Name = name;
        Description = description;
    }
}
