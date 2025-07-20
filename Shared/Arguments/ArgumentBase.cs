using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
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
    public static ReadOnlyCollection<VariableInfo> VariableArguments { get; private set; }
    public static ReadOnlyCollection<FlagInfo> FlagArguments { get; private set; }

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

        // NOTE: This category system only works as intended so long as the fields
        //       provided by the GetFields() method are returned in the order they
        //       are defined in the file. GitHub tells me this is not *always* the
        //       case.
        string currentCategory = "";
        List<PositionalInfo> posInfos = [];
        List<VariableInfo> varInfos = [];
        List<FlagInfo> flagInfos = [];
        List<FieldInfo> invalids = [];
        foreach (FieldInfo field in fields)
        {
            // Get attributes. Conversion to information types is below.
            IsPositionalAttribute? posAtt = field.GetCustomAttribute<IsPositionalAttribute>();
            IsVariableAttribute? varAtt = field.GetCustomAttribute<IsVariableAttribute>();
            IsFlagAttribute? flagAtt = field.GetCustomAttribute<IsFlagAttribute>();
            CategoryAttribute? catAtt = field.GetCustomAttribute<CategoryAttribute>();
            RequiredAttribute? requiredAtt = field.GetCustomAttribute<RequiredAttribute>();

            // Handle category changes before everything else, because even an
            // improperly defined argument should still consider the new category.
            if (catAtt is not null) currentCategory = catAtt.Category;

            // Double-check that this field has a parsable return type.
            // If this is a collection type, use the element type for parsing.
            Type parseType = field.FieldType;
            bool remainder = false;
            bool collection = IsCollectionType(parseType, out Type? subType);
            if (collection)
            {
                parseType = subType!;

                // Check for the Remainder attribute, since this is a collection type.
                remainder = field.GetCustomAttribute<RemainderAttribute>() is not null;
            }

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
            ArgumentInfo? argInfo = null;
            if (posAtt is not null) // Positional Argument
            {
                // TODO: Should we ignore a positional argument if there are two with the same index?

                argInfo = new PositionalInfo()
                {
                    Index = posAtt.Index,
                    Name = posAtt.Name ?? field.Name, // If a name is not specified, autofill with the field name.
                    Description = posAtt.Description
                };
                posInfos.Add((argInfo as PositionalInfo)!);
            }
            else if (varAtt is not null) // Variable Argument
            {
                argInfo = new VariableInfo()
                {
                    Name = varAtt.Name ?? $"-{field.Name}",
                    Description = varAtt.Description
                };
                varInfos.Add((argInfo as VariableInfo)!);
            }
            else if (flagAtt is not null) // Flag Argument
            {
                // Double check that the field type is compatible.
                if (field.FieldType != typeof(bool) && field.FieldType != typeof(int))
                {
                    // Gotta be a bool or an int.
                    // The actual conversion is below in the parse function.
                    // THESE CHECKS SHOULD ALWAYS BE THE SAME OR WEIRD STUFF WILL HAPPEN.
                    invalids.Add(field);
                    continue;
                }

                argInfo = new FlagInfo()
                {
                    Name = flagAtt.Name ?? $"--{field.Name}",
                    Description = flagAtt.Description
                };
                flagInfos.Add((argInfo as FlagInfo)!);
            }

            if (argInfo is not null)
            {
                // Fill in the remaining shared information.
                argInfo.Field = field;
                argInfo.Category = currentCategory;
                argInfo.IsCollectionType = collection;
                argInfo.ParseMethod = parseMethod;
                argInfo.ElementType = parseType;
                argInfo.IsRemainder = remainder;
                argInfo.Required = requiredAtt is not null;
                all.Add(argInfo);
            }
        }

        // Done reading info, store in readonlycollections.
        PositionalArguments = new(posInfos);
        VariableArguments = new(varInfos);
        FlagArguments = new(flagInfos);
        InvalidArguments = new(invalids);
        allArguments = new(all);
    }

    // Exposes which specific arguments were passed to the Parse() method.
    // This is to help detect when a program was called with no arguments,
    // for example.
    public bool AnyArguments { get; private set; }
    public ReadOnlyCollection<string> ParsedArguments { get; private set; } = null!;
    public ReadOnlyCollection<string> DuplicateArguments { get; private set; } = null!;
    public ReadOnlyCollection<string> MissingArguments { get; private set; } = null!;
    public ReadOnlyCollection<string> UnknownArguments { get; private set; } = null!;
    public ReadOnlyCollection<string> UnparsedArguments { get; private set; } = null!; // These are both set in the Parse() method.

    public static ArgumentInfo? GetArgumentByName(string name) =>
        allArguments.FirstOrDefault(x => x.Name == name);

    public static IEnumerable<ArgumentInfo> GetInfoByCategory(string category) => allArguments.Where(x => x.Category == category);
    public static TSelf Parse(string[] argsStr)
    {
        TSelf result = new();

        List<string> parsed = [], duplicate = [], unknown = [], unparsed = [],
                     missing = [.. from arg in allArguments
                                   where arg.Required
                                   select arg.Name];

        int posIndex = 0;
        for (int i = 0; i < argsStr.Length; i++)
        {
            string arg = argsStr[i];

            ArgumentInfo? argInfo;
            if ((argInfo = FlagArguments.FirstOrDefault(x => x.Name == arg)) is not null) // Try for a flag argument.
            {
                if (parsed.Contains(arg))
                {
                    // Already flipped flag, this is a duplicate!
                    duplicate.Add(arg);
                    continue;
                }

                // Flip the value of this flag.
                object? val = argInfo.Field.GetValue(result);
                if (val is bool valBool) val = !valBool;
                else if (val is int valInt) val = valInt > 0 ? 0 : 1;
                else val = default;

                // We should always hit one of those conditions. It's checked for in the static constructor.
                // THESE CHECKS SHOULD ALWAYS BE THE SAME OR WEIRD STUFF WILL HAPPEN.
                argInfo.Field.SetValue(result, val);
                parsed.Add(arg);
            }
            else if ((argInfo = VariableArguments.FirstOrDefault(x => x.Name == arg.Split(':')[0])) is not null) // Try for a variable argument.
            {
                if (parsed.Contains(argInfo.Name))
                {
                    // Already set variable, this is a duplicate!
                    duplicate.Add(argInfo.Name);
                    continue;
                }

                // This parser handles two formats for variables:
                // -varName:varValue
                // -varName varValue
                // That's why the name check above is weird. We need to only search for the name.

                string name, value;
                int separator = arg.IndexOf(':');
                if (separator != -1)
                {
                    name = arg[..separator];
                    value = arg[(separator + 1)..];
                }
                else
                {
                    i++;
                    if (i >= argsStr.Length)
                    {
                        // Overflow. Can't parse this argument.
                        unparsed.Add(argInfo.Name);
                        continue;
                    }
                    name = arg;
                    value = argsStr[i];
                }

                // Parse the value
                GeneralTryParse(value, argInfo, ref i);
            }
            else // Try for a positional argument.
            {
                if (PositionalArguments.Any(x => x.Index == posIndex))
                {
                    // This is a positional argument.
                    argInfo = PositionalArguments.First(x => x.Index == posIndex);
                    GeneralTryParse(arg, argInfo, ref i);
                    posIndex++;
                }
                else
                {
                    // Doesn't match anything. Likely outside the range of positional arguments.
                    unknown.Add(arg);
                    posIndex++;
                }
            }

            if (argInfo is not null) missing.Remove(argInfo.Name);
        }

        // Set extra collections and we're done!
        result.AnyArguments = parsed.Count > 0;
        result.ParsedArguments = new(parsed);
        result.DuplicateArguments = new(duplicate);
        result.MissingArguments = new(missing);
        result.UnknownArguments = new(unknown);
        result.UnparsedArguments = new(unparsed);

        return result;

        bool GeneralTryParse(string arg, ArgumentInfo argInfo, ref int arrIndex)
        {
            if (argInfo.IsCollectionType)
            {
                string[] parts;
                if (argInfo.IsRemainder)
                {
                    // Fetch other parameters from the argument array. We set the index here
                    // so the loop above stops after this. We've exhausted all arguments by now.
                    parts = argsStr[arrIndex..];
                    arrIndex = argsStr.Length;
                }
                else
                {
                    // Use the format "[itemA,itemB,itemC]"
                    arg = arg.Trim();
                    if (!arg.StartsWith('[') || !arg.EndsWith(']'))
                    {
                        // The brackets are missing or incomplete.
                        unparsed.Add(argInfo.Name);
                        return false;
                    }
                    parts = arg[1..^1].Split(',');
                }

                try
                {
                    Array argArr = Array.CreateInstance(argInfo.ElementType, parts.Length);
                    bool allFail = true;
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (argInfo.TryParseElement(parts[i], null, out object? argElement))
                        {
                            // Parsed this element.
                            argArr.SetValue(argElement, i);
                            allFail = false;
                        }
                        else
                        {
                            // Failed the parse. This is tricky. We still set the array and
                            // consider this a pass, but we also mark the specific index as
                            // unparsed.
                            unparsed.Add($"{argInfo.Name}[{i}]");
                            continue;
                        }
                    }
                    // Now we're done, we can set the argument to the completed array.
                    argInfo.Field.SetValue(result, argArr);
                    bool pass = !(allFail && parts.Length > 0);
                    if (pass) parsed.Add(argInfo.Name); // If at least one element parsed, we consider this a pass.
                    return pass;
                }
                catch
                {
                    // Some failure with the array creation, most likely.
                    // But we can't consider this a success.
                    return false;
                }
            }

            // Parse the value and save it.
            if (argInfo.TryParseElement(arg, null, out object? argParsed))
            {
                // Success!
                argInfo.Field.SetValue(result, argParsed);
                parsed.Add(argInfo.Name);
                return true;
            }
            else
            {
                // Failed to parse, oops.
                unparsed.Add(argInfo.Name);
                return false;
            }
        }
    }

    public static void PrintCategory(string category, Func<string, string?>? argumentFormats = null)
    {
        IEnumerable<ArgumentInfo> infos = GetInfoByCategory(category);
        int count = infos.Count();
        StringBuilder result = new();
        result.Append($"{new string(' ', 2)}\x1b[1;97m{category}:\x1b[22m\n");

        int maxLength = 0, index = 0;
        StringBuilder[] lines = new StringBuilder[count];
        foreach (ArgumentInfo arg in infos)
        {
            string format = argumentFormats?.Invoke(arg.Name) ?? GetArgumentFormat(arg);
            lines[index++] = new StringBuilder().Append($"{new string(' ', 4)}{format}{arg.Name}");
            if (arg.Name.Length > maxLength) maxLength = arg.Name.Length;
        }

        int desired = maxLength + 2;
        index = 0;
        foreach (ArgumentInfo arg in infos)
        {
            if (!string.IsNullOrWhiteSpace(arg.Description))
            {
                int remaining = desired - arg.Name.Length;
                lines[index].Append($"{new string(' ', remaining)}\x1b[91m- \x1b[37m{arg.Description}\x1b[0m");
            }
            result.Append(lines[index++]);
            result.AppendLine();
        }
        Console.WriteLine(result);
    }
    public bool PrintAnyIssues(Func<string, string?>? argumentFormats = null)
    {
        // If we || these together like normal,
        // one being true will cause the program to not print
        // the other ones (since we know the result will be true).
        bool fail = false;
        fail |= PrintUnknown(argumentFormats);
        fail |= PrintDuplicates(argumentFormats);
        fail |= PrintMissing(argumentFormats);
        fail |= PrintUnparsed(argumentFormats);
        return fail;
    }
    public bool PrintDuplicates(Func<string, string?>? argumentFormats = null)
    {
        int count = DuplicateArguments.Count;
        if (count == 0) return false;

        StringBuilder result = new();
        result.Append($"\x1b[3;33m  {count} {(count == 1 ? "argument was" : "arguments were")} included more than once:\n  ");

        int lineLen = 2, maxLineLen = (int)(0.65 * Console.WindowWidth - 1);
        foreach (string badArg in DuplicateArguments)
        {
            ArgumentInfo badArgInfo = GetArgumentByName(badArg)!;
            if (lineLen + badArg.Length + 3 > maxLineLen)
            {
                // New line.
                result.Append("\n  ");
                lineLen = 2;
            }
            string format = argumentFormats?.Invoke(badArg) ?? GetArgumentFormat(badArgInfo);
            result.Append($"\"{format}{badArg}\x1b[0;3;33m\" ");
            lineLen += badArg.Length + 3;
        }
        Console.WriteLine(result.AppendLine("\x1b[0m"));
        return true;
    }
    public bool PrintMissing(Func<string, string?>? argumentFormats = null)
    {
        int count = MissingArguments.Count;
        if (count == 0) return false;

        StringBuilder result = new();
        result.Append($"\x1b[3;91m  {count} {(count == 1 ? "argument is" : "arguments are")} missing:\n  ");

        int lineLen = 2, maxLineLen = (int)(0.65 * Console.WindowWidth - 1);
        foreach (string badArg in MissingArguments)
        {
            ArgumentInfo badArgInfo = GetArgumentByName(badArg)!;
            if (lineLen + badArg.Length + 3 > maxLineLen)
            {
                // New line.
                result.Append("\n  ");
                lineLen = 2;
            }
            string format = argumentFormats?.Invoke(badArg) ?? GetArgumentFormat(badArgInfo);
            result.Append($"\"{format}{badArg}\x1b[0;3;91m\" ");
            lineLen += badArg.Length + 3;
        }
        Console.WriteLine(result.AppendLine("\x1b[0m"));
        return true;
    }
    public bool PrintUnknown(Func<string, string?>? argumentFormats = null)
    {
        int count = UnknownArguments.Count;
        if (count == 0) return false;

        StringBuilder result = new();
        result.Append($"\x1b[3;33m  {count} {(count == 1 ? "argument was" : "arguments were")} not recognized:\n  ");

        int lineLen = 2, maxLineLen = (int)(0.65 * Console.WindowWidth - 1);
        foreach (string badArg in UnknownArguments)
        {
            if (lineLen + badArg.Length + 3 > maxLineLen)
            {
                // New line.
                result.Append("\n  ");
                lineLen = 2;
            }
            string format = argumentFormats?.Invoke(badArg) ?? GetArgumentFormat(badArg);
            result.Append($"\"{format}{badArg}\x1b[0;3;33m\" ");
            lineLen += badArg.Length + 3;
        }
        Console.WriteLine(result.AppendLine("\x1b[0m"));
        return true;
    }
    public bool PrintUnparsed(Func<string, string?>? argumentFormats = null)
    {
        int count = UnparsedArguments.Count;
        if (count == 0) return false;

        StringBuilder result = new();
        result.Append($"\x1b[3;91m  {count} {(count == 1 ? "argument has" : "arguments have")} failed to be parsed:\n  ");

        int lineLen = 2, maxLineLen = (int)(0.65 * Console.WindowWidth - 1);
        foreach (string badArg in UnparsedArguments)
        {
            ArgumentInfo? badArgInfo = GetArgumentByName(badArg);
            if (lineLen + badArg.Length + 3 > maxLineLen)
            {
                // New line.
                result.Append("\n  ");
                lineLen = 2;
            }
            string format = argumentFormats?.Invoke(badArg) ?? (badArgInfo is null ? GetArgumentFormat(badArg) : GetArgumentFormat(badArgInfo));
            result.Append($"\"{format}{badArg}\x1b[0;3;91m\" ");
            lineLen += badArg.Length + 3;
        }
        Console.WriteLine(result.AppendLine("\x1b[0m"));
        return true;
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
    private static string GetArgumentFormat(ArgumentInfo arg)
    {
        if (arg is FlagInfo) return "\x1b[90m";
        else if (arg is VariableInfo) return "\x1b[36m";
        else return "\x1b[37m"; // Positional is here.
    }
    private static string GetArgumentFormat(string argName)
    {
        if (argName.StartsWith("--")) return "\x1b[90m";
        else if (argName.StartsWith('-')) return "\x1b[36m";
        else return "\x1b[37m"; // Positional is here.
    }
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
