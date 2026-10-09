namespace EcclesiaCast.Core.Updates;

/// <summary>A newer version the operator can install.</summary>
/// <param name="Version">The version, as people read it (1.0.0, no leading v).</param>
/// <param name="Notes">What it brings, already turned into plain text.</param>
public sealed record UpdateOffer(
    AppVersion Version,
    string Tag,
    bool IsPreRelease,
    string Notes,
    ReleaseAsset Installer);

/// <summary>
/// Decides which release, if any, is worth offering. It looks only at the
/// list GitHub sent, so it needs neither internet nor windows to be tested.
/// </summary>
public static class UpdatePicker
{
    /// <summary>
    /// The newest release that is worth offering to someone running
    /// <paramref name="installed"/>, or null when there is none.
    /// </summary>
    /// <param name="skipped">
    /// The version the operator chose «Saltar esta versión» for. Only that
    /// exact version is hidden: skipping 1.0.1 does not hide 1.0.2. Null for a
    /// search the operator asked for by hand, which shows everything.
    /// </param>
    public static UpdateOffer? Choose(
        AppVersion installed, IEnumerable<ReleaseInfo> releases, AppVersion? skipped = null)
    {
        UpdateOffer? best = null;

        foreach (var release in releases)
        {
            if (release.IsDraft)
                continue;

            if (!AppVersion.TryParse(release.Tag, out var version))
                continue;

            // Someone on a stable version asked for stable ones only. Whoever
            // is testing betas gets the betas that follow and the 1.0 itself.
            var isPreRelease = release.IsPreRelease || version.IsPreRelease;
            if (isPreRelease && !installed.IsPreRelease)
                continue;

            if (version <= installed)
                continue;

            if (skipped is not null && version == skipped)
                continue;

            var installer = FindInstaller(release);
            if (installer is null)
                continue;

            if (best is null || version > best.Version)
                best = new UpdateOffer(version, release.Tag, isPreRelease, ReleaseNotesText.FromRelease(release.Body), installer);
        }

        return best;
    }

    /// <summary>
    /// The file to run: EcclesiaCast-&lt;version&gt;-setup.exe. It also has to
    /// be fetched over https, so a damaged answer cannot send the program to
    /// download from somewhere in the clear.
    /// </summary>
    private static ReleaseAsset? FindInstaller(ReleaseInfo release) =>
        release.Assets.FirstOrDefault(asset =>
            asset.Name.StartsWith("EcclesiaCast-", StringComparison.OrdinalIgnoreCase)
            && asset.Name.EndsWith("-setup.exe", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(asset.DownloadUrl, UriKind.Absolute, out var url)
            && url.Scheme == Uri.UriSchemeHttps);
}
