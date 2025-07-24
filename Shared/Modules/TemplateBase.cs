using System.IO;

namespace NLang.DevelopmentKit.Shared.Modules;

public abstract class TemplateBase : IModuleTool
{
    public abstract Stream DataStream { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Language { get; }

    public static TemplateBase? Get(string language, string name) => ModuleLoader.Get<TemplateBase>(name, language);
    static IModuleTool? IModuleTool.Get(string variant, string language) => ModuleLoader.Get<TemplateBase>(variant, language);
}
