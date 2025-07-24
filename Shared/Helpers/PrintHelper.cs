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

    public static void PrintLargeString(string title, int indent, string text)
    {
        int maxLineLength = (int)(0.65 * Console.WindowWidth + 1) - indent - 2;
        string newLine = $"\n{new string(' ', indent + 2)}";
        StringBuilder result = new($"{new string(' ', indent)}\x1b[1;97m{title}:\x1b[0m{newLine}");

        int textIndex = 0, lineIndex = 0;
        text = text.Replace("\r\n", " ").Replace('\n', ' ').Trim();
        while (text.Contains("  ")) text = text.Replace("  ", " ");
        while (textIndex < text.Length)
        {
            int nextWordEnd = text.IndexOf(' ', textIndex);
            if (nextWordEnd == -1) nextWordEnd = text.Length;

            string word = text[textIndex..nextWordEnd];
            textIndex = nextWordEnd + 1;

            while (word.Length > maxLineLength)
            {
                // The whole word is too big to fit on this line.
                // Just break it up, whatever.
                int remaining = maxLineLength - lineIndex;
                string thisLinePart = word[..remaining];
                word = word[remaining..];
                result.Append(thisLinePart).Append(newLine);
                lineIndex = 0;
            }
            if (word.Length > maxLineLength - lineIndex)
            {
                // The word is too big to fit on this line, so we put
                // it on the next one.
                result.Append(newLine).Append(word).Append(' ');
                lineIndex = word.Length + 1;
            }
            else
            {
                // Put the word here.
                result.Append(word).Append(' ');
                lineIndex += word.Length + 1;
            }
        }
        Console.WriteLine(result.AppendLine());
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
}

