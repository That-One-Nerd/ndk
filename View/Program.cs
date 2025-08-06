using NLang.DevelopmentKit.Shared;
using NLang.DevelopmentKit.Shared.Helpers;
using NLang.DevelopmentKit.Shared.Modules;
using NLang.DevelopmentKit.Shared.Projects;
using System;
using System.Collections.Generic;
using System.IO;

namespace NLang.DevelopmentKit.View;

[IsSubsystem("view", NDK.VersionStr)]
public class Program : SubsystemBase
{
    public override string Name => "view";
    public override string Description => "View information about an NLang project or file.";

    public override void Invoke(string[] argsStr)
    {
        ViewArguments args = ViewArguments.Parse(argsStr);

        if (!args.AnyArguments)
        {
            DisplayHelp();
            return;
        }

        if (args.PrintAnyIssues(FormatArguments)) return;

        switch (args.option)
        {
            case ViewOption.Properties: DisplayProperties(args); break;
            case ViewOption.Directory: DisplayDirectory(args); break;
            case ViewOption.Outline: DisplayOutline(args); break;
        }
    }

    private void DisplayHelp()
    {
        PrintHelper.PrintLargeString("Description", 2, DetailedDescription);
        PrintHelper.PrintList("Usage", 2, [
            "\x1b[95mndk \x1b[93mview \x1b[96m[option] \x1b[36m[arguments?]"
        ], valueFormat: "");
        PrintHelper.PrintEnum<ViewOption>("Options", 2, "\x1b[96m");
        ViewArguments.PrintCategory("Arguments");
        PrintHelper.PrintList("Examples", 2, [
            "\x1b[95mndk \x1b[93mview \x1b[96moutline",
            "\x1b[95mndk \x1b[93mview \x1b[96mproperties \x1b[36m-file:Output.nlib",
            "\x1b[95mndk \x1b[93mview \x1b[96mdirectory  \x1b[36m-file:\"C:\\Project\\Project.nproj\""
        ]);
    }
    private static void DisplayProperties(ViewArguments args)
    {
        ProjectContext? project = ReadProject(args);
        if (project is null) return;

        Console.WriteLine();
        project.PrintInfo();
    }
    private static void DisplayDirectory(ViewArguments args)
    {
        ReadProject(args);
    }
    private static void DisplayOutline(ViewArguments args)
    {
        ReadProject(args);
    }

    private static ProjectContext? ReadProject(ViewArguments args, LoadingBar? loading = null)
    {
        bool customLoading = loading is null;
        loading ??= new(LoadingBarColor.Blue)
        {
            FinalText = "Reading Project",
            Text = "Reading Project",
            PartsDone = 1
        };

        ProjectContext? result;
        try
        {
            if (args.file is null)
            {
                result = ProjectContext.FromDirectory(Directory.GetCurrentDirectory(), false);
                if (result is null) throw new("Cannot auto-detect project, no project file found in current directory.");
            }
            else
            {
                if (!File.Exists(args.file)) throw new("Specified file path does not exist.");
                result = ProjectContext.FromFile(args.file);
            }
        }
        catch (Exception ex)
        {
            result = null;
            loading.Failed = true;
            loading.Dispose();

            IEnumerable<Exception> toPrint;
            if (ex is AggregateException agx) toPrint = agx.Flatten().InnerExceptions;
            else toPrint = [ex];

            Console.WriteLine("\n  \x1b[1;91mFailed to read project file:\x1b[0m");
            foreach (Exception subEx in toPrint) PrintHelper.PrintLargeString(null, 2, subEx.Message, "\x1b[91m");
        }

        if (result is not null)
        {
            loading.PartsDone++;
            if (customLoading) loading.Dispose();
        }
        return result;
    }

    private static string? FormatArguments(string name) => name switch
    {
        "option" => "\x1b[96m",
        "-file" => "\x1b[36m",
        _ => null
    };
}
