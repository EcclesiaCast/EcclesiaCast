using EcclesiaCast.App.ViewModels;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Services;

public sealed class ProjectionWindowService(ProjectionViewModel projectionViewModel)
    : IProjectionWindowService
{
    private OutputWindow? _window;

    public bool IsOutputVisible => _window?.IsVisible == true;

    public event EventHandler? VisibilityChanged;

    public event EventHandler? VideoEnded;

    public void EnsureVisible(DisplayInfo display)
    {
        if (_window is null || !_window.IsLoaded)
        {
            _window = new OutputWindow { DataContext = projectionViewModel };
            _window.IsVisibleChanged += (_, _) =>
                VisibilityChanged?.Invoke(this, EventArgs.Empty);
            _window.VideoEnded += (_, _) => VideoEnded?.Invoke(this, EventArgs.Empty);

            // The operator's Live panel draws the very picture the projector
            // is drawing, so the video moves there too instead of sitting on
            // a still poster — and nothing is decoded twice.
            _window.VideoFrameChanged += (_, frame) => projectionViewModel.LiveFrame = frame;
        }

        var wasHidden = !_window.IsVisible;
        _window.ShowOn(display);

        // Coming back from "output off": pick the video up where it stopped.
        if (wasHidden)
            _window.ResumeAfterHiddenOutput();
    }

    /// <summary>
    /// Hides the output. The video is paused too: hiding the window on its own
    /// left it playing, so the sound kept going out and coming back mid-clip.
    /// </summary>
    public void HideOutput()
    {
        _window?.PauseForHiddenOutput();
        _window?.Hide();
    }

    // ── Transporte del video proyectado ──────────────────────────

    public PlaybackState Playback => _window?.PlaybackState ?? PlaybackState.None;

    public void TogglePlayPause() => _window?.TogglePlayPause();

    public void SeekTo(TimeSpan position) => _window?.SeekTo(position);

    public void Skip(TimeSpan delta) => _window?.Skip(delta);
}
