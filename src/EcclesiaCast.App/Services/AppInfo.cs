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
}
