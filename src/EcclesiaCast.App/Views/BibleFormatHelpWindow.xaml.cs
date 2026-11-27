using System.Windows;
using EcclesiaCast.App.Platform;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// For whoever brings their own Bible: the formats, their examples and the
/// instructions for an AI chat. What the buttons do is in
/// <see cref="BibleFormatHelpViewModel"/>.
/// </summary>
public partial class BibleFormatHelpWindow : Window
{
    private readonly BibleFormatHelpViewModel _viewModel =
        new(new WpfDialogs(), new WpfClipboardText(), PlatformSystem.Windows);

    public BibleFormatHelpWindow()
    {
        InitializeComponent();

        // Never taller than the screen: with three formats it no longer fits on
        // a notebook, and the buttons at the bottom were left out of reach.
        MaxHeight = Math.Min(MaxHeight, SystemParameters.WorkArea.Height - 16);

        JsonPreview.Text = _viewModel.JsonPreview;
        XmlPreview.Text = _viewModel.XmlPreview;
        OpenSongPreview.Text = _viewModel.OpenSongPreview;
        _viewModel.PropertyChanged += (_, _) => CopiedText.Text = _viewModel.Status;
    }

    /// <summary>The operator pressed «Importar una Biblia…»: the caller opens the import.</summary>
    public bool ImportRequested => _viewModel.ImportRequested;

    private void SaveJson_Click(object sender, RoutedEventArgs e) => _viewModel.SaveJson();

    private void SaveXml_Click(object sender, RoutedEventArgs e) => _viewModel.SaveZefania();

    private void SaveOpenSong_Click(object sender, RoutedEventArgs e) => _viewModel.SaveOpenSong();

    private void CopyInstructions_Click(object sender, RoutedEventArgs e) => _viewModel.CopyInstructions();

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.RequestImport();
        Close();
    }
}
