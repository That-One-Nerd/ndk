using System.Collections.Generic;
using System.Xml.Linq;

namespace NLang.DevelopmentKit.Shared.Projects;

// TODO: We should make the linker extensible like the other things.
//       We could let somebody make a custom linker to add code from other
//       places not natively supported by NLang. See issue #48.
public class ProjectLinkerInfo
{
    public List<string> StandardLibraries { get; } = [];
    public List<string> ExternalLibraries { get; } = [];
    public List<string> ProjectReferences { get; } = [];

    public static ProjectLinkerInfo FromProjectFile(XElement linkerInfoRoot)
    {
        ProjectLinkerInfo info = new();
        foreach (XElement reference in linkerInfoRoot.Elements("Reference"))
        {
            // TODO: Maybe throw an error instead of ignoring.
            XAttribute? type = reference.Attribute("type");
            if (type is null || reference.Value is null) continue;

            switch (type.Value)
            {
                case "standardlib": info.StandardLibraries.Add(type.Value); break;
                case "library": info.ExternalLibraries.Add(type.Value); break;
                case "project": info.ProjectReferences.Add(type.Value); break;
                default: continue;
            }
        }
        return info;
    }
}
