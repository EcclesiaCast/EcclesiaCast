using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// Brings songs over from another program (ProPresenter, EasyWorship or
/// Holyrics). What is looked for, what is listed and how it is imported is in
/// <see cref="SongImportViewModel"/>; this window places the controls and
/// asks for files and folders.
/// </summary>
public sealed partial class SongImportWindow : Window
{
    private readonly SongImportViewModel _viewModel;
    private readonly IDialogs _dialogs;
    private readonly IShell _shell;

    public SongImportWindow(SongImportViewModel viewModel, IDialogs dialogs, IShell shell)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _dialogs = dialogs;
        _shell = shell;
        OpenFolderButton.Content = _viewModel.OpenFolderText;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Posted, so the window is on the screen, saying «Buscando…», while it looks.
        Opened += (_, _) => Dispatcher.UIThread.Post(DiscoverAll);
    }

    /// <summary>True when the import ran to its end (WPF's DialogResult).</summary>
    public bool Accepted { get; private set; }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // The operator cannot close it half way through an import; the program closing can.
        if (!_viewModel.CanClose && e.CloseReason == WindowCloseReason.WindowClosing)
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
        ToolTip.SetTip(BrowseButton, _viewModel.BrowseTip.Length > 0 ? _viewModel.BrowseTip : null);
        CloudWarning.IsVisible = _viewModel.CloudWarningVisible;
        CloudWarningText.Text = _viewModel.CloudWarningText;

        if (_viewModel.IsImporting)
        {
            CancelButton.IsEnabled = false;
            BrowseButton.IsEnabled = false;
            ProPresenterChoice.IsEnabled = false;
            EasyWorshipChoice.IsEnabled = false;
            HolyricsChoice.IsEnabled = false;
            HolyricsFiles.IsEnabled = false;
            Progress.IsVisible = true;
            ProgressText.IsVisible = true;
            ProgressText.Text = _viewModel.ProgressText;
            Progress.IsIndeterminate = _viewModel.ProgressIndeterminate;
            Progress.Maximum = _viewModel.ProgressMaximum;
            Progress.Value = _viewModel.ProgressValue;
        }
    }

    /// <summary>Opens on the Holyrics choice (the Importar canciones tutorial shows its «Abrir archivos…»).</summary>
    public void ChooseHolyrics() => HolyricsChoice.IsChecked = true;

    private void DiscoverAll()
    {
        var first = _viewModel.DiscoverAll();
        ShowLabels();
        Choice(first).IsChecked = true;
    }

    private RadioButton Choice(SongImportSource source) => source switch
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

    private void Source_Checked(object? sender, RoutedEventArgs e)
    {
        if (!_viewModel.Discovered || sender is not RadioButton { IsChecked: true })
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

        ListTitle.Text = _viewModel.ListTitle;
        HolyricsPanel.IsVisible = _viewModel.IsHolyrics;
        BrowseButton.IsVisible = !_viewModel.IsHolyrics;
        ShowState();
    }

    private void HolyricsFiles_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.IsImporting || _dialogs.OpenFiles(_viewModel.HolyricsFilesRequest()) is not { } files)
            return;

        _viewModel.ReadHolyricsFiles(files);
        ShowLabels();
        ShowList();
    }

    private void Browse_Click(object? sender, RoutedEventArgs e)
    {
        if (_dialogs.PickFolder(_viewModel.BrowseTitle) is not { } folder || !_viewModel.Browse(folder))
            return;

        ShowLabels();
        ShowList();
    }

    /// <summary>Opens the first library that has undownloaded songs.</summary>
    private void OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.FolderToOpen() is { } folder)
            _shell.OpenFolder(folder);
    }

    private void CheckAll_Click(object? sender, RoutedEventArgs e) => _viewModel.SetAll(true);

    private void CheckNone_Click(object? sender, RoutedEventArgs e) => _viewModel.SetAll(false);

    private async void Import_Click(object? sender, RoutedEventArgs e)
    {
        // A second click can be waiting its turn from before the button went off.
        if (_viewModel.IsImporting)
            return;

        if (await _viewModel.ImportAsync(
                async () => await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background)))
        {
            Accepted = true;
            Close();
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}
