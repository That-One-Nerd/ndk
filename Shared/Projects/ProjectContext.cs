using System.IO;
using System.Xml.Linq;

namespace NLang.DevelopmentKit.Shared.Projects;

public class ProjectContext
{
    public required string ProjectName { get; init; }
    public required ProjectGeneralInfo General { get; init; }
    // TODO: CompilerInfo
    // TODO: OutputInfo
    public required ProjectVariables Variables { get; init; }

    internal ProjectContext() { }

    /*public static ProjectContext? FromDirectory(string dir, bool deep)
    {
        ProjectContext? result = null;
        foreach (string file in Directory.EnumerateFiles(dir))
        {

        }
    }*/
    public static ProjectContext FromFile(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        using FileStream fs = new(path, FileMode.Open, FileAccess.Read);

        XDocument doc = XDocument.Load(fs);
        XElement root = doc.Root!;
        if (root.Name != "Project") throw new("Root element must be a <Project> element.");

        ProjectGeneralInfo generalInfo = ProjectGeneralInfo.FromProjectFile(root);
        ProjectVariables variables = new()
        {
            ProjectName = name,
            NdkVersion = NDK.VersionStr
        };

        XElement? temp;
        if ((temp = root.Element("VariableInfo")) is not null) variables.FromProjectFile(temp);
    }
}
