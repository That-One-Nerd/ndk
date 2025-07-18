using System;

namespace NLang.DevelopmentKit.Shared.Arguments;

[AttributeUsage(AttributeTargets.Field)]
public class IsVariableAttribute : Attribute
{
    public string? Name { get; private set; }
    public string? Description { get; private set; }

    public IsVariableAttribute()
    {
        Name = null;
        Description = null;
    }
    public IsVariableAttribute(string? name)
    {
        Name = name;
        Description = null;
    }
    public IsVariableAttribute(string? name, string? description)
    {
        Name = name;
        Description = description;
    }
}
