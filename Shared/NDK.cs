using NLang.DevelopmentKit.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

[assembly: AssemblyVersion(NDK.VersionStr)]
[assembly: AssemblyInformationalVersion(NDK.VersionFull)]

namespace NLang.DevelopmentKit.Shared;

public static class NDK
{
    public const string RepositoryUrl = "https://github.com/That-One-Nerd/ndk";
    public const string ContributorsUrl = "https://api.github.com/repos/That-One-Nerd/ndk/contributors";

    public static readonly Version Version = Version.Parse($"{VersionMajor}.{VersionMinor}");
    public const string VersionFull = $"{VersionStr}{VersionSuffix}";
    public const string VersionStr = $"{VersionMajor}.{VersionMinor}";
    public const string VersionMajor = "0.1";
    public const string VersionMinor = "0.0";
    public const string VersionSuffix = "-alpha";

    public static readonly bool IsPrerelease = !string.IsNullOrEmpty(VersionSuffix);

    public static async Task<string[]?> GetContributors()
    {
        try
        {
            HttpClient web = new();
            web.DefaultRequestHeaders.Add("User-Agent", "ndk-contributor-fetcher");
            HttpResponseMessage response = await web.GetAsync(ContributorsUrl);
            if (!response.IsSuccessStatusCode) return null;

            JsonArray? contributors = JsonSerializer.Deserialize<JsonArray>(response.Content.ReadAsStream());
            if (contributors is null) return null;

            // Sort by contribution count.
            List<(string, int)> users = [];
            foreach (JsonNode? node in contributors)
            {
                if (node is not JsonObject user) continue;

                string username = user["login"]!.GetValue<string>();
                int contributions = user["contributions"]!.GetValue<int>();
                users.Add((username, contributions));
            }
            users.Sort((a, b) => -a.Item2.CompareTo(b.Item2));

            // TODO: Should we show contribution count? Could be cool.
            return [.. from user in users
                       select user.Item1];
        }
        catch
        {
            return null;
        }
    }
}
