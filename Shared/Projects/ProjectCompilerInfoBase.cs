using NLang.DevelopmentKit.Shared.Modules;
using System.Xml.Linq;

namespace NLang.DevelopmentKit.Shared.Projects;

public abstract class ProjectCompilerInfoBase : IModuleTool
{
    public abstract string Language { get; }
    public abstract string Variant { get; }

    public abstract void FromProjectFile(XElement compilerInfoRoot);

    public static ProjectCompilerInfoBase? Get(string variant, string language) => ModuleLoader.Get<ProjectCompilerInfoBase>(variant, language);
    static IModuleTool? IModuleTool.Get(string variant, string language) => ModuleLoader.Get<ProjectCompilerInfoBase>(variant, language);
}
