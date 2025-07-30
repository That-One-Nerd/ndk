using System.ComponentModel;

namespace NLang.DevelopmentKit.View;

public enum ViewOption
{
    [Description("Displays compilation, linker, and output properties of a project or file.")] Properties,
    [Description("Displays a tree of all code files in the directory. Must be applied to a project.")] Directory,
    [Description("Displays a hierarchy of all namespaces, types, and members of those types in a project or file.")] Outline
}
