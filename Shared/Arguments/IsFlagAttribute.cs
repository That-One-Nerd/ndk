using System;

namespace NLang.DevelopmentKit.Shared.Arguments;

[AttributeUsage(AttributeTargets.Field)]
public class IsFlagAttribute : Attribute
{
    public string? Name { get; private set; }
    public string? Description { get; private set; }
    public string Category { get; set; } = "";

    public IsFlagAttribute()
    {
        Name = null;
        Description = null;
    }
    public IsFlagAttribute(string? name)
    {
        Name = name;
        Description = null;
    }
    public IsFlagAttribute(string? name, string? description)
    {
        Name = name;
        Description = description;
    }
}
