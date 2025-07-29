using NLang.DevelopmentKit.Shared.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace NLang.DevelopmentKit.Shared.Projects;

public class ProjectContext
{
    public required string ProjectName { get; init; }
    public required string ProjectPath { get; init; }
    public required ProjectGeneralInfo General { get; init; }
    public required ProjectCompilerInfoBase Compiler { get; init; }
    public required ProjectLinkerInfo Linker { get; init; }
    public required ProjectOutputInfoBase Output { get; init; }
    public required ProjectVariables Variables { get; init; }

    internal ProjectContext() { }

    public static ProjectContext? FromDirectory(string dir, bool deep)
    {
        EnumerationOptions skipInvalid = new() { IgnoreInaccessible = true };
        ProjectContext? result = null;
        foreach (string file in Directory.EnumerateFiles(dir, "*.nproj", skipInvalid))
        {
            if (result is null) result = FromFile(file);
            else throw new("Multiple projects exist in the same root directory!");
        }

        if (deep && result is null)
        {
            foreach (string subDir in Directory.EnumerateDirectories(dir, "*", skipInvalid))
            {
                ProjectContext? subResult = FromDirectory(subDir, deep);
                if (result is null) result = subResult;
                else if (subResult is not null) throw new($"Multiple folders in \"{dir}\" have projects. It is ambiguous which one to pick.");
            }
        }

        return result;
    }
    public static ProjectContext FromFile(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        using FileStream fs = new(path, FileMode.Open, FileAccess.Read);

        XDocument doc = XDocument.Load(fs);
        XElement root = doc.Root!;
        if (root.Name != "Project") throw new("Root element must be a <Project> element.");

        List<Exception> exceptions = [];
        ProjectGeneralInfo generalInfo = ProjectGeneralInfo.FromProjectFile(root);
        ProjectCompilerInfoBase compilerInfo = null!;
        ProjectLinkerInfo? linkerInfo = null;
        ProjectOutputInfoBase outputInfo = null!;
        ProjectVariables varInfo = new()
        {
            ProjectName = name,
            NdkVersion = NDK.VersionStr
        };

        XElement? section;
        if ((section = root.Element("CompilerInfo")) is not null)
        {
            XElement? variant = section.Element("Variant");
            if (variant is not null)
            {
                string variantStr = variant.Value;
                ProjectCompilerInfoBase? possible = ProjectCompilerInfoBase.Get(variantStr, generalInfo.Language);
                if (possible is not null)
                {
                    try
                    {
                        possible.FromProjectFile(section);
                        compilerInfo = possible;
                    }
                    catch (Exception ex)
                    {
                        compilerInfo = null!;
                        exceptions.Add(ex);
                    }
                }
                else exceptions.Add(new($"Unknown compiler variant specified in the project file: \"{variantStr}\""));
            }
            else exceptions.Add(new("The <CompilerInfo> element must contain a child element <Variant>."));
        }
        else exceptions.Add(new("Missing required <CompilerInfo> element."));
        if ((section = root.Element("LinkerInfo")) is not null)
        {
            try { linkerInfo = ProjectLinkerInfo.FromProjectFile(section); }
            catch (Exception ex)
            {
                linkerInfo = null;
                exceptions.Add(ex);
            }
        }
        if ((section = root.Element("OutputInfo")) is not null)
        {
            XElement? variant = section.Element("Variant");
            if (variant is not null)
            {
                string variantStr = variant.Value;
                ProjectOutputInfoBase? possible = ProjectOutputInfoBase.Get(variantStr);
                if (possible is not null)
                {
                    try
                    {
                        possible.FromProjectFile(section);
                        outputInfo = possible;
                    }
                    catch (Exception ex)
                    {
                        outputInfo = null!;
                        exceptions.Add(ex);
                    }
                }
                else exceptions.Add(new($"Unknown output variant specified in the project file: \"{variantStr}\""));
            }
            else exceptions.Add(new("The <OutputInfo> element must contain a child element <Variant>."));
        }
        else exceptions.Add(new("Missing required <OutputInfo> element."));
        if ((section = root.Element("VariableInfo")) is not null)
        {
            try { varInfo.FromProjectFile(section); }
            catch (Exception ex) { exceptions.Add(ex); }
        }

        if (exceptions.Count > 0) throw new AggregateException("One or more errors occurred while parsing the project file.", exceptions);
        else return new()
        {
            ProjectName = name,
            ProjectPath = Path.GetFullPath(path),
            General = generalInfo,
            Compiler = compilerInfo,
            Linker = linkerInfo ?? ProjectLinkerInfo.Default,
            Output = outputInfo,
            Variables = varInfo
        };
    }
}
