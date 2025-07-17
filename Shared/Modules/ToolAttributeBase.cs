using System;

namespace NLang.DevelopmentKit.Shared.Modules;

/// <summary>
/// Represents an attribute applied to a class that should
/// be loaded by the toolkit. Keep in mind anything using this attribute
/// should also derive from `IModuleTool`
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
public abstract class ToolAttributeBase(string variant, string language, string version) : Attribute
{
    public string Variant { get; protected init; } = variant;
    public string Language { get; protected init; } = language;

    public string VersionStr { get; protected init; } = version;

    public abstract Type? RequiredBase { get; }
}
