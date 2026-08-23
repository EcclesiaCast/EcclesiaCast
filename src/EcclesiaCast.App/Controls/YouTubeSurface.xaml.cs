using System.IO;
using System.Windows.Controls;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// Plays a YouTube video full screen using the official embedded player,
/// signed in with the operator's own browser session (so a Premium account
/// gets its usual ad-free playback).
/// </summary>
public partial class YouTubeSurface : UserControl
{
    private const string PlayerPage = "player.html";

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

            // Serve the player page from a real https origin. Loading it with
            // NavigateToString leaves the origin null and YouTube then refuses
            // to play with "error 153: video player configuration error".
            WritePlayerPage();
            Browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
                WebViewProfile.PlayerHost,
                WebViewProfile.PlayerFolder,
                CoreWebView2HostResourceAccessKind.DenyCors);

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

    /// <summary>
    /// Drops the player page on disk for the virtual host to serve. Rewrites it
    /// only when the contents changed, so recreating the output window (moving
    /// to another monitor) can't collide with a copy still being written.
    /// </summary>
    private static void WritePlayerPage()
    {
        var page = Path.Combine(WebViewProfile.PlayerFolder, PlayerPage);
        try
        {
            Directory.CreateDirectory(WebViewProfile.PlayerFolder);
            if (File.Exists(page) && File.ReadAllText(page) == PlayerHtml)
                return;
            File.WriteAllText(page, PlayerHtml);
        }
        catch (IOException ex)
        {
            // Another surface just wrote the same bytes; carry on.
            Log.Debug(ex, "No se pudo reescribir la página del reproductor");
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
        _blur = media.Blur;
        _blurApplied = false;
        var loop = media.EndBehavior == VideoEndBehavior.Loop ? "1" : "0";
        var muted = media.Muted ? "1" : "0";
        Browser.CoreWebView2.Navigate(
            $"https://{WebViewProfile.PlayerHost}/{PlayerPage}" +
            $"?v={Uri.EscapeDataString(media.YouTubeId!)}&loop={loop}&muted={muted}&volume={media.Volume}");
    }

    public void Clear()
    {
        _pending = null;
        _currentId = null;
        _state = PlaybackState.None;
        _pausedForHiddenOutput = false;
        if (_ready)
            Browser.NavigateToString("<html><body style='margin:0;background:#000'></body></html>");
    }

    // ── Transporte (barra de reproducción del operador) ──────────

    private PlaybackState _state = PlaybackState.None;
    private bool _pausedForHiddenOutput;

    /// <summary>Last position the page reported; it posts one every 300 ms.</summary>
    public PlaybackState State => _currentId is null ? PlaybackState.None : _state;

    public void Pause() => Run("pause()");

    public void Resume() => Run("play()");

    public void TogglePlayPause() => Run(_state.IsPlaying ? "pause()" : "play()");

    public void PauseForHiddenOutput()
    {
        if (_currentId is null || !_state.IsPlaying)
            return;

        _pausedForHiddenOutput = true;
        Pause();
    }

    public void ResumeAfterHiddenOutput()
    {
        if (!_pausedForHiddenOutput)
            return;

        _pausedForHiddenOutput = false;
        Resume();
    }

    public void SeekTo(TimeSpan position) =>
        Run($"seek({Math.Max(0, position.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture)})");

    public void Skip(TimeSpan delta) => SeekTo(_state.Position + delta);

    /// <summary>
    /// Blurs the video, 0–100. WebView2 hosts a native window, so a WPF effect
    /// can't touch it — the blur is applied as a CSS filter inside the page.
    /// </summary>
    public void SetBlur(double amount)
    {
        _blur = Math.Clamp(amount, 0, 100);
        Run($"blur({(_blur * 0.6).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)})");
    }

    private double _blur;
    private bool _blurApplied;

    /// <summary>Calls one of the page's helper functions; ignored if it isn't ready.</summary>
    private void Run(string call)
    {
        if (!_ready || _currentId is null)
            return;

        try
        {
            _ = Browser.CoreWebView2.ExecuteScriptAsync($"window.ec && window.ec.{call};");
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo controlar el reproductor de YouTube ({Call})", call);
        }
    }

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.TryGetWebMessageAsString();

        // The page posts "t <position>;<duration>;<playing>" a few times a
        // second — it is the only way to know where an embedded video is.
        if (message?.StartsWith("t ") == true)
        {
            var parts = message[2..].Split(';');
            if (parts.Length == 3
                && double.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var position)
                && double.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var duration))
            {
                _state = new PlaybackState(
                    HasVideo: true,
                    IsPlaying: parts[2] == "1",
                    Position: TimeSpan.FromSeconds(Math.Max(0, position)),
                    Duration: TimeSpan.FromSeconds(Math.Max(0, duration)));
            }

            // The first report means the page is alive: now the blur sticks
            // (calls made while it was still loading went nowhere).
            if (!_blurApplied)
            {
                _blurApplied = true;
                SetBlur(_blur);
            }
            return;
        }

        // The page posts "ended" when playback finishes without looping.
        if (message == "ended")
        {
            Ended?.Invoke(this, VideoEndBehavior.Stop);
            return;
        }

        // ...and "error N" when YouTube itself refuses to play the video, which
        // otherwise only shows up as a message on the projector.
        if (message?.StartsWith("error ") == true)
            Log.Warning("El reproductor de YouTube rechazó el video {Id}: {Message}", _currentId, message);
    }

    /// <summary>
    /// A minimal page hosting YouTube's IFrame player API, which gives us the
    /// end-of-video event and programmatic control while keeping the official
    /// player (and the signed-in session) intact. Written to disk once and
    /// served over <see cref="WebViewProfile.PlayerHost"/>; the video and its
    /// options arrive as query parameters.
    /// </summary>
    private const string PlayerHtml = """
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
              var params = new URLSearchParams(location.search);
              var videoId = params.get('v');
              var loop = params.get('loop') === '1';
              var muted = params.get('muted') === '1';
              var volume = parseInt(params.get('volume') || '100', 10);
              var player;
              function post(text) {
                try { window.chrome.webview.postMessage(String(text)); } catch (e) {}
              }
              function onYouTubeIframeAPIReady() {
                player = new YT.Player('player', {
                  videoId: videoId,
                  playerVars: {
                    origin: location.origin,
                    autoplay: 1, controls: 0, rel: 0, modestbranding: 1,
                    iv_load_policy: 3, playsinline: 1, fs: 0, disablekb: 1,
                    // Asks for captions off. Note this only expresses a
                    // preference: when the signed-in account has "always show
                    // captions" set, the embedded player shows them anyway and
                    // neither unloadModule('captions') nor setOption(...,'track',{})
                    // overrides it (both verified against the real player). The
                    // fix is to turn captions off in the YouTube account itself.
                    cc_load_policy: 0
                  },
                  events: {
                    onReady: function (e) {
                      if (muted) { e.target.mute(); } else { e.target.unMute(); e.target.setVolume(volume); }
                      e.target.playVideo();
                      startReporting();
                    },
                    onStateChange: function (e) {
                      if (e.data === YT.PlayerState.ENDED) {
                        if (loop) { player.seekTo(0); player.playVideo(); }
                        else { post('ended'); }
                      }
                    },
                    onError: function (e) { post('error ' + e.data); }
                  }
                });
              }

              // The host has no other way to know where the video is, so the
              // page reports its position a few times a second, and exposes
              // the transport controls the operator's play bar drives.
              function startReporting() {
                setInterval(function () {
                  try {
                    var playing = player.getPlayerState() === YT.PlayerState.PLAYING;
                    post('t ' + player.getCurrentTime() + ';' + player.getDuration() + ';' + (playing ? '1' : '0'));
                  } catch (e) {}
                }, 300);
              }

              window.ec = {
                play: function () { try { player.playVideo(); } catch (e) {} },
                pause: function () { try { player.pauseVideo(); } catch (e) {} },
                seek: function (seconds) { try { player.seekTo(seconds, true); } catch (e) {} },
                // Blurring shrinks the picture at the edges, so scale it up a
                // little to keep the screen covered.
                blur: function (px) {
                  var box = document.getElementById('player');
                  box.style.filter = px > 0 ? 'blur(' + px + 'px)' : '';
                  box.style.transform = px > 0 ? 'scale(1.08)' : '';
                }
              };
            </script>
          </body>
        </html>
        """;
}
