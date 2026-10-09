using System.Text.Json;

namespace EcclesiaCast.Core.Updates;

/// <summary>One file attached to a release.</summary>
/// <param name="Sha256">
/// The SHA-256 GitHub computed for the file, as lowercase hexadecimal. GitHub
/// gives it for new files and may leave it out for old ones: null then.
/// </param>
public sealed record ReleaseAsset(string Name, string DownloadUrl, long Size, string? Sha256);

/// <summary>What GitHub says about one release.</summary>
/// <param name="Body">The notes, in Markdown. Never null.</param>
public sealed record ReleaseInfo(
    string Tag,
    bool IsPreRelease,
    bool IsDraft,
    string Body,
    IReadOnlyList<ReleaseAsset> Assets);

/// <summary>
/// Reads the answer of <c>GET /repos/{owner}/{repo}/releases</c>. Only the
/// handful of fields the updater needs are taken, and every one of them is
/// optional: a release missing a piece is still a release, just a less useful
/// one, and must not break the reading of the others.
/// </summary>
public static class GitHubReleases
{
    /// <summary>Where the releases of EcclesiaCast are listed.</summary>
    public const string DefaultUrl = "https://api.github.com/repos/EcclesiaCast/EcclesiaCast/releases";

    /// <exception cref="JsonException">The text is not the list GitHub sends.</exception>
    public static IReadOnlyList<ReleaseInfo> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new JsonException("GitHub no mandó una lista de versiones.");

        var releases = new List<ReleaseInfo>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
                continue;

            var tag = Text(element, "tag_name");
            if (string.IsNullOrWhiteSpace(tag))
                continue;

            releases.Add(new ReleaseInfo(
                tag,
                Flag(element, "prerelease"),
                Flag(element, "draft"),
                Text(element, "body") ?? string.Empty,
                Assets(element)));
        }

        return releases;
    }

    private static List<ReleaseAsset> Assets(JsonElement release)
    {
        var assets = new List<ReleaseAsset>();
        if (!release.TryGetProperty("assets", out var list) || list.ValueKind != JsonValueKind.Array)
            return assets;

        foreach (var asset in list.EnumerateArray())
        {
            if (asset.ValueKind != JsonValueKind.Object)
                continue;

            var name = Text(asset, "name");
            var url = Text(asset, "browser_download_url");
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
                continue;

            var size = asset.TryGetProperty("size", out var sizeElement)
                && sizeElement.ValueKind == JsonValueKind.Number
                && sizeElement.TryGetInt64(out var value)
                ? value
                : 0;

            assets.Add(new ReleaseAsset(name, url, size, Sha256Of(Text(asset, "digest"))));
        }

        return assets;
    }

    /// <summary>"sha256:ABC…" becomes "abc…"; any other algorithm, or nothing, becomes null.</summary>
    internal static string? Sha256Of(string? digest)
    {
        const string prefix = "sha256:";
        if (digest is null || !digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var hex = digest[prefix.Length..].Trim().ToLowerInvariant();
        return hex.Length == 64 && hex.All(Uri.IsHexDigit) ? hex : null;
    }

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool Flag(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
}
