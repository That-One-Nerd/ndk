using System;

namespace NLang.DevelopmentKit.Shared;

public enum LoadingBarColor
{
    Red,
    Green,
    Yellow,
    Blue,
    Purple,
    Gray,
}

public static class LoadingBarColorExtensions
{
    public static string GetFrontFormat(this LoadingBarColor color) => color switch
    {
        LoadingBarColor.Red => "\x1b[97;101m",
        LoadingBarColor.Green => "\x1b[30;102m",
        LoadingBarColor.Yellow => "\x1b[30;103m",
        LoadingBarColor.Blue => "\x1b[97;104m",
        LoadingBarColor.Purple => "\x1b[97;105m",
        LoadingBarColor.Gray or _ => "\x1b[30;47m"
    };
    public static string GetBackFormat(this LoadingBarColor color) => color switch
    {
        LoadingBarColor.Red => "\x1b[97;41m",
        LoadingBarColor.Green => "\x1b[30;42m",
        LoadingBarColor.Yellow => "\x1b[30;43m",
        LoadingBarColor.Blue => "\x1b[97;44m",
        LoadingBarColor.Purple => "\x1b[97;45m",
        LoadingBarColor.Gray or _ => "\x1b[97;100m"
    };
}

