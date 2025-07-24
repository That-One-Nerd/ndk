using System;

namespace NLang.DevelopmentKit.Shared.Modules;

public class IsTemplateAttribute(string language, string name, string cliVersion) : ToolAttributeBase(name, language, cliVersion)
{
    public override Type? RequiredBase => typeof(TemplateBase);
}
