using System.ComponentModel;
using System.Windows;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Brings songs over from another program (ProPresenter, EasyWorship or
/// Holyrics). What is looked for, what is listed and how it is imported is in
/// <see cref="SongImportViewModel"/>; this window places the controls and
/// asks for files and folders.
/// </summary>
public partial class SongImportWindow : Window
{
    private readonly SongImportViewModel _viewModel;

    public SongImportWindow(SongImportViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        Loaded += (_, _) => DiscoverAll();
    }

    /// <summary>
    /// The X (or Alt+F4) cannot close it half way through an import: the songs
    /// would go on being saved out of sight and its end would find no window.
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_viewModel.CanClose)
            e.Cancel = true;

        base.OnClosing(e);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) => ShowState();

    /// <summary>The texts and buttons follow the view model.</summary>
    private void ShowState()
    {
        HeaderText.Text = _viewModel.HeaderText;
        RootText.Text = _viewModel.RootText;
        ImportButton.IsEnabled = _viewModel.ImportEnabled;
        BrowseButton.ToolTip = _viewModel.BrowseTip.Length > 0 ? _viewModel.BrowseTip : null;
        CloudWarning.Visibility = _viewModel.CloudWarningVisible ? Visibility.Visible : Visibility.Collapsed;
        CloudWarningText.Text = _viewModel.CloudWarningText;

        if (_viewModel.IsImporting)
        {
            CancelButton.IsEnabled = false;
            BrowseButton.IsEnabled = false;
            ProPresenterChoice.IsEnabled = false;
            EasyWorshipChoice.IsEnabled = false;
            HolyricsChoice.IsEnabled = false;
            HolyricsFiles.IsEnabled = false;
            Progress.Visibility = Visibility.Visible;
            ProgressText.Visibility = Visibility.Visible;
            ProgressText.Text = _viewModel.ProgressText;
            Progress.IsIndeterminate = _viewModel.ProgressIndeterminate;
            Progress.Maximum = _viewModel.ProgressMaximum;
            Progress.Value = _viewModel.ProgressValue;
        }
    }

    /// <summary>Opens on the Holyrics choice (the Importar canciones tutorial shows its «Abrir archivos…»).</summary>
    public void ChooseHolyrics() => HolyricsChoice.IsChecked = true;

    /// <summary>How many songs were actually added (0 if cancelled).</summary>
    public int Imported => _viewModel.Imported;

    /// <summary>Line for the operator's status bar.</summary>
    public string Summary => _viewModel.Summary;

    // ── Descubrimiento ───────────────────────────────────────────

    private void DiscoverAll()
    {
        var first = _viewModel.DiscoverAll();
        ShowLabels();
        Choice(first).IsChecked = true;
    }

    private System.Windows.Controls.RadioButton Choice(SongImportSource source) => source switch
    {
        SongImportSource.ProPresenter => ProPresenterChoice,
        SongImportSource.EasyWorship => EasyWorshipChoice,
        _ => HolyricsChoice,
    };

    private void ShowLabels()
    {
        ProPresenterChoice.Content = _viewModel.ChoiceLabel(SongImportSource.ProPresenter);
        EasyWorshipChoice.Content = _viewModel.ChoiceLabel(SongImportSource.EasyWorship);
        HolyricsChoice.Content = _viewModel.ChoiceLabel(SongImportSource.Holyrics);
    }

    private void Source_Checked(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.Discovered)
            return;

        _viewModel.SelectSource(ReferenceEquals(sender, EasyWorshipChoice) ? SongImportSource.EasyWorship
            : ReferenceEquals(sender, HolyricsChoice) ? SongImportSource.Holyrics
            : SongImportSource.ProPresenter);
        ShowList();
    }

    /// <summary>Puts the libraries found for the chosen program in the list.</summary>
    private void ShowList()
    {
        LibrariesList.ItemsSource = null;
        LibrariesList.ItemsSource = _viewModel.Rows;

        var holyrics = _viewModel.IsHolyrics;
        ListTitle.Text = _viewModel.ListTitle;
        HolyricsPanel.Visibility = holyrics ? Visibility.Visible : Visibility.Collapsed;
        BrowseButton.Visibility = holyrics ? Visibility.Collapsed : Visibility.Visible;
        ShowState();
    }

    /// <summary>
    /// The way in for the free Holyrics: files saved from its «Guardar» or
    /// «Exportación en lote» (.json or .txt).
    /// </summary>
    private void HolyricsFiles_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.IsImporting)
            return;

        if (new Platform.WpfDialogs().OpenFiles(_viewModel.HolyricsFilesRequest()) is not { } files)
            return;

        _viewModel.ReadHolyricsFiles(files);
        ShowLabels();
        ShowList();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = _viewModel.BrowseTitle };
        if (dialog.ShowDialog() != true)
            return;

        if (!_viewModel.Browse(dialog.FolderName))
            return;

        ShowLabels();
        ShowList();
    }

    /// <summary>Opens the first library that has undownloaded songs.</summary>
    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.FolderToOpen() is not { } folder)
            return;

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true,
        });
    }

    private void CheckAll_Click(object sender, RoutedEventArgs e) => _viewModel.SetAll(true);

    private void CheckNone_Click(object sender, RoutedEventArgs e) => _viewModel.SetAll(false);

    // ── Importación ──────────────────────────────────────────────

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        if (await _viewModel.ImportAsync(
                async () => await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background)))
            DialogResult = true;
    }
}
