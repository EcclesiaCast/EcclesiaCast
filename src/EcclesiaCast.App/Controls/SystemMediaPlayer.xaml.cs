using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EcclesiaCast.App.Services;
using Serilog;
using Windows.Media.Control;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// A small player for whatever is playing on the computer (Spotify, the
/// browser…): cover, title, artist, progress and the three transport
/// buttons. It reads the same media controls Windows shows when the volume
/// changes, so it needs no Spotify account and no Spotify code. It only
/// listens while it is on screen; with the tab hidden it costs nothing.
/// </summary>
public partial class SystemMediaPlayer : UserControl
{
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };

    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;

    /// <summary>Bumped on every start and stop, so a slow answer from Windows is ignored when it is stale.</summary>
    private int _generation;

    public SystemMediaPlayer()
    {
        InitializeComponent();
        _clock.Tick += (_, _) => ShowTimeline();
        IsVisibleChanged += (_, _) => Sync();
        Loaded += (_, _) => Sync();
        Unloaded += (_, _) => Stop();
        ShowNothing();
    }

    private void Sync()
    {
        if (IsVisible && IsLoaded)
            _ = StartAsync();
        else
            Stop();
    }

    // ── Escuchar a Windows ───────────────────────────────────────

    private async Task StartAsync()
    {
        if (_manager is not null)
            return;

        var generation = ++_generation;
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (generation != _generation)
                return;

            _manager = manager;
            manager.CurrentSessionChanged += OnCurrentSessionChanged;
            Attach(manager.GetCurrentSession());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo leer lo que suena en la computadora");
            ShowNothing();
        }
    }

    private void Stop()
    {
        _generation++;
        _clock.Stop();

        if (_manager is { } manager)
            manager.CurrentSessionChanged -= OnCurrentSessionChanged;
        _manager = null;
        Detach();
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, object args)
    {
        var generation = _generation;
        Dispatcher.BeginInvoke(() =>
        {
            if (generation == _generation)
                Attach(sender.GetCurrentSession());
        });
    }

    private void Attach(GlobalSystemMediaTransportControlsSession? session)
    {
        Detach();
        _session = session;
        if (session is null)
        {
            ShowNothing();
            return;
        }

        session.MediaPropertiesChanged += OnSessionChanged;
        session.PlaybackInfoChanged += OnSessionChanged;
        session.TimelinePropertiesChanged += OnSessionChanged;
        _ = ShowMediaAsync(session);
        ShowTimeline();
    }

    private void Detach()
    {
        if (_session is not { } session)
            return;

        session.MediaPropertiesChanged -= OnSessionChanged;
        session.PlaybackInfoChanged -= OnSessionChanged;
        session.TimelinePropertiesChanged -= OnSessionChanged;
        _session = null;
    }

    private void OnSessionChanged(GlobalSystemMediaTransportControlsSession sender, object args)
    {
        var generation = _generation;
        Dispatcher.BeginInvoke(() =>
        {
            if (generation != _generation || _session != sender)
                return;
            _ = ShowMediaAsync(sender);
            ShowTimeline();
        });
    }

    // ── Mostrar ──────────────────────────────────────────────────

    private void ShowNothing()
    {
        _clock.Stop();
        TitleText.Text = "No hay nada sonando";
        ArtistText.Text = "Abrí Spotify y poné una canción.";
        SourceText.Text = string.Empty;
        Cover.Source = null;
        TimeRow.Visibility = Visibility.Collapsed;
    }

    private async Task ShowMediaAsync(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            var properties = await session.TryGetMediaPropertiesAsync();
            if (_session != session)
                return;

            TitleText.Text = string.IsNullOrWhiteSpace(properties.Title) ? "Sin título" : properties.Title;
            ArtistText.Text = properties.Artist;
            SourceText.Text = AppName(session.SourceAppUserModelId);
            Cover.Source = await ReadCoverAsync(properties);
        }
        catch (Exception ex)
        {
            // A player that closes in the middle of the question: the next change redraws.
            Log.Debug(ex, "No se pudieron leer los datos de lo que suena");
        }
    }

    private static async Task<BitmapImage?> ReadCoverAsync(GlobalSystemMediaTransportControlsSessionMediaProperties properties)
    {
        if (properties.Thumbnail is not { } reference)
            return null;

        try
        {
            using var source = await reference.OpenReadAsync();
            using var stream = source.AsStreamForRead();
            var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            copy.Position = 0;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 128;
            image.StreamSource = copy;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo leer la carátula");
            return null;
        }
    }

    private void ShowTimeline()
    {
        if (_session is not { } session)
            return;

        try
        {
            var info = session.GetPlaybackInfo();
            var playing = info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            var timeline = session.GetTimelineProperties();

            var length = timeline.EndTime - timeline.StartTime;
            if (length <= TimeSpan.Zero)
            {
                TimeRow.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Windows reports the position only when it changes: while it plays, it is worked out.
                var position = timeline.Position;
                if (playing)
                    position += (DateTimeOffset.UtcNow - timeline.LastUpdatedTime) * (info.PlaybackRate ?? 1.0);
                position = TimeSpan.FromTicks(Math.Clamp(position.Ticks, 0, length.Ticks));

                TimeRow.Visibility = Visibility.Visible;
                Progress.Value = (double)position.Ticks / length.Ticks;
                PositionText.Text = Format(position);
                TotalText.Text = Format(length);
            }

            PlayPauseButton.Content = playing ? "⏸" : "▶";
            if (playing)
                _clock.Start();
            else
                _clock.Stop();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo leer el avance de lo que suena");
        }
    }

    private static string Format(TimeSpan time) =>
        time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss");

    /// <summary>«Spotify.exe» → «Spotify»; the Store's «SpotifyAB.SpotifyMusic_xyz!Spotify» → «Spotify».</summary>
    private static string AppName(string? appId)
    {
        if (string.IsNullOrWhiteSpace(appId))
            return string.Empty;

        var name = appId[(appId.LastIndexOf('!') + 1)..];
        return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
    }

    // ── Botones ──────────────────────────────────────────────────

    private async void Previous_Click(object sender, RoutedEventArgs e) =>
        await DoAsync(s => s.TrySkipPreviousAsync().AsTask(), MediaKey.PreviousTrack);

    private async void PlayPause_Click(object sender, RoutedEventArgs e) =>
        await DoAsync(s => s.TryTogglePlayPauseAsync().AsTask(), MediaKey.PlayPause);

    private async void Next_Click(object sender, RoutedEventArgs e) =>
        await DoAsync(s => s.TrySkipNextAsync().AsTask(), MediaKey.NextTrack);

    /// <summary>The command goes to the player shown; with none, it goes out as a media key.</summary>
    private async Task DoAsync(Func<GlobalSystemMediaTransportControlsSession, Task<bool>> command, MediaKey fallback)
    {
        try
        {
            if (_session is { } session && await command(session))
                return;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "El reproductor no aceptó el botón");
        }

        MediaKeys.Send(fallback);
    }
}
