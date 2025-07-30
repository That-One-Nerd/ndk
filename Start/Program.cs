using NLang.DevelopmentKit.Shared;
using NLang.DevelopmentKit.Shared.Helpers;
using NLang.DevelopmentKit.Shared.Modules;
using NLang.DevelopmentKit.Shared.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

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

    // File or directory names that are allowed to exist alongside
    // the project without a warning. The main example of a valid
    // directory is the .git/ directory, since it only signifies
    // that git is present in this project.
    private static readonly string[] ValidExtraNames = [
        ".git",
        ".vs",
    ];

    public override void Invoke(string[] argsStr)
    {
        StartArguments args = StartArguments.Parse(argsStr);

        if (!args.AnyArguments)
        {
            DisplayHelp();
            return;
        }

        if (args.PrintAnyIssues()) return;

        CreateProject(args);
    }

    private static void CreateProject(StartArguments args)
    {
        LanguageInfoBase? lang = LanguageInfoBase.GetHighest(args.language);
        if (lang is null)
        {
            Console.WriteLine($"  \x1b[3;91mUnknown language \"\x1b[32m{args.language}\x1b[91m.\" Either this language is not supported\n" +
                               "    or the module defining this language is not installed.\n" +
                               "  Run \x1b[23;95mndk \x1b[93mstart \x1b[3;91mfor a list of known languages.\x1b[0m\n");
            return;
        }

        TemplateBase? template;
        if (args.template is not null) template = TemplateBase.Get(args.language, args.template);
        else template = TemplateBase.GetFirst(args.language);

        if (template is null)
        {
            if (args.template is null)
            {
                Console.WriteLine($"  \x1b[3;91mThe language \"\x1b[32m{args.language}\x1b[91m\" does not appear to have any valid templates!\n" +
                                   "  Is it possible you forgot to install the language module?\n" +
                                   "  Run \x1b[23;95mndk \x1b[93mstart \x1b[3;91mfor a list of available templates.\x1b[0m\n");
            }
            else
            {
                Console.WriteLine($"  \x1b[3;91mThe template \"\x1b[36m{args.template}\x1b[91m\" could not be found for the language \"\x1b[32m{args.language}\x1b[91m\".\n" +
                                   "  The module defining this template may not have been installed.\n" +
                                  $"  Run \x1b[23;95mndk \x1b[93mstart \x1b[3;91mfor a list of available templates.\x1b[0m\n");
            }
            return;
        }

        string projectDir = args.rootDirectory ?? Directory.GetCurrentDirectory();
        string projectName = Path.GetFileName(projectDir);

        Console.WriteLine("  \x1b[1;94mCreating New Project\x1b[0m");
        PrintHelper.PrintKeyValues("Properties", 2, new()
        {
            { "Project Name", $"\x1b[1;97;44m {projectName} " },
            { "Language",     $"\x1b[1;32m{lang.FullName}\x1b[22;90m (\x1b[92m{lang.LanguageVersion}\x1b[23;90m)" },
            { "Template",     $"\x1b[1;36m{template.Name}" },
            { "Directory",    $"\x1b[33m{projectDir}" },
            { "Project File", $"\x1b[1;93m{projectName}.nproj" },
            { "NDK Version",  $"\x1b[1;95m{NDK.VersionStr}" }
        }, valueFormat: "");

        if (InDirectoryWithContents(projectDir, out bool isAlreadyProject))
        {
            if (isAlreadyProject)
            {
                Console.WriteLine("  \x1b[3;91mThere is an existing project in this directory.\n" +
                                  "  Please remove that project before creating a new one.\x1b[0m\n");
                return;
            }
            else
            {
                Console.Write("  \x1b[3;33mThere are already contents in this directory.\n" +
                              "  Are you sure you want to continue? \x1b[0m");
                if (!PrintHelper.GetYesNoQuery())
                {
                    Console.WriteLine("\n  \x1b[3;91mOperation cancelled by user.\x1b[0m\n");
                    return;
                }
                else Console.WriteLine();
            }
        }

        using LoadingBar loading = new(LoadingBarColor.Blue)
        {
            FinalText = "Preparing Project",
            Text = "Extracting Template"
        };

        switch (template.Format)
        {
            case TemplateFormat.Zip: if (ExtractZipTemplate()) break; else return;
            default:
                loading.Failed = true;
                loading.Dispose();

                Console.WriteLine("\n  \x1b[3;91mThis template is encoded in an unknown format.\n" +
                                  "  Either the template is corrupt (and maybe the module itself), or\n" +
                                 $"    you are using an old version of the ndk.start module (this is version \x1b[23;95m{NDK.VersionStr}\x1b[3;91m).\x1b[0m\n");
                break;
        }

        bool ExtractZipTemplate()
        {
            using ZipArchive zip = new(template.DataStream);
            loading.PartsTotal = zip.Entries.Count(x => !string.IsNullOrEmpty(x.Name));
            loading.PartsDone = 0;

            ProjectVariables projVars = new()
            {
                ProjectName = projectName
            };

            ZipArchiveEntry projectEntry = zip.Entries.Single(x => x.Name.EndsWith(".nproj"));
            if (projectEntry is not null)
            {
                // Read project information for environment variables.
                // We don't need to fully serialize the project by this point,
                // just read the variableinfo part.
                using Stream projectStream = projectEntry.Open();
                try
                {
                    XDocument projectDoc = XDocument.Load(projectStream);
                    projVars.FromProjectFile(projectDoc);
                }
                catch { }
            }

            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    // Creating a folder.
                    string dir = Path.Combine(projectDir, entry.ToString());
                    Directory.CreateDirectory(dir);
                }
                else
                {
                    // Creating a file. Double check the folder exists.
                    // As far as I know, a folder creation entry will ALWAYS
                    // precede any files in that folder, but I may be wrong.
                    string entryName = projVars.FillValues(entry.Name),
                           entryStr = projVars.FillValues(entry.ToString());
                    loading.Text = $"Extracting {entryName}...";

                    string dir = Path.Combine(projectDir, Path.GetDirectoryName(entryStr)!);
                    Directory.CreateDirectory(dir);

                    string filePath = Path.Combine(projectDir, entryStr);
                    if (File.Exists(filePath)) File.Delete(filePath);

                    using Stream inFile = entry.Open();
                    using FileStream outFile = new(filePath, FileMode.CreateNew, FileAccess.Write);
                    projVars.FillValues(inFile, outFile);
                    loading.PartsDone++;
                }
            }

            return true;
        }

        loading.Dispose();
        Console.WriteLine();
    }

    private static bool InDirectoryWithContents(string path, out bool isAlreadyProject)
    {
        isAlreadyProject = false;
        IEnumerable<string> entries = Directory.EnumerateFileSystemEntries(path);
        foreach (string entry in entries)
        {
            string name = Path.GetFileName(entry);
            if (ValidExtraNames.Contains(name)) continue;
            else if (name.EndsWith(".nproj")) isAlreadyProject = true;
            return true;
        }
        return false;
    }

    public void DisplayHelp()
    {
        // Print description and usage.
        PrintHelper.PrintLargeString("Description", 2, DetailedDescription);
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
            if (!templateGroups.TryGetValue((template.Identifier, template.Description), out List<string>? workLangs))
            {
                workLangs = [];
                templateGroups.TryAdd((template.Identifier, template.Description), workLangs);
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
