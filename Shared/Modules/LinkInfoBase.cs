namespace NLang.DevelopmentKit.Shared.Modules;

public abstract class LinkInfoBase
{
    public abstract string Type { get; }

    public abstract (string, string) GetFormatted();
    public override string ToString()
    {
        (string type, string value) = GetFormatted();
        return $"\x1b[0m{type}\x1b[0;91m => \x1b[0m{value}\x1b[0m";
    }
}
