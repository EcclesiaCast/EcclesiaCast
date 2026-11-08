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
    /// The package of Bibles the program offers to download (Biblia → 📥 →
    /// «Bajar Biblias de internet…»): only for whoever asks for it, never in the
    /// installer. Only that exact file is accepted (its SHA-256). Until it is
    /// uploaded to that address the program says it is not available yet. A new
    /// package means a new address and a new hash here.
    /// </summary>
    public const string BiblePackUrl =
        "https://github.com/EcclesiaCast/EcclesiaCast/releases/download/biblias-1/EcclesiaCast-biblias.ecbiblias";

    public const string BiblePackSha256 = "3781681b89e37c99108da66d2c15b565ba125e08b2fa3a3060efddc5fb276951";

    /// <summary>What the package brings, for the question before downloading it.</summary>
    public const string BiblePackContents = "NTV, NVI, RV1960 y TLA";

    public static bool HasDonateUrl => DonateUrl.StartsWith("https://", StringComparison.Ordinal);
}
