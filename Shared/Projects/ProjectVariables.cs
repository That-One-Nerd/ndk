using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NLang.DevelopmentKit.Shared.Projects;

public class ProjectVariables : IEnumerable<KeyValuePair<string, string>>
{
    public required string ProjectName
    {
        get => variables[nameof(ProjectName)];
        set => variables[nameof(ProjectName)] = value;
    }
    public string NdkVersion
    {
        get => variables[nameof(NdkVersion)];
        set => variables[nameof(NdkVersion)] = value;
    }

    private readonly Dictionary<string, string> variables = new()
    {
        { nameof(NdkVersion), NDK.VersionStr }
    };

    public ProjectVariables() { }

    public string? this[string variable, bool recursive = true]
    {
        get
        {
            if (variables.TryGetValue(variable, out string? result))
            {
                if (recursive)
                {
                    List<string> prevNames = [variable];
                    return FillValues(result, ref prevNames);
                }
                else return result;
            }
            else return null;
        }
        set
        {
            if (value is null) variables.Remove(variable);
            else if (!variables.TryAdd(variable, value))
            {
                if (recursive)
                {
                    List<string> prevNames = [variable];
                    variables[variable] = FillValues(value, ref prevNames);
                }
                else variables[variable] = value;
            }
        }
    }
    public bool KnownVariable(string variable) => variables.ContainsKey(variable);

    public void FromProjectFile(XElement varInfoRoot)
    {
        foreach (XElement varInfo in varInfoRoot.Elements())
        {
            if (varInfo.Name != "Variable") continue;

            XAttribute? name = varInfo.Attribute("name");
            if (name is null) continue;

            this[name.Value] = varInfo.Value;
        }
    }
    public void FromProjectFile(XDocument project)
    {
        XElement? varInfoRoot = project.Root?.Element("VariableInfo");
        if (varInfoRoot is not null) FromProjectFile(varInfoRoot);
    }

    public string FillValues(string input)
    {
        List<string> prevNames = [];
        return FillValues(input, ref prevNames);
    }
    private string FillValues(string input, ref List<string> prevNames)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        int varIndex = 0;
        while (varIndex < input.Length && (varIndex = input.IndexOf("${", varIndex)) != -1)
        {
            int endVarIndex = input.IndexOf('}', varIndex);
            string varName = input[(varIndex + 2)..endVarIndex];
            string? varValue = this[varName, false];

            if (varValue is null)
            {
                varIndex = endVarIndex + 1;
                continue;
            }
            else if (prevNames.Count == 0 || !varValue.Contains(prevNames[0]))
            {
                prevNames.Add(varName);
                varValue = FillValues(varValue, ref prevNames);
                prevNames.Remove(varName);
            }
            input = input[..varIndex] + varValue + input[(endVarIndex + 1)..];
            varIndex = endVarIndex + 1;
        }
        return input;
    }
    public void FillValues(Stream input, Stream output)
    {
        using StreamReader reader = new(input, leaveOpen: false);
        using StreamWriter writer = new(output, reader.CurrentEncoding, leaveOpen: false);

        string? line;
        while ((line = reader.ReadLine()) is not null) writer.WriteLine(FillValues(line));
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => variables.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => variables.GetEnumerator();

    public IEnumerable<(string, string)> GetFormattedVariables() =>
        from kv in this
        select (kv.Key, $"{GetFormatFor(kv.Key)}{kv.Value}");
    public static string GetFormatFor(string key) => key switch
    {
        nameof(NdkVersion) => "\x1b[1;95m",
        _ => ""
    };
}
