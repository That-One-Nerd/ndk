using NLang.DevelopmentKit.Shared.Arguments;
using System.ComponentModel.DataAnnotations;

namespace NLang.DevelopmentKit.Start;

public class StartArguments : ArgumentBase<StartArguments>
{
    [Required] [IsPositional(0)] public string language = "";
               [IsPositional(1)] public string? template;

    [IsVariable("-dir")] public string? rootDirectory;
}
