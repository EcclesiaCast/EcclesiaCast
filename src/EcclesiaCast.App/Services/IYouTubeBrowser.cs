namespace EcclesiaCast.App.Services;

/// <summary>Opens the embedded YouTube browser (sign in, search, pick videos).</summary>
public interface IYouTubeBrowser
{
    /// <summary>Video ids the operator added; empty if they just browsed or signed in.</summary>
    IReadOnlyList<string> Browse();
}
