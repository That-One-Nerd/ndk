using NLang.DevelopmentKit.Shared;
using NLang.DevelopmentKit.Shared.Helpers;
using NLang.DevelopmentKit.Shared.Modules;
using System;
using System.Collections.Generic;

namespace NLang.DevelopmentKit.Hub;

public static class Program
{
    public static void Main(string[] argsStr)
    {
        Console.WriteLine($"\n\x1b[1;95m  N Language Suite {NDK.VersionStr}\x1b[0m\n");

        HubArguments args = HubArguments.Parse(argsStr);

        if (args.UnparsedArguments.Count > 0)
        {
            // Failed to parse an argument, reset.
            DisplayHelp();
            return;
        }
        else if (args.ParsedArguments.Contains("submodule"))
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

        // TODO: Print flags as a "about" category.
    }
    public static void DisplayCredits()
    {
        // TODO
    }
    public static void DisplayVersion()
    {
        // TODO
    }
}
