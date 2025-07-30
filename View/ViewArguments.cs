using NLang.DevelopmentKit.Shared.Arguments;

namespace NLang.DevelopmentKit.View;

public class ViewArguments : ArgumentBase<ViewArguments>
{
    [IsPositional(0)] public ViewOption option;

    [Category("Arguments")]
    [IsVariable("-file", "The project file or build file to target. Leave blank to auto-detect project file.")] public string? file;
}
