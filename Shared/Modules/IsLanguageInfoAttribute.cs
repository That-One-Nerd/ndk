using System;

namespace NLang.DevelopmentKit.Shared.Modules;

public class IsLanguageInfoAttribute(string language, string langVersion, string libVersion)
    : ToolAttributeBase(langVersion, language, libVersion)
{
    public override Type? RequiredBase { get; } = typeof(LanguageInfoBase);
}
