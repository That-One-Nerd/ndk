using System;
using System.Collections.Generic;
using System.Linq;

namespace NLang.DevelopmentKit.Shared.Modules;

public abstract class LanguageInfoBase : IComparable<LanguageInfoBase>, IModuleTool
{
    public abstract string Identifier { get; }
    public abstract string FullName { get; }
    public abstract string LanguageVersion { get; }
    public abstract string[] Aliases { get; }
    public abstract string[] FileExtensions { get; }

    string IModuleTool.Name => LanguageVersion;
    string IModuleTool.Language => Identifier;

    // More to come at some point.

    public static LanguageInfoBase? Get(string identifier, string langVersion) => ModuleLoader.Get<LanguageInfoBase>(langVersion, identifier);
    public static LanguageInfoBase? GetFirst(string identifier) => ModuleLoader.Get<LanguageInfoBase>().FirstOrDefault(x => x.Identifier == identifier);
    public static LanguageInfoBase? GetHighest(string identifier)
    {
        List<LanguageInfoBase> possible = [.. ModuleLoader.Get<LanguageInfoBase>().Where(x => x.Identifier == identifier)];
        possible.Sort((a, b) => a.CompareTo(b));
        return possible.FirstOrDefault();
    }
    static IModuleTool? IModuleTool.Get(string variant, string language) => ModuleLoader.Get<LanguageInfoBase>(variant, language);

    public virtual int CompareTo(LanguageInfoBase other) => DefaultCompareTo(this, other);
    int IComparable<LanguageInfoBase>.CompareTo(LanguageInfoBase? other) =>
        other is null ? 1 : CompareTo(other);


    public static int DefaultCompareTo(LanguageInfoBase a, LanguageInfoBase b)
    {
        // This method is intended to compare language infos that are
        // different versions of the same language. If the languages are
        // different, it's not really defensible what this method should do.
        if (a.Identifier != b.Identifier) return a.Identifier.CompareTo(b.Identifier);

        // Language versions tend to look something like
        // c#12 or c++25. for all standard Nlang versions, the versions will be typed like
        // nc#12-3 and c++25-4, where the first number is the standard language version and
        // the second number is the Nlang version of that base language.

        // Our default system is this: split versions by the -, and only focus on the first
        // two numbers. if the nlang version is higher (and if it exists), pick that one,
        // otherwise sort by the base version. It's not exactly perfect, but it works for
        // most situations.

        // And just as an extra compatibility step: we parse the numbers as doubles for
        // situations like c#7.3. it probably won't be used much, but there it is. if
        // three-number versions arise, maybe it would be better to parse as versions.
        // Anyway, lots of talk for not a lot of complexity.

        string verA = a.LanguageVersion,
               verB = b.LanguageVersion;

        // Get indices.
        int firstDigitA = -1, firstDigitB = -1,
            separatorA = -1, separatorB = -1,
            lastDigitA = -1, lastDigitB = -1;
        for (int i = 0; i < Math.Max(verA.Length, verB.Length); i++)
        {
            if (i < verA.Length)
            {
                if (char.IsDigit(verA[i])) lastDigitA = i;
                if (firstDigitA == -1)
                {
                    if (char.IsDigit(verA[i])) firstDigitA = i;
                }
                else if (separatorA == -1)
                {
                    if (verA[i] == '-') separatorA = i;
                }
            }
            if (i < verB.Length)
            {
                if (char.IsDigit(verB[i])) lastDigitB = i;
                if (firstDigitB == -1)
                {
                    if (char.IsDigit(verB[i])) firstDigitB = i;
                }
                else if (separatorB == -1)
                {
                    if (verB[i] == '-') separatorB = i;
                }
            }
        }

        // TODO: Do we need to catch this?
        //       Nothing I tried caused this system to crash,
        //       but I also didn't test a tremendous amount.
        double baseVerA, baseVerB,
               nLangVerA, nLangVerB;

        if (separatorA >= 0)
        {
            string nLangSegmentA = verA[(separatorA + 1)..(lastDigitA + 1)];
            if (!double.TryParse(nLangSegmentA, out nLangVerA)) nLangVerA = 0;
        }
        else
        {
            nLangVerA = 0;
            separatorA = verA.Length;
        }
        if (separatorB >= 0)
        {
            string nLangSegmentB = verB[(separatorB + 1)..(lastDigitB + 1)];
            if (!double.TryParse(nLangSegmentB, out nLangVerB)) nLangVerB = 0;
        }
        else
        {
            nLangVerB = 0;
            separatorB = verB.Length;
        }

        if (firstDigitA >= 0)
        {
            string baseSegmentA = verA[firstDigitA..separatorA];
            if (!double.TryParse(baseSegmentA, out baseVerA)) baseVerA = 0;
        }
        else baseVerA = 0;
        if (firstDigitB >= 0)
        {
            string baseSegmentB = verB[firstDigitB..separatorB];
            if (!double.TryParse(baseSegmentB, out baseVerB)) baseVerB = 0;
        }
        else baseVerB = 0;

        int cmpTemp = -nLangVerA.CompareTo(nLangVerB);
        if (cmpTemp != 0) return cmpTemp;
        else return -baseVerA.CompareTo(baseVerB);
    }
}
