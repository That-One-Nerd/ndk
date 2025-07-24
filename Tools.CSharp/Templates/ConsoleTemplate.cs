using NLang.DevelopmentKit.Shared;
using NLang.DevelopmentKit.Shared.Modules;
using System.IO;
using System.Resources;

namespace NLang.DevelopmentKit.Tools.CSharp.Templates;

[IsTemplate("csharp", "console", NDK.VersionStr)]
public class ConsoleTemplate : TemplateBase
{
    public override string Name => "console";
    public override string Language => "csharp";
    public override string Description => "Creates a simple console application.";
    public override Stream DataStream { get; }

    public ConsoleTemplate()
    {
        ResourceManager res = new("NLang.DevelopmentKit.Tools.CSharp.Templates.Templates", typeof(ConsoleTemplate).Assembly);
        DataStream = new MemoryStream((byte[])res.GetObject("Console")!);
    }
}
