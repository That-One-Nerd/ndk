using NLang.DevelopmentKit.Shared.Modules;
using System.Collections.Generic;
using System.Xml.Linq;

namespace NLang.DevelopmentKit.Shared.Projects;

public abstract class ProjectOutputInfoBase : IModuleTool
{
    string IModuleTool.Language => "";
    public abstract string Variant { get; }

    public abstract void FromProjectFile(XElement outputInfoRoot);
    public abstract IEnumerable<(string, string)> GetFormattedProperties();

    public static ProjectOutputInfoBase? Get(string variant) => ModuleLoader.Get<ProjectOutputInfoBase>(variant, "");
    static IModuleTool? IModuleTool.Get(string variant, string language) => ModuleLoader.Get<ProjectOutputInfoBase>(variant, language);
}
