using System.Xml.Linq;

namespace NLang.DevelopmentKit.Shared.Modules;

public abstract class LinkerInfoBase : IModuleTool
{
    public abstract string Variant { get; }
    string IModuleTool.Language => "";

    public abstract LinkInfoBase ParseReference(XElement refNode);

    public static LinkerInfoBase? Get(string variant) => ModuleLoader.Get<LinkerInfoBase>(variant, "");
    static IModuleTool? IModuleTool.Get(string variant, string language) => ModuleLoader.Get<LinkerInfoBase>(variant, language);
}
