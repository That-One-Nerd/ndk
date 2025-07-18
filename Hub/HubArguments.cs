using NLang.DevelopmentKit.Shared.Arguments;

namespace NLang.DevelopmentKit.Hub;

/// <summary>
/// Arguments you enter into the hub. The `submoduleArgs` are just going to be passed to the next module called.
/// </summary>
public class HubArguments : ArgumentBase<HubArguments>
{
    [IsPositional(0)] public string subsystem = "";
    [IsPositional(1)] [Remainder] public string[] subsystemArgs = [];

    [Category("About")]
    [IsFlag("--help", "Displays this menu.")] public bool showHelp;
    [IsFlag("--credits", "Displays the GitHub repository for this software and all contributors.")] public bool showCredits;
    [IsFlag("--version", "Displays the current version of this software.")] public bool showVersion;
}
