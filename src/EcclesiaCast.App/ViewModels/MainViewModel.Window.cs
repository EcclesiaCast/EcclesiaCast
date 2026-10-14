using System.Windows;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Capture;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// Projecting another program's window (a PowerPoint, a video player, a
/// browser) live, with Windows' own window capture.
///
/// Like the live web page, what goes to the screen is a media item that is not
/// in the library (a negative id) whose picture is a feed, here
/// <see cref="WindowCaptureFeed"/>; anything else that takes the background
/// switches the capture off (see <see cref="OnIsWindowLiveChanged"/>).
/// </summary>
public sealed partial class MainViewModel
{
    private const string WindowCursorKey = "window.cursor";

    /// <summary>The id of the window shown live: there is only ever one.</summary>
    private const int WindowLiveId = -3_000_000;

    private IWindowCapture? _windowCapture;
    private bool _windowEventsHooked;

    /// <summary>The window capture. Made when first needed, so a program that never projects a window pays nothing.</summary>
    public IWindowCapture WindowCaptureService
    {
        get => _windowCapture ??= new WindowCapture();
        set => _windowCapture = value;
    }

    /// <summary>The list of windows the operator picks from.</summary>
    public IWindowPickerDialog WindowPicker { get; set; } = new WindowPickerDialogService();

    /// <summary>True while another program's window is on the screen in motion.</summary>
    [ObservableProperty]
    private bool _isWindowLive;

    /// <summary>Whatever takes the background from the window (or clears it) ends the capture: nothing is left drawing for nobody.</summary>
    partial void OnIsWindowLiveChanged(bool value)
    {
        if (!value)
            WindowCaptureService.Stop();
    }

    /// <summary>The «🪟 Ventana» button: choose a window and put it on the screen.</summary>
    [RelayCommand]
    private async Task OpenWindowPicker()
    {
        var capture = WindowCaptureService;
        if (!capture.IsSupported)
        {
            MessageBox.Show(
                "Esta versión de Windows no permite proyectar otra ventana.\n\n" +
                "Hace falta Windows 10 versión 1903 (de mayo de 2019) o más nueva. " +
                "Si tu Windows es más nuevo y ves este mensaje, actualizá los controladores de video.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText = "Esta versión de Windows no permite proyectar otra ventana.";
            return;
        }

        var showCursor = _settings.Get(WindowCursorKey) == "1";
        var pick = WindowPicker.Pick(capture, showCursor);
        if (pick is null)
            return;

        if (pick.ShowCursor != showCursor)
            _settings.Set(WindowCursorKey, pick.ShowCursor ? "1" : "0");

        await ProjectWindowAsync(pick);
    }

    /// <summary>Frees the capture when the program closes (it holds the video card's resources).</summary>
    public void StopWindowCapture() => _windowCapture?.Stop();

    private async Task ProjectWindowAsync(WindowPick pick)
    {
        HookWindowEvents();
        var capture = WindowCaptureService;

        string title;
        string path;
        if (pick.Window is { } window)
        {
            NoteAction($"Proyectar en vivo la ventana «{window.Title}»");
            if (!capture.Start(window, pick.ShowCursor, out var error))
            {
                StatusText = error ?? "No se pudo proyectar esa ventana.";
                return;
            }

            title = window.Title;
            path = WindowTarget.Format(window.ProcessName, window.Title);
        }
        else
        {
            NoteAction("Proyectar en vivo lo que se elija en el selector de Windows");
            var owner = Application.Current.MainWindow is { } main ? new WindowInteropHelper(main).Handle : 0;
            var (name, error) = await capture.StartFromPickerAsync(owner, pick.ShowCursor);
            if (name is null)
            {
                if (error is not null)
                    StatusText = error;
                return;
            }

            title = name;
            // There is no program to look for again: this one lives only as long as the capture does.
            path = WindowTarget.Format("selector-de-windows", name);
        }

        ApplyBackgroundCore(new MediaItem
        {
            Id = WindowLiveId,
            Name = title,
            Path = path,
            Type = MediaType.Window,
            // A window is read, not decorated: all of it has to show, and over
            // the lyrics, like an announcement.
            Scaling = MediaScaling.Fit,
            Behavior = MediaBehavior.Foreground,
            EndBehavior = VideoEndBehavior.Stop,
        });
        LiveTitle = $"🪟 {title} (en vivo)";
        StatusText = $"«{title}» en vivo: la pantalla muestra esa ventana. Para dejar de proyectarla, sacá el fondo (F4) o poné otro.";
    }

    private void HookWindowEvents()
    {
        if (_windowEventsHooked)
            return;

        _windowEventsHooked = true;
        var capture = WindowCaptureService;

        // The window was closed under the capture: the screen goes back to what was behind it.
        capture.Ended += (_, _) =>
        {
            if (!IsWindowLive)
                return;

            var name = _presentation.Background?.Name ?? "la ventana";
            ClearBackground();
            StatusText = $"Se cerró «{name}»: se dejó de proyectar.";
        };

        // A minimized window stops sending pictures: the last one stays.
        capture.MinimizedChanged += (_, _) =>
        {
            if (!IsWindowLive)
                return;

            var name = _presentation.Background?.Name ?? "La ventana";
            StatusText = capture.IsMinimized
                ? $"«{name}» está minimizada: en la pantalla queda el último cuadro. Restaurala para que siga en vivo."
                : $"«{name}» volvió: sigue en vivo.";
        };
    }
}
