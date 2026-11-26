using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// Makes the light copy of one heavy video while the operator waits, saying
/// how far along it is. The window closes by itself when the copy is ready
/// (<see cref="CloseRequested"/>); when it can not be made it stays open with
/// the message until the operator closes it.
/// </summary>
public sealed partial class LightCopyViewModel : ObservableObject
{
    private const string FailedMessage = "No se pudo hacer la copia. Se va a proyectar el original.";

    private readonly string _name;
    private readonly Func<IProgress<double>, CancellationToken, Task<bool>> _work;
    private readonly CancellationTokenSource _cancellation = new();
    private bool _finished;
    private bool _succeeded;
    private bool _cancelled;
    private bool _closed;

    public LightCopyViewModel(string name, Func<IProgress<double>, CancellationToken, Task<bool>> work)
    {
        _name = name;
        _work = work;
        Title = $"Preparando la copia liviana de «{name}»";
    }

    /// <summary>The one that makes the real copy of the video at <paramref name="path"/>.</summary>
    public static LightCopyViewModel For(string path, string name) =>
        new(name, (progress, cancellation) => LightVideoCache.MakeCopyNowAsync(path, progress, cancellation));

    public string Title { get; }

    [ObservableProperty]
    private string _status = "Empezando… puede tardar varios minutos, según el largo del video.";

    /// <summary>Until ffmpeg reports the first position there is nothing to measure.</summary>
    [ObservableProperty]
    private bool _isIndeterminate = true;

    /// <summary>0 to 100.</summary>
    [ObservableProperty]
    private double _percent;

    [ObservableProperty]
    private string _closeText = "Cancelar";

    /// <summary>The window has to close now.</summary>
    public event Action? CloseRequested;

    /// <summary>How it ended, to be read once the window closed.</summary>
    public LightCopyResult Result =>
        _succeeded ? LightCopyResult.Done
        : _cancelled ? LightCopyResult.Cancelled
        : LightCopyResult.Failed;

    /// <summary>Does the work. Call it on the screen's thread, once the window is up.</summary>
    public async Task RunAsync()
    {
        var progress = new Progress<double>(fraction =>
        {
            IsIndeterminate = false;
            Percent = fraction * 100;
            Status = $"Preparando la copia… {Math.Round(fraction * 100):0} %";
        });

        try
        {
            if (await _work(progress, _cancellation.Token))
            {
                if (_closed)
                    return;
                _succeeded = true;
                CloseRequested?.Invoke();
                return;
            }

            Status = FailedMessage;
            Finish();
        }
        catch (OperationCanceledException)
        {
            // Closing the window with the X cancels too, and by then there is
            // no window left to close.
            _cancelled = true;
            if (!_closed)
                CloseRequested?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falló la copia liviana de {Name}", _name);
            Status = FailedMessage;
            Finish();
        }
    }

    /// <summary>The one button: «Cancelar» while it works, «Cerrar» after it failed.</summary>
    public void PressClose()
    {
        if (_finished)
            CloseRequested?.Invoke();
        else
            _cancellation.Cancel();
    }

    /// <summary>The window closed, however it did.</summary>
    public void Closed()
    {
        if (_closed)
            return;

        _closed = true;
        if (!_finished && !_succeeded)
            _cancelled = true;

        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    /// <summary>Leaves the window open with the message; the button now closes it.</summary>
    private void Finish()
    {
        _finished = true;
        IsIndeterminate = false;
        CloseText = "Cerrar";
    }
}
