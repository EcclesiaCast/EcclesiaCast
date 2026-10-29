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
    public const string DonateUrl = "";

    /// <summary>
    /// The pack of extra backgrounds, an asset of a GitHub release that is not a
    /// version of the program, and its SHA-256: only that exact file is accepted.
    /// A new pack means a new release, a new address and a new hash here.
    /// </summary>
    public const string BackgroundPackUrl =
        "https://github.com/EcclesiaCast/EcclesiaCast/releases/download/fondos-extra-1/EcclesiaCast-fondos-extra.zip";

    public const string BackgroundPackSha256 = "f9895baadfec4179972ba5b3b452c5331a1b4d9f7e484b3c97d366afad159236";

    public static bool HasDonateUrl => DonateUrl.StartsWith("https://", StringComparison.Ordinal);
}
