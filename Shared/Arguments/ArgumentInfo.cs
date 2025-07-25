﻿using System;
using System.Reflection;

namespace NLang.DevelopmentKit.Shared.Arguments;

public abstract class ArgumentInfo
{
    public string Category { get; set; }
    public string Name { get; internal set; }
    public string? Description { get; internal set; }
    public FieldInfo Field { get; internal set; }
    public bool IsCollectionType { get; internal set; }
    public bool IsRemainder { get; internal set; }
    public MethodInfo? ParseMethod { get; internal set; }
    public Type ElementType { get; internal set; }
    public bool Required { get; internal set; }

    internal ArgumentInfo()
    {
        // These get taken care of where this object is created.
        // See: ArgumentBase.ArgumentBase()
        Category = "";
        Name = null!;
        Field = null!;
        ElementType = null!;
    }

    /// <summary>
    /// Useful wrapper for the TryParse method, so we can hide away the ugly casting.
    /// NOTE: This is always expecting a single value! This does NOT handle collection
    /// types, that's done in ArgumentBase.
    /// </summary>
    public bool TryParseElement(string? str, out object? obj)
    {
        if (ElementType == typeof(string))
        {
            // Faster copy, no reflection required.
            obj = str;
            return true;
        }
        else if (ElementType.IsEnum)
        {
            // Call the method slightly differently, since this is an enum.
            // It's a bit easier to be honest.
            bool success = Enum.TryParse(ElementType, str, true, out object? result);
            obj = result;
            return success;
        }
        else
        {
            object?[] args = [str, null, null];
            bool success = (bool)ParseMethod!.Invoke(null, args)!;

            obj = args[2];
            return success;
        }
    }
}
