using NLang.DevelopmentKit.Shared.Projects;
using System;

namespace NLang.DevelopmentKit.Shared.Modules;

public class IsCompilerInfoAttribute(string variant, string language, string version) : ToolAttributeBase(variant, language, version)
{
    public override Type? RequiredBase => typeof(ProjectCompilerInfoBase);
}
