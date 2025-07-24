using System;

namespace NLang.DevelopmentKit.Shared.Modules;

public class IsSubsystemAttribute(string cliName, string version) : ToolAttributeBase(cliName, "", version)
{
    public override Type? RequiredBase { get; } = typeof(SubsystemBase);
}
