using System;

namespace NLang.DevelopmentKit.Shared.Modules;

public class ModuleToolInfo
{
    public required string Variant { get; set; }
    public required string Language { get; set; }
    public required ToolAttributeBase AttributeInfo { get; set; }
    public required Type Type { get; set; }
    public required Type BaseType { get; set; } // Either IModuleTool or something that derives from it.
    public required Version Version { get; set; }
}
