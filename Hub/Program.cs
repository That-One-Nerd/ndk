using NLang.DevelopmentKit.Shared;
using NLang.DevelopmentKit.Shared.Helpers;
using NLang.DevelopmentKit.Shared.Modules;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

[assembly: AssemblyVersion(NDK.VersionStr)]
[assembly: AssemblyInformationalVersion(NDK.VersionFull)]

namespace NLang.DevelopmentKit.Hub;

public static class Program
{
    public static void Main(string[] argsStr)
    {
        Console.WriteLine($"\n\x1b[1;95m  N Language Suite {NDK.VersionFull}\x1b[0m\n");

        HubArguments args = HubArguments.Parse(argsStr);

        if (args.UnparsedArguments.Count > 0)
        {
            // Failed to parse an argument, reset.
            DisplayHelp();
            return;
        }
        else if (args.ParsedArguments.Contains("subsystem"))
        {
            // Invoke this submodule.
            SubsystemBase? subsystem = SubsystemBase.Get(args.subsystem);
            if (subsystem is null)
            {
                Console.WriteLine($"\x1b[3;91m  Subsystem not found: \"{args.subsystem}\"\n" +
                                  $"  Run \x1b[95mndk \x1b[90m--help\x1b[91m for a list " +
                                   "of available subsystems.\x1b[0m\n");
                return;
            }
            subsystem.Invoke(args.subsystemArgs);
        }
        else
        {
            // Run hub stuff, display things.
            if (args.showHelp || !args.AnyArguments) DisplayHelp();
            if (args.showCredits) DisplayCredits();
            if (args.showVersion) DisplayVersion();
        }
    }

    public static void DisplayHelp()
    {
        PrintHelper.PrintList("Usage", 2,
        [
            $"\x1b[95mndk \x1b[93m<subsystem> <command> \x1b[36m[arguments?]",
            $"\x1b[95mndk \x1b[90m[flags]\x1b[0m"
        ]);

        // Display commands.
        Dictionary<string, string> subsystemDesc = [];
        foreach (SubsystemBase subsystem in ModuleLoader.Get<SubsystemBase>())
        {
            subsystemDesc.Add(subsystem.Name, subsystem.Description);
        }
        PrintHelper.PrintKeyValues("Subsystems", 2, subsystemDesc, keyFormat: "\x1b[93m");
        HubArguments.PrintCategory("About");
    }
    public static void DisplayCredits()
    {
        PrintHelper.PrintKeyValues("External Links", 2, [
            ("Repository", NDK.RepositoryUrl)
        ], separatorFormat: "", valueFormat: "\x1b[3;32m");

        using LoadingBar creditLoader = new(LoadingBarColor.Blue)
        {
            FinalText = "Fetched Contributors",
            Text = "Fetching GitHub Contributors"
        };
        string[]? contributors = NDK.GetContributors().GetAwaiter().GetResult();
        if (contributors is null)
        {
            // Failed to load contributors.
            creditLoader.Failed = true;
            creditLoader.FinalText = "Failed to Load Contributors";
            creditLoader.Dispose();
            Console.WriteLine();
            return;
        }
        creditLoader.Dispose();

        // This used to be here. But our loading message is basically the header now.
        // If we want it back, it's here:
        //Console.WriteLine("  \x1b[1;97mContributors:\x1b[22m");
        StringBuilder result = new();

        int maxLength = 0;
        for (int i = 0; i < contributors.Length; i++)
        {
            string person = contributors[i];
            if (person.Length > maxLength) maxLength = person.Length;
        }
        int spacing = maxLength + 2, indent = 4;

        int totalSpace = Console.WindowWidth - indent - 1,
            columns = int.Max(1, totalSpace / spacing);

        int rows = 0;
        for (int p = 0; p < contributors.Length; p++, rows++)
        {
            result.Append(new string(' ', indent));
            for (int c = 0; c < columns && p < contributors.Length; c++, p++)
            {
                string person = contributors[p];
                int extra = spacing - person.Length;

                if ((c + rows) % 2 == 0) result.Append("\x1b[36m");
                else result.Append("\x1b[94m");
                result.Append(person);
                result.Append(new string(' ', extra));
            }
            result.AppendLine();
        }
        result.Append("\x1b[0m");
        Console.WriteLine(result);

    }
    public static void DisplayVersion()
    {
        // TODO: Is it possible to encode variables at compile time?
        //       Like automatically set the date of release as the compilation
        //       date without manually setting it?
        PrintHelper.PrintKeyValues("Version", 2, [
            ("Full Version", $"\x1b[95mv{NDK.VersionFull}"),
            ("Is Prerelease", NDK.IsPrerelease ? "\x1b[33mYes" : "\x1b[94mNo")
            //("Date of Release", "\x1b[94m???")
        ], separatorFormat: "", valueFormat: "");
    }
}
