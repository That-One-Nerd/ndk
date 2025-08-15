using NLang.DevelopmentKit.Shared;
using NLang.DevelopmentKit.Shared.Helpers;
using NLang.DevelopmentKit.Shared.Modules;
using NLang.DevelopmentKit.Shared.Projects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

    private static readonly string FormatDirectory = "\x1b[1;93m";
    private static readonly string FormatProjectDirectory = "\x1b[1;97;105m";
    private static readonly string FormatProjectFile = "\x1b[95m";
    private static readonly string FormatIgnoredDirectory = "\x1b[1;3;90m";
    private static readonly string FormatIgnoredFile = "\x1b[90m";
    private static readonly string FormatLanguageFile = "\x1b[32m";

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
        using LoadingBar loading = new(LoadingBarColor.Blue)
        {
            FailureText = "Failed to read project file",
            FinalText = "Reading Project",
            Text = "Reading Project",
            PartsTotal = 2
        };
        ProjectContext? project = ReadProject(args, loading);
        if (project is null) return;

        LanguageInfoBase? lang = LanguageInfoBase.Get(project.General.Language, project.General.Version);
        if (lang is null)
        {
            loading.Failures.Add(new($"Missing language information for {project.General.Version}. Perhaps you're missing a language pack?"));
            loading.Dispose();
            return;
        }

        loading.PartsDone++;
        loading.Text = "Reading Directory";

        string dir = Path.GetDirectoryName(project.ProjectPath)!;
        TreeNode<string> tree = TreeFromDirectory(dir, lang);

        static TreeNode<string> TreeFromDirectory(string dir, LanguageInfoBase lang, EnumerationOptions? skipInvalid = null)
        {
            string name = Path.GetFileName(dir);
            TreeNode<string> result = new($"{name}/");
            if (name.StartsWith('.') || name == "bin" || name == "obj")
            {
                result.ValueFormat = FormatIgnoredDirectory;
                return result;
            }
            else result.ValueFormat = FormatDirectory;

            skipInvalid ??= new() { IgnoreInaccessible = true };
            foreach (string subDir in Directory.EnumerateDirectories(dir, "*", skipInvalid))
                result.Children.Add(TreeFromDirectory(subDir, lang, skipInvalid));
            foreach (string file in Directory.EnumerateFiles(dir, "*.*", skipInvalid))
            {
                TreeNode<string> fileNode = new(Path.GetFileName(file));
                if (file.EndsWith(".nproj"))
                {
                    fileNode.ValueFormat = FormatProjectFile;
                    result.Value = $" {result.Value} ";
                    result.ValueFormat = FormatProjectDirectory;
                }
                else if (lang.FileExtensions.Any(file.EndsWith)) fileNode.ValueFormat = FormatLanguageFile;
                else fileNode.ValueFormat = FormatIgnoredFile;
                result.Add(fileNode);
            }
            return result;
        }

        int folders = 0, files = 0;
        foreach (TreeNode<string> node in tree.Recurse())
        {
            // Not a super good system. Maybe I could encode
            // metadata as an enum or something?
            if (node.Children.Count > 0)
            {
                if (node.ValueFormat == FormatProjectDirectory ||
                    node.ValueFormat == FormatDirectory)
                    folders++;
            }
            else
            {
                if (node.ValueFormat == FormatProjectFile ||
                    node.ValueFormat == FormatLanguageFile)
                    files++;
            }
        }

        // Dude sometimes formatting strings results in the WORST code imaginable.
        // And I tend to write a lot of formatting code.
        StringBuilder result = new();
        result.AppendLine().Append(tree.ToString(new() { lineFormat = "\x1b[90m" }));
        if (folders > 0) result.Append($"\n  \x1b[1;97m{folders}\x1b[0m important {FormatDirectory}{(folders == 1 ? "folder" : "folders")}\x1b[0m");
        if (files > 0)
        {
            if (folders > 0) result.Append(", ");
            else result.Append("\n  ");
            result.Append($"\x1b[1;97m{files}\x1b[0m important {FormatLanguageFile}{(files == 1 ? "file" : "files")}\x1b[0m");
        }
        if (folders != 0 || files != 0) result.AppendLine();

        loading.Dispose();
        Console.WriteLine(result);
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
            FailureText = "Failed to read project file",
            FinalText = "Reading Project",
            Text = "Reading Project",
            PartsTotal = 1
        };

        ProjectContext? result;
        try
        {
            if (args.file is null)
            {
                result = ProjectContext.FromDirectoryOrParent(Directory.GetCurrentDirectory());
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
            loading.Failures.Add(ex);
            loading.Dispose();
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
