using NLang.DevelopmentKit.Shared;
using NLang.DevelopmentKit.Shared.Modules;
using System.Reflection;

[assembly: AssemblyVersion(NDK.VersionStr)]
[assembly: AssemblyInformationalVersion(NDK.VersionFull)]

namespace NLang.DevelopmentKit.Start;

[IsSubsystem("start", NDK.VersionStr)]
public class Program : SubsystemBase
{
    public override string Name => "start";
    public override string Description => "Creates an NLang project of a specific language in the current directory.";

    public override void Invoke(string[] argsStr)
    {
        StartArguments args = StartArguments.Parse(argsStr);

        if (args.PrintAnyIssues()) return;

        // TODO: Make the frontend.
    }
}
