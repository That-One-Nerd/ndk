using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NLang.DevelopmentKit.Shared.Arguments;

/// <summary>
/// The base class for handling argument parsing. Inherit from this class and
/// use the attributes in this namespace to easily create more argument structures.
/// </summary>
public abstract class ArgumentBase<TSelf> where TSelf : ArgumentBase<TSelf>, new()
{
    // Exposes information about the arguments. This is theoretically not publically needed,
    // but is needed privately for the parsing system.
    public static ReadOnlyCollection<PositionalInfo> PositionalArguments { get; private set; }

    // This also exposes debugging information.
    public static ReadOnlyCollection<FieldInfo> InvalidArguments { get; private set; }

    // This is purely for the ToString() method, it's a combination of all the arguments
    // that respects the original order they were placed it.
    private static readonly ReadOnlyCollection<ArgumentInfo> allArguments;

    static ArgumentBase()
    {
        // Analyze the argument structure for positional, variable, or flag arguments
        // associated with fields.

        Type t = typeof(TSelf);
        FieldInfo[] fields = t.GetFields();
        List<ArgumentInfo> all = [];

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
                all.Add(posInfo);
            }
        }

        // Done reading info, store in readonlycollections.
        PositionalArguments = new(posInfos);
        InvalidArguments = new(invalids);
        allArguments = new(all);
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
            //       When we do that, put the positional code in an "else" statement.

            if (PositionalArguments.Any(x => x.Index == posIndex))
            {
                // This is a positional argument.
                PositionalInfo posArg = PositionalArguments.First(x => x.Index == posIndex);
                if (GeneralTryParse(arg, posArg, result)) parsed.Add(posArg.Name);
                else unparsed.Add(posArg.Name);
                posIndex++;
            }
            else
            {
                // Doesn't match anything. Likely outside the range of positional arguments.
                unparsed.Add(arg);
                posIndex++;
            }
        }

        // Set extra collections and we're done!
        result.AnyArguments = parsed.Count > 0;
        result.ParsedArguments = new(parsed);
        result.UnparsedArguments = new(unparsed);

        return result;

        static bool GeneralTryParse(string arg, ArgumentInfo argInfo, TSelf result)
        {
            if (argInfo.IsCollectionType)
            {
                // TODO: We need to parse these.
                //       The format will be "[itemA,itemB,itemC]"
                return false;
            }

            // Parse the value and save it.
            if (argInfo.TryParseElement(arg, null, out object? argParsed))
            {
                // Success!
                argInfo.Field.SetValue(result, argParsed);
                return true;
            }
            else return false; // Failed to parse, oops.
        }
    }

    public override string ToString()
    {
        // TODO: Split up this function and make it more colorful.
        //       Break it up into printing warnings for unparsed arguments or
        //       unset required arguments or such.

        StringBuilder builder = new();
        foreach (ArgumentInfo arg in allArguments)
        {
            builder.Append($"{arg.Name}: {arg.Field.GetValue(this)}");
            if (!ParsedArguments.Contains(arg.Name)) builder.Append(" (unset)");
            builder.AppendLine();
        }

        if (UnparsedArguments.Count > 0)
        {
            builder.Append($"\n{UnparsedArguments.Count} {(UnparsedArguments.Count == 1 ? "argument" : "arguments")} failed to parse: ");
            for (int i = 0; i < UnparsedArguments.Count; i++)
            {
                builder.Append(UnparsedArguments[i]);
                if (i < UnparsedArguments.Count - 1) builder.Append(", ");
            }
            builder.AppendLine();
        }

        return builder.ToString();
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
