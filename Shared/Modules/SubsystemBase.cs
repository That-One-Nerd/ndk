namespace NLang.DevelopmentKit.Shared.Modules;

public abstract class SubsystemBase : IModuleTool
{
    public abstract string Name { get; }
    string IModuleTool.Language => "";
    public abstract string Description { get; }
    public virtual string DetailedDescription { get; }
    string IModuleTool.Variant => Name;

    public SubsystemBase()
    {
        DetailedDescription ??= Description;
    }

    public abstract void Invoke(string[] args);

    public static SubsystemBase? Get(string name) => ModuleLoader.Get<SubsystemBase>(name, "");
    static IModuleTool? IModuleTool.Get(string variant, string language) => ModuleLoader.Get<SubsystemBase>(variant, language);
}
