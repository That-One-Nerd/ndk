namespace NLang.DevelopmentKit.Shared.Modules;

public interface IModuleTool
{
    public string Name { get; }
    public string Language { get; }

    public static virtual IModuleTool Get(string variant, string language) => ModuleLoader.Get<IModuleTool>(variant, language);
}
