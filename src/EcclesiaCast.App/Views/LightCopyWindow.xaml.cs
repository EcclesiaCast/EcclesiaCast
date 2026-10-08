using System.Windows;
using EcclesiaCast.App.Services;
using Serilog;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Makes the light copy of one heavy video while the operator waits, with a
/// bar that says how far along it is. Closes by itself when the copy is ready.
/// </summary>
public partial class LightCopyWindow : Window
{
    private readonly string _path;
    private readonly string _name;
    private readonly CancellationTokenSource _cancellation = new();
    private bool _finished;
    private bool _succeeded;
    private bool _closed;

    public LightCopyWindow(string path, string name)
    {
        InitializeComponent();
        _path = path;
        _name = name;

        TitleText.Text = $"Preparando la copia liviana de «{name}»";
        Progress.IsIndeterminate = true;
        Loaded += async (_, _) => await RunAsync();
    }

    /// <summary>The operator pressed Cancelar (as opposed to the copy failing).</summary>
    public bool Cancelled { get; private set; }

    private async Task RunAsync()
    {
        // Until ffmpeg reports the first position there is nothing to measure.
        var progress = new Progress<double>(fraction =>
        {
            Progress.IsIndeterminate = false;
            Progress.Value = fraction * 100;
            StatusText.Text = $"Preparando la copia… {Math.Round(fraction * 100):0} %";
        });

        try
        {
            if (await LightVideoCache.MakeCopyNowAsync(_path, progress, _cancellation.Token))
            {
                _succeeded = true;
                DialogResult = true;
                return;
            }

            StatusText.Text = "No se pudo hacer la copia. Se va a proyectar el original.";
            Finish();
        }
        catch (OperationCanceledException)
        {
            // Closing the window with the X cancels too, and by then there is
            // no dialog left to hand a result to.
            Cancelled = true;
            if (!_closed)
                DialogResult = false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falló la copia liviana de {Name}", _name);
            StatusText.Text = "No se pudo hacer la copia. Se va a proyectar el original.";
            Finish();
        }
    }

    /// <summary>Leaves the window open with the message; the button now closes it.</summary>
    private void Finish()
    {
        _finished = true;
        Progress.IsIndeterminate = false;
        CloseButton.Content = "Cerrar";
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (_finished)
            DialogResult = false;
        else
            _cancellation.Cancel();
    }

    protected override void OnClosed(EventArgs e)
    {
        _closed = true;
        if (!_finished && !_succeeded)
            Cancelled = true;

        _cancellation.Cancel();
        _cancellation.Dispose();
        base.OnClosed(e);
    }
}
