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

    /// <summary>
    /// Host name the player page is served under. YouTube's IFrame API refuses
    /// to play (error 153) when the embedding page has no real origin, which is
    /// what NavigateToString produces, so the page is mapped to a virtual host.
    /// </summary>
    public const string PlayerHost = "player.ecclesiacast.local";

    /// <summary>Folder backing <see cref="PlayerHost"/>.</summary>
    public static string PlayerFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EcclesiaCast", "player");

    public static Task<CoreWebView2Environment> GetEnvironmentAsync()
    {
        Directory.CreateDirectory(UserDataFolder);
        return _environment ??= CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: UserDataFolder,
            options: new CoreWebView2EnvironmentOptions
            {
                // Without this the browser's autoplay policy refuses to start a
                // video that has sound until someone clicks the page, so a video
                // projected with audio would just sit on its first frame.
                AdditionalBrowserArguments = "--autoplay-policy=no-user-gesture-required",
            });
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
