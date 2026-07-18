using System.Windows;
using System.Windows.Controls;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;
using Serilog;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// Plays a YouTube video full screen using the official embedded player,
/// signed in with the operator's own browser session (so a Premium account
/// gets its usual ad-free playback).
/// </summary>
public partial class YouTubeSurface : UserControl
{
    private bool _ready;
    private MediaItem? _pending;
    private string? _currentId;

    public YouTubeSurface()
    {
        InitializeComponent();
        Loaded += async (_, _) => await EnsureBrowserAsync();
    }

    /// <summary>Raised when the video reaches its end and shouldn't loop.</summary>
    public event EventHandler<VideoEndBehavior>? Ended;

    private async Task EnsureBrowserAsync()
    {
        if (_ready)
            return;

        try
        {
            var environment = await WebViewProfile.GetEnvironmentAsync();
            await Browser.EnsureCoreWebView2Async(environment);

            Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            Browser.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            Browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            Browser.CoreWebView2.WebMessageReceived += OnWebMessage;

            _ready = true;

            if (_pending is { } pending)
            {
                _pending = null;
                Play(pending);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo iniciar el navegador embebido (WebView2)");
        }
    }

    /// <summary>Loads and plays the video; call with null to clear the player.</summary>
    public void Play(MediaItem? media)
    {
        if (media is not { Type: MediaType.YouTube } || string.IsNullOrWhiteSpace(media.YouTubeId))
        {
            Clear();
            return;
        }

        if (!_ready)
        {
            _pending = media;
            return;
        }

        if (media.YouTubeId == _currentId)
            return;

        _currentId = media.YouTubeId;
        var loop = media.EndBehavior == VideoEndBehavior.Loop ? "true" : "false";
        var muted = media.Muted ? "true" : "false";
        Browser.NavigateToString(BuildPlayerHtml(media.YouTubeId!, loop, muted, media.Volume));
    }

    public void Clear()
    {
        _pending = null;
        _currentId = null;
        if (_ready)
            Browser.NavigateToString("<html><body style='margin:0;background:#000'></body></html>");
    }

    private void OnWebMessage(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        // The page posts "ended" when playback finishes without looping.
        if (e.TryGetWebMessageAsString() == "ended")
            Ended?.Invoke(this, VideoEndBehavior.Stop);
    }

    /// <summary>
    /// A minimal page hosting YouTube's IFrame player API, which gives us the
    /// end-of-video event and programmatic control while keeping the official
    /// player (and the signed-in session) intact.
    /// </summary>
    private static string BuildPlayerHtml(string videoId, string loop, string muted, int volume) => $$"""
        <!doctype html>
        <html>
          <head><meta charset="utf-8"><style>
            html,body{margin:0;height:100%;background:#000;overflow:hidden}
            #player{width:100%;height:100%}
          </style></head>
          <body>
            <div id="player"></div>
            <script src="https://www.youtube.com/iframe_api"></script>
            <script>
              var loop = {{loop}}, muted = {{muted}}, volume = {{volume}};
              var player;
              function onYouTubeIframeAPIReady() {
                player = new YT.Player('player', {
                  videoId: '{{videoId}}',
                  playerVars: {
                    autoplay: 1, controls: 0, rel: 0, modestbranding: 1,
                    iv_load_policy: 3, playsinline: 1, fs: 0, disablekb: 1
                  },
                  events: {
                    onReady: function (e) {
                      if (muted) { e.target.mute(); } else { e.target.unMute(); e.target.setVolume(volume); }
                      e.target.playVideo();
                    },
                    onStateChange: function (e) {
                      if (e.data === YT.PlayerState.ENDED) {
                        if (loop) { player.seekTo(0); player.playVideo(); }
                        else { window.chrome.webview.postMessage('ended'); }
                      }
                    }
                  }
                });
              }
            </script>
          </body>
        </html>
        """;
}
