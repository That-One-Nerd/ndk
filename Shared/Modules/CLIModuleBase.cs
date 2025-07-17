namespace NLang.DevelopmentKit.Shared.Modules;

public abstract class CLIModuleBase : IModuleTool
{
    public abstract string Name { get; }
    string IModuleTool.Language => "";
    public abstract string Description { get; }

    public abstract void Run(string[] args);

    public static CLIModuleBase Get(string name) => ModuleLoader.Get<CLIModuleBase>(name, "");
    static IModuleTool IModuleTool.Get(string variant, string language) => ModuleLoader.Get<CLIModuleBase>(variant, language);
}
