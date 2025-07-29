using System;

namespace NLang.DevelopmentKit.Shared.Modules;

public class IsLinkerInfoAttribute(string variant, string libVersion) : ToolAttributeBase(variant, "", libVersion)
{
    public override Type? RequiredBase => typeof(LinkerInfoBase);
}
