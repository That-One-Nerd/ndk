using System;
using System.Reflection;

namespace NLang.DevelopmentKit.Shared.Arguments;

public class PositionalInfo
{
    public int Index { get; internal set; }
    public string Name { get; internal set; }
    public string? Description { get; internal set; }
    public FieldInfo Field { get; internal set; }
    public bool IsCollectionType { get; internal set; }
    public bool IsString { get; internal set; }
    public MethodInfo? ParseMethod { get; internal set; }

    internal PositionalInfo()
    {
        // These get taken care of where this object is created.
        // See: ArgumentBase.ArgumentBase()
        Name = null!;
        Field = null!;
        ParseMethod = null!;
    }

    /// <summary>
    /// Useful wrapper for the TryParse method, so we can hide away the ugly casting.
    /// NOTE: This is always expecting a single value! This does NOT handle collection
    /// types, that's done in ArgumentBase.
    /// </summary>
    public bool TryParse(string? str, IFormatProvider? provider, out object? obj)
    {
        if (IsString)
        {
            // Faster copy, no reflection required.
            obj = str;
            return true;
        }
        else
        {
            object?[] args = [str, provider, null];
            bool success = (bool)ParseMethod!.Invoke(null, args)!;

            obj = args[2];
            return success;
        }
    }
}
