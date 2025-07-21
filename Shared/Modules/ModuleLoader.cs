using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NLang.DevelopmentKit.Shared.Modules;

/// <summary>
/// Dynamically loads types and their respective tools.
/// </summary>
public static class ModuleLoader
{
    // All attribute types considered important to load.
    // TODO: We should also allow the creation of custom module attributes,
    //       but we'll likely need to make this a 2-step loader for that.
    private static readonly Assembly sharedLib = Assembly.GetExecutingAssembly();
    static ModuleLoader() => DetectAssemblies();

    public static void DetectAssemblies()
    {
        // Search for DLL files to load. Keep in mind this INCLUDES the sharedlib.
        string directory = Path.GetDirectoryName(sharedLib.Location)!;
        string[] files = Directory.GetFiles(directory, "*.dll");
        foreach (string file in files)
        {
            try
            {
                Assembly fileAsm = Assembly.LoadFrom(file);
                RegisterAssembly(fileAsm);
            }
            catch { } // Some error loading the assembly, most likely because it's a DLL but not a .NET assembly.
        }
    }

    private static readonly List<ModuleToolInfo> registeredTools = [];
    private static readonly Dictionary<ModuleToolInfo, object> toolInstances = [];

    public static void RegisterAssembly(Assembly assembly)
    {
        // Check for any types that derive from `IModuleTool`, and of those, break them up based
        // on their toolattributes.

        Type[] types = assembly.GetTypes();
        foreach (Type type in types)
        {
            if (type.GetInterface("NLang.DevelopmentKit.Shared.Modules.IModuleTool") is null) continue; // Not a module tool.

            IEnumerable<ToolAttributeBase> attributes = type.GetCustomAttributes<ToolAttributeBase>();
            foreach (ToolAttributeBase att in attributes)
            {
                if (att.RequiredBase is not null)
                {
                    // Double check that the base is what's required.
                    if (type.BaseType != att.RequiredBase) continue;
                }

                // If the major part of the version is not the same, this is incompatible.
                Version ver = Version.Parse(att.VersionStr);
                if (NDK.Version.Major != ver.Major ||
                    NDK.Version.Minor != ver.Minor) continue;

                ModuleToolInfo thisTool = new()
                {
                    AttributeInfo = att,
                    BaseType = att.RequiredBase ?? typeof(IModuleTool),
                    Language = att.Language,
                    Variant = att.Variant,
                    Type = type,
                    Version = ver
                };

                // Check for duplicates.
                ModuleToolInfo? otherTool = registeredTools.FirstOrDefault(x =>
                    x.Variant == thisTool.Variant &&
                    x.Language == thisTool.Language &&
                    x.AttributeInfo.GetType() == att.GetType());

                if (otherTool is null)
                {
                    registeredTools.Add(thisTool);
                }
                else
                {
                    // Compare versions. Identical = discard new, otherwise pick higher version.
                    if (otherTool.Version < thisTool.Version)
                    {
                        registeredTools.Remove(otherTool);
                        registeredTools.Add(thisTool);
                    }
                }
            }
        }
    }

    private static IModuleTool Get(ModuleToolInfo toolInfo)
    {
        if (!toolInstances.TryGetValue(toolInfo, out object? instance))
        {
            instance = Activator.CreateInstance(toolInfo.Type)!;
            toolInstances.Add(toolInfo, instance);
        }
        return (IModuleTool)instance;
    }
    public static IEnumerable<IModuleTool> Get(Func<ModuleToolInfo, bool> predicate)
    {
        foreach (ModuleToolInfo toolInfo in registeredTools.Where(predicate)) yield return Get(toolInfo);
    }
    public static TTool? Get<TTool>(string variant, string language) where TTool : IModuleTool
    {
        Type toolType = typeof(TTool);
        ModuleToolInfo? toolInfo = registeredTools.FirstOrDefault(x => x.Variant == variant &&
                                                                       x.Language == language &&
                                                                      (x.BaseType == toolType ||
                                                                       x.BaseType == toolType.BaseType ||
                                                                       toolType == typeof(IModuleTool) ||
                                                                       x.Type == toolType));
        if (toolInfo is null) return default;
        else return (TTool)Get(toolInfo);
    }
    public static IEnumerable<TTool> Get<TTool>() where TTool : IModuleTool
    {
        Type toolType = typeof(TTool);
        IEnumerable<ModuleToolInfo> matches = from x in registeredTools
                                              where x.Type == toolType ||
                                                    x.BaseType == toolType ||
                                                    x.BaseType == toolType.BaseType ||
                                                    toolType == typeof(IModuleTool)
                                              select x;
        foreach (ModuleToolInfo toolInfo in matches) yield return (TTool)Get(toolInfo);
    }
}
