using NLang.DevelopmentKit.Shared.Arguments;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NLang.DevelopmentKit.Shared.Helpers;

public static class PrintHelper
{
    public static bool GetYesNoQuery()
    {
    _getkey:
        int key = Console.IsInputRedirected ? Console.Read() : Console.ReadKey(true).KeyChar;

        bool yes;
        switch (key)
        {
            case 'y' or 'Y':
                yes = true;
                break;

            case 'n' or 'N':
                yes = false;
                break;

            default: goto _getkey;
        }

        if (yes) Console.WriteLine("\x1b[1;32m[ Yes ]\x1b[0m");
        else Console.WriteLine("\x1b[1;31m[ No ]\x1b[0m");
        return yes;
    }

    public static void PrintEnum<T>(string title, int indent, string keyFormat, string? separatorFormat = null, string? descFormat = null)
        where T : struct, Enum => PrintEnum(title, indent, (T val) => keyFormat, separatorFormat, descFormat);
    public static void PrintEnum<T>(string title, int indent, Func<T, string>? keyFormat = null, string? separatorFormat = null,
        string? descFormat = null) where T : struct, Enum
    {
        StringBuilder result = new();
        result.Append($"{new string(' ', indent)}\x1b[1;97m{title}:\x1b[22m\n");

        IEnumerable<FieldInfo> values = typeof(T).GetFields().Where(x => x.IsStatic);
        int maxLength = 0;
        StringBuilder[] lines = new StringBuilder[values.Count()];
        int index = 0;
        foreach (FieldInfo valField in values)
        {
            string valStr = valField.Name;
            T val = (T)valField.GetValue(null)!;
            lines[index] = new StringBuilder().Append($"{new string(' ', indent + 2)}{keyFormat?.Invoke(val) ?? "\x1b[90m"}{valStr}");
            if (valStr.Length > maxLength) maxLength = valStr.Length;
            index++;
        }

        int desired = maxLength + 2;
        index = 0;
        foreach (FieldInfo valField in values)
        {
            DescriptionAttribute? desc = valField.GetCustomAttribute<DescriptionAttribute>();

            string valStr = valField.Name;
            int remaining = desired - valStr.Length;
            lines[index].Append($"{new string(' ', remaining)}{separatorFormat ?? "\x1b[91m- "}{descFormat ?? "\x1b[37m"}{desc?.Description ?? "\x1b[3mNo Description\x1b[23m"}\x1b[0m");
            result.Append(lines[index]);
            result.AppendLine();
            index++;
        }
        Console.WriteLine(result);
    }
    public static void PrintList(string title, int indent, IEnumerable<string> values, string? valueFormat = null)
    {
        StringBuilder result = new();
        result.Append($"{new string(' ', indent)}\x1b[1;97m{title}:\x1b[22m\n");
        foreach (string v in values) result.Append($"{new string(' ', indent + 2)}{valueFormat ?? "\x1b[37m"}{v}\n");
        result.Append("\x1b[0m");
        Console.WriteLine(result);
    }
    public static void PrintKeyValues(string title, int indent, Dictionary<string, string> values,
                                      string? keyFormat = null, string? separatorFormat = null, string? valueFormat = null)
    {
        StringBuilder result = new();
        result.Append($"{new string(' ', indent)}\x1b[1;97m{title}:\x1b[22m\n");

        int maxLength = 0, index = 0;
        StringBuilder[] lines = new StringBuilder[values.Count];
        foreach (KeyValuePair<string, string> kv in values)
        {
            lines[index++] = new StringBuilder().Append($"{new string(' ', indent + 2)}{keyFormat ?? "\x1b[90m"}{kv.Key}");
            if (kv.Key.Length > maxLength) maxLength = kv.Key.Length;
        }

        int desired = maxLength + 2;
        index = 0;
        foreach (KeyValuePair<string, string> kv in values)
        {
            int remaining = desired - kv.Key.Length;
            lines[index].Append($"{new string(' ', remaining)}{separatorFormat ?? "\x1b[91m- "}{valueFormat ?? "\x1b[37m"}{kv.Value}\x1b[0m");
            result.Append(lines[index++]);
            result.AppendLine();
        }
        Console.WriteLine(result);
    }

    public static void PrintArgumentCategory<TArg>(string category) where TArg : ArgumentBase<TArg>, new()
    {
        IEnumerable<ArgumentInfo> infos = ArgumentBase<TArg>.GetInfoByCategory(category);
        int count = infos.Count();
        StringBuilder result = new();
        result.Append($"{new string(' ', 2)}\x1b[1;97m{category}:\x1b[22m\n");

        int maxLength = 0, index = 0;
        StringBuilder[] lines = new StringBuilder[count];
        foreach (ArgumentInfo arg in infos)
        {
            string format = "\x1b[37m";
            if (arg is VariableInfo) format = "\x1b[36m";
            else if (arg is FlagInfo) format = "\x1b[90m";

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
}

