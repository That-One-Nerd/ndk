using NLang.DevelopmentKit.Shared.Arguments;

namespace NLang.DevelopmentKit.Hub;

/// <summary>
/// Arguments you enter into the hub. The `submoduleArgs` are just going to be passed to the next module called.
/// </summary>
public class HubArguments : ArgumentBase<HubArguments>
{
    [IsPositional(0)] public string submodule = "";
    [IsPositional(1)] [Remainder] public string[] submoduleArgs = [];

    public bool showHelp = true;
    public bool showRepository;
    public bool showVersion;
}
