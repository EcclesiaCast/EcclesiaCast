using System.Reflection;

namespace EcclesiaCast.App.Services;

/// <summary>What the program says about itself: shown by the loading screen and "Acerca de".</summary>
public static class AppInfo
{
    /// <summary>The version without the build suffix the SDK appends (1.0.0-beta.6+abc123 → 1.0.0-beta.6).</summary>
    public static string Version { get; } =
        typeof(AppInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0] ?? "?";

    /// <summary>
    /// The page where people can chip in (the PayPal link). Empty until Alexis
    /// creates it: while it is empty the «Colaborar» entries stay hidden, so
    /// there is never a button that leads nowhere.
    /// </summary>
    public const string DonateUrl = "https://paypal.me/ecclesiacast";

    /// <summary>
    /// The pack of extra backgrounds, an asset of a GitHub release that is not a
    /// version of the program, and its SHA-256: only that exact file is accepted.
    /// A new pack means a new release, a new address and a new hash here.
    /// </summary>
    public const string BackgroundPackUrl =
        "https://github.com/EcclesiaCast/EcclesiaCast/releases/download/fondos-extra-1/EcclesiaCast-fondos-extra.zip";

    public const string BackgroundPackSha256 = "f9895baadfec4179972ba5b3b452c5331a1b4d9f7e484b3c97d366afad159236";

    /// <summary>
    /// The list of Bibles the program offers to download (Biblia → 📥 → «Bajar
    /// Biblias de internet…»; see <see cref="EcclesiaCast.Core.Bible.BibleCatalog"/>).
    /// The Bibles live next to it, in the Cloudflare R2 bucket
    /// «ecclesiacast-biblias», served through its public r2.dev address. If it
    /// is ever left empty, the program says there are no Bibles to download yet.
    /// ECCLESIACAST_BIBLE_CATALOG points a test at another list (for example one
    /// served on this computer).
    /// </summary>
    public const string BibleCatalogUrl = "https://pub-ec2cd8b767e842a2970aa9564a3ea4fb.r2.dev/catalogo.json";

    public static string CurrentBibleCatalogUrl =>
        Environment.GetEnvironmentVariable("ECCLESIACAST_BIBLE_CATALOG") is { Length: > 0 } test ? test : BibleCatalogUrl;

    /// <summary>
    /// The anonymous count of use (servicios/api in the repository, on
    /// Cloudflare). ECCLESIACAST_USAGE_URL points a test at another address.
    /// </summary>
    public const string UsageCountUrl = "https://api.ecclesiacast.org/v1/uso";

    public static string CurrentUsageCountUrl =>
        Environment.GetEnvironmentVariable("ECCLESIACAST_USAGE_URL") is { Length: > 0 } test ? test : UsageCountUrl;

    /// <summary>
    /// The search of backgrounds on Pixabay, through our own service (it keeps
    /// the Pixabay key, which can not live in public code).
    /// ECCLESIACAST_BACKGROUNDS_URL points a test at another address.
    /// </summary>
    public const string BackgroundSearchUrl = "https://api.ecclesiacast.org/v1/fondos";

    public static string CurrentBackgroundSearchUrl =>
        Environment.GetEnvironmentVariable("ECCLESIACAST_BACKGROUNDS_URL") is { Length: > 0 } test ? test : BackgroundSearchUrl;

    public static bool HasDonateUrl => DonateUrl.StartsWith("https://", StringComparison.Ordinal);
}
