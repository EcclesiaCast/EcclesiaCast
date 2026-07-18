using System.IO;
using Microsoft.Web.WebView2.Core;

namespace EcclesiaCast.App.Services;

/// <summary>
/// One shared browser profile for the whole app, so signing in to YouTube
/// once (with the church's own Premium account) keeps the session for the
/// player and across restarts.
/// </summary>
public static class WebViewProfile
{
    private static Task<CoreWebView2Environment>? _environment;

    public static string UserDataFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EcclesiaCast", "browser");

    public static Task<CoreWebView2Environment> GetEnvironmentAsync()
    {
        Directory.CreateDirectory(UserDataFolder);
        return _environment ??= CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: UserDataFolder);
    }

    /// <summary>True when the WebView2 runtime (Edge) is available on this machine.</summary>
    public static bool IsRuntimeAvailable()
    {
        try
        {
            return !string.IsNullOrEmpty(CoreWebView2Environment.GetAvailableBrowserVersionString());
        }
        catch
        {
            return false;
        }
    }
}
