using NLang.DevelopmentKit.Shared;
using NLang.DevelopmentKit.Shared.Helpers;
using NLang.DevelopmentKit.Shared.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

[assembly: AssemblyVersion(NDK.VersionStr)]
[assembly: AssemblyInformationalVersion(NDK.VersionFull)]

namespace NLang.DevelopmentKit.Start;

[IsSubsystem("start", NDK.VersionStr)]
public class Program : SubsystemBase
{
    public override string Name => "start";
    public override string Description => "Creates an NLang project of a specific language in the current directory.";
    public override string DetailedDescription => """
        Creates an NLang project of a specific language in the current directory.
        If a template is not provided, the first available template (typically the
        "console" template) is chosen.
        """;

    public override void Invoke(string[] argsStr)
    {
        StartArguments args = StartArguments.Parse(argsStr);

        if (!args.AnyArguments)
        {
            DisplayHelp();
            return;
        }

        if (args.PrintAnyIssues()) return;

        // TODO: Handle project creation.
        //       Look up templates.
    }

    public void DisplayHelp()
    {
        // Print description and usage.
        Console.WriteLine($"  \x1b[1;97mDescription:\x1b[0m\n    {DetailedDescription.Replace("\n", "\n    ")}\n");
        PrintHelper.PrintList("Usage", 2, [
            "\x1b[95mndk \x1b[93mstart \x1b[32m[language] \x1b[36m[template?]"
        ]);

        // Fetch languages and print their aliases.
        IEnumerable<LanguageInfoBase> langs = ModuleLoader.Get<LanguageInfoBase>();
        Dictionary<string, string> langDescs = [];
        foreach (LanguageInfoBase lang in langs)
        {
            if (lang.Aliases.Length == 0) continue;
            
            StringBuilder aliases = new();
            foreach (string alias in lang.Aliases) aliases.Append($"{alias}, ");
            aliases.Remove(aliases.Length - 2, 2);

            langDescs.Add(aliases.ToString(), $"Create an {lang.FullName} project.");
        }
        PrintHelper.PrintKeyValues("Languages", 2, langDescs);

        // Fetch templates, group them by those with the same name and description,
        // and print which languages work for them.
        IEnumerable<TemplateBase> templates = ModuleLoader.Get<TemplateBase>();
        Dictionary<(string name, string desc), List<string>> templateGroups = [];
        foreach (TemplateBase template in templates)
        {
            // Kind of crazy syntax, if I'm being honest. But it works!
            // No AI coding here, folks.
            if (!templateGroups.TryGetValue((template.Name, template.Description), out List<string>? workLangs))
            {
                workLangs = [];
                templateGroups.TryAdd((template.Name, template.Description), workLangs);
            }
            if (!workLangs.Contains(template.Language)) workLangs.Add(template.Language);
        }
        Dictionary<string, string> templateDescs = [];
        foreach (KeyValuePair<(string name, string desc), List<string>> template in templateGroups)
        {
            // If all known languages support this template, just list "all." Otherwise,
            // list each one (for example, "(csharp, cpp)").
            StringBuilder nameWithLangs = new(template.Key.name);
            if (langs.Any(x => !template.Value.Contains(x.Identifier)))
            {
                nameWithLangs.Append(" (");
                foreach (string lang in template.Value) nameWithLangs.Append($"{lang}, ");
                nameWithLangs.Remove(nameWithLangs.Length - 2, 2);
                nameWithLangs.Append(')');
            }
            else nameWithLangs.Append(" (all)");
            templateDescs.Add(nameWithLangs.ToString(), template.Key.desc);
        }
        PrintHelper.PrintKeyValues("Templates", 2, templateDescs);

        PrintHelper.PrintList("Examples", 2, [
            "\x1b[95mndk \x1b[93mstart \x1b[32mcsharp",
            "\x1b[95mndk \x1b[93mstart \x1b[32mcpp \x1b[36mlibrary"
        ]);
    }
}
