using System;

namespace NLang.DevelopmentKit.Shared.Arguments;

/// <summary>
/// Intended for array-based arguments. Specifies that the rest of the arguments
/// after this point should be treated as elements in this array.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class RemainderAttribute : Attribute { }
