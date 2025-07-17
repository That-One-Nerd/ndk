using NLang.DevelopmentKit.Shared.Arguments;

namespace NLang.DevelopmentKit.Hub;

/// <summary>
/// Arguments you enter into the hub. The `submoduleArgs` are just going to be passed to the next module called.
/// </summary>
public class HubArguments : ArgumentBase<HubArguments>
{
    [IsPositional(0)] public string subsystem = "";
    [IsPositional(1)] [Remainder] public string[] subsystemArgs = [];

    [IsFlag("--help")] public bool showHelp;
    [IsFlag("--credits")] public bool showCredits;
    [IsFlag("--version")] public bool showVersion;
}
