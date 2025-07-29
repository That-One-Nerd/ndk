using System.Xml.Linq;

namespace NLang.DevelopmentKit.Shared.Modules.Builtin;

[IsLinkerInfo("standardlib", NDK.VersionStr)]
public class StandardLibraryLinkerInfo : LinkerInfoBase
{
    public override string Variant => "standardlib";

    public override LinkInfoBase ParseReference(XElement refNode)
    {
        XElement name = refNode.Element(nameof(LinkInfo.Name)) ?? throw new($"Missing <{nameof(LinkInfo.Name)}> element.");
        return new LinkInfo()
        {
            Name = name.Value ?? throw new($"<{nameof(LinkInfo.Name)}> element has no value.")
        };
    }

    public class LinkInfo : LinkInfoBase
    {
        public override string Type => "standardlib";

        public required string Name { get; init; }

        public override string ToString()
        {
            // This could probably be improved to be more helpful.
            return $"StandardLibraryLink: {Name}";
        }
    }
}
