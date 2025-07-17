using System;

namespace NLang.DevelopmentKit.Shared.Modules;

/// <summary>
/// Marks a class as a "module" for the CLI. This will be invoked by the hub
/// this class MUST have a <c>public static void Main(string[] args)</c> function.
/// </summary>
public class IsCLIModuleAttribute(string cliName, string version) : ToolAttributeBase(cliName, "", version)
{
    public override Type? RequiredBase => typeof(CLIModuleBase);
}
