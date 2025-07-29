using System.Xml.Linq;

namespace NLang.DevelopmentKit.Shared.Projects;

public class ProjectGeneralInfo
{
    public required string Language { get; set; }
    public required string Version { get; set; }

    public static ProjectGeneralInfo FromProjectFile(XElement root)
    {
        // If this becomes large, I might invest in an auto parser
        // that uses reflection. But for now it's fine.
        return new()
        {
            Language = root.Element(nameof(Language))?.Value ?? "",
            Version = root.Element(nameof(Version))?.Value ?? ""
        };
    }
}
