using NLang.DevelopmentKit.Shared.Projects;
using System;

namespace NLang.DevelopmentKit.Shared.Modules;

public class IsOutputInfoAttribute(string variant, string version) : ToolAttributeBase(variant, "", version)
{
    public override Type? RequiredBase => typeof(ProjectOutputInfoBase);
}
