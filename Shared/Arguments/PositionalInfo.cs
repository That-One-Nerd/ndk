using System;
using System.Reflection;

namespace NLang.DevelopmentKit.Shared.Arguments;

public class PositionalInfo : ArgumentInfo
{
    public int Index { get; internal set; }

    internal PositionalInfo() { }
}
