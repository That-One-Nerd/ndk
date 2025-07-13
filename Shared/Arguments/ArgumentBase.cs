using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace NLang.DevelopmentKit.Shared.Arguments;

/// <summary>
/// The base class for handling argument parsing. Inherit from this class and
/// use the attributes in this namespace to easily create more argument structures.
/// </summary>
public abstract class ArgumentBase<TSelf> where TSelf : ArgumentBase<TSelf>, new()
{
    // Exposes information about the arguments. This is theoretically not publically needed,
    // but is needed privately for the parsing system.
    public static readonly ReadOnlyCollection<PositionalInfo> PositionalArguments;

    // This also exposes debugging information.
    public static readonly ReadOnlyCollection<FieldInfo> InvalidArguments;

    static ArgumentBase()
    {
        // Analyze the argument structure for positional, variable, or flag arguments
        // associated with fields.

        Type t = typeof(TSelf);
        FieldInfo[] fields = t.GetFields();

        List<PositionalInfo> posInfos = [];
        List<FieldInfo> invalids = [];
        foreach (FieldInfo field in fields)
        {
            // Get attributes. Conversion to information types is below.
            IsPositionalAttribute? posAtt = field.GetCustomAttribute<IsPositionalAttribute>();

            // Double-check that this field has a parsable return type.
            // If this is a collection type, use the element type for parsing.
            Type parseType = field.FieldType;
            bool collection = IsCollectionType(parseType, out Type? subType);
            if (collection) parseType = subType!;

            // If it is, use reflection to load the specific parse method we need.
            // If it isn't, skip this field.
            if (parseType.GetInterface("System.IParsable`1") is null && parseType != typeof(string))
            {
                // This field cannot be parsed into, consider it invalid.
                invalids.Add(field);
                continue;
            }

            // VERY specific getmethod arguments so we don't have any ambiguity with other
            // parse method overloads. The `IParsable` interface guarantees this method will be defined,
            // except in the case of a string. For that reason, there's a catch for that in the PositionalInfo.TryParse method.
            MethodInfo parseMethod = parseType.GetMethod("TryParse", BindingFlags.Public | BindingFlags.Static, [typeof(string), typeof(IFormatProvider), parseType.MakeByRefType()])!;

            // If this field matches any of those attributes, convert it to its information type
            // for later use.
            if (posAtt is not null)
            {
                // TODO: Should we ignore a positional argument if there are two with the same index?

                PositionalInfo posInfo = new()
                {
                    Index = posAtt.Index,
                    Name = posAtt.Name ?? field.Name, // If a name is not specified, autofill with the field name.
                    Description = posAtt.Description,
                    Field = field,
                    IsCollectionType = collection,
                    ParseMethod = parseMethod,
                    IsString = parseType == typeof(string)
                };
                posInfos.Add(posInfo);
            }
        }

        // Done reading info, store in readonlycollections.
        PositionalArguments = new(posInfos);
        InvalidArguments = new(invalids);
    }

    // Exposes which specific arguments were passed to the Parse() method.
    // This is to help detect when a program was called with no arguments,
    // for example.
    public bool AnyArguments { get; private set; }
    public ReadOnlyCollection<string> ParsedArguments { get; private set; } = null!;
    public ReadOnlyCollection<string> UnparsedArguments { get; private set; } = null!; // These are both set in the Parse() method.

    public static TSelf Parse(string[] argsStr)
    {
        TSelf result = new();

        List<string> parsed = [], unparsed = [];

        int posIndex = 0;
        for (int i = 0; i < argsStr.Length; i++)
        {
            string arg = argsStr[i];
            // TODO: Check for variables and flags.

            if (!PositionalArguments.Any(x => x.Index == posIndex))
            {
                // This particular index doesn't have a positional argument,
                // most likely because this is one too many arguments. So we
                // can't parse it.
                unparsed.Add(arg);
                posIndex++;
                continue;
            }
            PositionalInfo posArg = PositionalArguments.First(x => x.Index == posIndex);
            if (posArg.IsCollectionType)
            {
                // TODO: We need to parse these.
                //       The format will be: "[itemA,itemB,itemC]"
                unparsed.Add(posArg.Name);
                posIndex++;
                continue;
            }

            // Parse the value and save it.
            if (posArg.TryParse(arg, null, out object? argParsed))
            {
                // Success!
                posArg.Field.SetValue(result, argParsed);
                parsed.Add(posArg.Name);
            }
            else
            {
                // Failed to parse, oops.
                unparsed.Add(posArg.Name);
            }
            posIndex++; // Get ready for the next positional argument.
        }

        // Set extra collections and we're done!
        result.AnyArguments = parsed.Count > 0;
        result.ParsedArguments = new(parsed);
        result.UnparsedArguments = new(unparsed);

        return result;
    }

    #region Helper Functions
    private static bool IsCollectionType(Type type, [NotNullWhen(true)] out Type? subType)
    {
        if (type.IsArray)
        {
            subType = type.GetElementType()!;
            return true;
        }
        // TODO: support for lists and possibly ienumerables.

        subType = null;
        return false;
    }
    #endregion
}
