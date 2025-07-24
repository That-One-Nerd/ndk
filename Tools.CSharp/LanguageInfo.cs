using NLang.DevelopmentKit.Shared;
using NLang.DevelopmentKit.Shared.Modules;

namespace NLang.DevelopmentKit.Tools.CSharp;

[IsLanguageInfo("csharp", "c#12", NDK.VersionStr)]
public class LanguageInfo : LanguageInfoBase
{
    public override string Identifier => "csharp";
    public override string FullName => "NC#";
    public override string LanguageVersion => "c#12";
    public override string[] Aliases => ["csharp", "cs"];
    public override string[] FileExtensions => [".cs"];
}
