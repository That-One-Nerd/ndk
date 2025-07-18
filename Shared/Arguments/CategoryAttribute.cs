using System;

namespace NLang.DevelopmentKit.Shared.Arguments;

[AttributeUsage(AttributeTargets.Field)]
public class CategoryAttribute(string? category) : Attribute
{
    public string Category { get; private set; } = category ?? "";
}
