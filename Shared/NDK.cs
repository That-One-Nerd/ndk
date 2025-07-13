using System;

namespace NLang.DevelopmentKit.Shared;

public static class NDK
{
    public static readonly string RepositoryUrl = "https://github.com/That-One-Nerd/ndk";

    public static readonly Version Version = Version.Parse(VersionStr);
    public const string VersionStr = $"{VersionMajor}.{VersionMinor}";
    public const string VersionMajor = "0.1";
    public const string VersionMinor = "0.0";
}
