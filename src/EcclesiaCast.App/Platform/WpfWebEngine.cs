using EcclesiaCast.App.Services;

namespace EcclesiaCast.App.Platform;

/// <summary>The WebView2 of the program, behind <see cref="IWebEngine"/>.</summary>
public sealed class WpfWebEngine : IWebEngine
{
    public bool IsRuntimeAvailable() => WebViewProfile.IsRuntimeAvailable();

    public bool ShowPointer
    {
        get => WebLiveFeed.ShowPointer;
        set => WebLiveFeed.ShowPointer = value;
    }
}
