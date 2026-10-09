using System.Windows;
using EcclesiaCast.Core.Updates;
using Serilog;

namespace EcclesiaCast.App.Views;

/// <summary>
/// The notice of a new version, with what it brings and three ways out:
/// update now, later, or skip this version. «Actualizar ahora» does not close
/// it: the same window shows the download, and the error if something fails,
/// so the operator is never left wondering whether anything is happening.
///
/// It is not modal. Updating is not urgent, and the operator may be in the
/// middle of preparing the service; the main window keeps working behind it.
/// </summary>
public partial class UpdateWindow : Window
{
    private readonly UpdateOffer _offer;
    private readonly UpdateInstaller _installer;
    private readonly Func<bool> _isProjecting;
    private CancellationTokenSource? _cancellation;
    private bool _working;

    public UpdateWindow(UpdateOffer offer, string installedVersion, UpdateInstaller installer, Func<bool> isProjecting)
    {
        InitializeComponent();
        _offer = offer;
        _installer = installer;
        _isProjecting = isProjecting;

        VersionsText.Text = $"Tenés la {installedVersion} · La nueva es la {offer.Version}";
        NotesText.Text = string.IsNullOrWhiteSpace(offer.Notes)
            ? "Esta versión no trae una lista de cambios."
            : offer.Notes;
        ShowOffer();
    }

    /// <summary>Raised when the operator chooses «Saltar esta versión».</summary>
    public event EventHandler? SkipRequested;

    // ── Los estados de la ventana ────────────────────────────────

    /// <summary>The notice as it first appears: the notes and the three buttons.</summary>
    public void ShowOffer()
    {
        _working = false;
        TitleText.Text = "Hay una versión nueva de EcclesiaCast";
        WorkPanel.Visibility = Visibility.Collapsed;
        ErrorText.Visibility = Visibility.Collapsed;
        SkipButton.Visibility = Visibility.Visible;
        LaterButton.Content = "Más tarde";
        LaterButton.Visibility = Visibility.Visible;
        LaterButton.IsCancel = true;
        UpdateButton.Content = "Actualizar ahora";
        UpdateButton.Visibility = Visibility.Visible;
        UpdateButton.IsDefault = true;
        CancelButton.Visibility = Visibility.Collapsed;
    }

    /// <summary>The bar and the sentence under the notes while the update is being prepared. -1 for a bar with no end.</summary>
    public void ShowProgress(string message, double percent)
    {
        _working = true;
        WorkPanel.Visibility = Visibility.Visible;
        ErrorText.Visibility = Visibility.Collapsed;
        StatusText.Text = message;
        Progress.IsIndeterminate = percent < 0;
        Progress.Value = Math.Max(percent, 0);

        SkipButton.Visibility = Visibility.Collapsed;
        LaterButton.Visibility = Visibility.Collapsed;
        LaterButton.IsCancel = false;
        UpdateButton.Visibility = Visibility.Collapsed;
        UpdateButton.IsDefault = false;
        CancelButton.Visibility = Visibility.Visible;
    }

    /// <summary>What went wrong, in words, and the button to try again.</summary>
    public void ShowError(string message)
    {
        _working = false;
        WorkPanel.Visibility = Visibility.Collapsed;
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;

        SkipButton.Visibility = Visibility.Collapsed;
        LaterButton.Content = "Cerrar";
        LaterButton.Visibility = Visibility.Visible;
        LaterButton.IsCancel = true;
        UpdateButton.Content = "Reintentar";
        UpdateButton.Visibility = Visibility.Visible;
        UpdateButton.IsDefault = true;
        CancelButton.Visibility = Visibility.Collapsed;
    }

    // ── Los botones ──────────────────────────────────────────────

    private void Later_Click(object sender, RoutedEventArgs e) => Close();

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        SkipRequested?.Invoke(this, EventArgs.Empty);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => _cancellation?.Cancel();

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmClosingProjection())
            return;

        await UpdateAsync();
    }

    /// <summary>
    /// Updating closes the program, and with it the output. Whoever is
    /// projecting is asked first, however they got here.
    /// </summary>
    private bool ConfirmClosingProjection() =>
        !_isProjecting()
        || MessageBox.Show(
            this,
            "Se va a cerrar la proyección para actualizar. ¿Seguimos?",
            "Actualizar EcclesiaCast", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
            == MessageBoxResult.Yes;

    private async Task UpdateAsync()
    {
        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();
        var cancellation = _cancellation.Token;

        ShowProgress("Preparando la descarga…", -1);
        try
        {
            var path = await _installer.PrepareAsync(_offer, new Progress<UpdateProgress>(OnProgress), cancellation);

            cancellation.ThrowIfCancellationRequested();

            // The download can take minutes: the output may have gone on
            // meanwhile, and it must not be closed without asking.
            if (_isProjecting() && !ConfirmClosingProjection())
            {
                ShowOffer();
                return;
            }

            ShowProgress("Instalando. EcclesiaCast se cierra y se vuelve a abrir solo…", -1);
            Log.Information("Instalando la versión {Version} desde {Path}", _offer.Version, path);
            _installer.Launch(path);

            // The installer is running and needs the program's files.
            Application.Current.Shutdown();
        }
        catch (OperationCanceledException)
        {
            Log.Information("Se canceló la actualización a {Version}", _offer.Version);
            ShowOffer();
        }
        catch (UpdateException ex)
        {
            Log.Warning(ex, "No se pudo actualizar a {Version} ({Step})", _offer.Version, ex.Failure);
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falló la actualización a {Version}", _offer.Version);
            ShowError("Ocurrió un error inesperado al actualizar y no se instaló nada. Probá de nuevo.");
        }
    }

    private void OnProgress(UpdateProgress progress)
    {
        switch (progress.Step)
        {
            case UpdateStep.Downloading when progress.Percent >= 0:
                ShowProgress($"Descargando la actualización… {progress.Percent:0}%", progress.Percent);
                break;
            case UpdateStep.Downloading:
                ShowProgress("Descargando la actualización…", -1);
                break;
            case UpdateStep.Verifying:
                ShowProgress("Comprobando que el archivo descargado esté bien…", -1);
                break;
            case UpdateStep.BackingUp:
                ShowProgress("Guardando una copia de tu biblioteca por las dudas…", -1);
                break;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // Closing the window in the middle of a download is cancelling it.
        if (_working)
            _cancellation?.Cancel();
        base.OnClosed(e);
    }
}
