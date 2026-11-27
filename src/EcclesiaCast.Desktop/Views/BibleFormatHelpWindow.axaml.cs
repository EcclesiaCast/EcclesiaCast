using Avalonia.Controls;
using Avalonia.Interactivity;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// For whoever brings their own Bible: the formats, their examples and the
/// instructions for an AI chat. What the buttons do is in
/// <see cref="BibleFormatHelpViewModel"/>.
/// </summary>
public sealed partial class BibleFormatHelpWindow : Window
{
    /// <summary>The system's title bar, which a window's height here leaves out.</summary>
    private const double TitleBar = 40;

    private readonly BibleFormatHelpViewModel _viewModel;

    public BibleFormatHelpWindow(BibleFormatHelpViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // Never taller than the screen: with three formats it does not fit on
        // a notebook, and the buttons at the bottom would be out of reach.
        if ((Screens.ScreenFromWindow(this) ?? Screens.Primary) is { } screen)
            MaxHeight = HeightLimit(MaxHeight, screen.WorkingArea.Height, screen.Scaling);
    }

    /// <summary>The tallest the window can be on a screen with this working area, given in pixels.</summary>
    internal static double HeightLimit(double tallest, int workingPixels, double scaling) =>
        scaling > 0 && workingPixels > 0
            ? Math.Min(tallest, workingPixels / scaling - 16 - TitleBar)
            : tallest;

    /// <summary>The operator pressed «Importar una Biblia…»: the caller opens the import.</summary>
    public bool ImportRequested => _viewModel.ImportRequested;

    private void SaveJson_Click(object? sender, RoutedEventArgs e) => _viewModel.SaveJson();

    private void SaveXml_Click(object? sender, RoutedEventArgs e) => _viewModel.SaveZefania();

    private void SaveOpenSong_Click(object? sender, RoutedEventArgs e) => _viewModel.SaveOpenSong();

    private void CopyInstructions_Click(object? sender, RoutedEventArgs e) => _viewModel.CopyInstructions();

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    private void Import_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.RequestImport();
        Close();
    }
}
