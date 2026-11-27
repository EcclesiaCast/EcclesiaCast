using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EcclesiaCast.Desktop.Services;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// All the preferences in one place, sections on the left and their content on
/// the right. It holds no state of its own: every control is bound to the same
/// properties and commands the menus use; which sections there are and the
/// texts that name the system come from <see cref="SettingsViewModel"/>.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly UiModeChoice _uiMode;
    private readonly IColorPicker _colors;
    private readonly MenuDialogs _menus;
    private readonly Action? _openListening;

    public SettingsWindow(
        SettingsViewModel viewModel, UiModeChoice uiMode, IColorPicker colors, MenuDialogs menus,
        Action? openListening = null)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _uiMode = uiMode;
        _colors = colors;
        _menus = menus;
        _openListening = openListening;
        DataContext = viewModel.Main;

        ListeningPill.IsVisible = viewModel.ShowFollowSong;
        UpdateCheck.IsVisible = viewModel.ShowUpdateCheck;
        UpdatesPill.Content = viewModel.UpdatesTitle;
        UpdatesTitle.Text = viewModel.UpdatesTitle;
        SoundText.Text = viewModel.SoundText;
        LearningRadio.IsChecked = !uiMode.IsAdvanced;
        AdvancedRadio.IsChecked = uiMode.IsAdvanced;

        AddHandler(KeyDownEvent, Enter_KeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Enter is «Cerrar», as in WPF. Here a pill, a box to tick or a closed
    /// list would keep the key for themselves.
    /// </summary>
    private void Enter_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter
            || FocusManager?.GetFocusedElement() is not (RadioButton or CheckBox or ComboBox { IsDropDownOpen: false }))
            return;

        e.Handled = true;
        Close();
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    private void RestartSound_Click(object? sender, RoutedEventArgs e) => _viewModel.Main.RestartSound();

    private void Listening_Click(object? sender, RoutedEventArgs e)
    {
        // Its window is not modal and belongs to the main one: this one
        // closes so it is not left underneath.
        Close();
        _openListening?.Invoke();
    }

    private void Transitions_Click(object? sender, RoutedEventArgs e) => _menus.ShowTransitions();

    private void NewVideoColor_Click(object? sender, RoutedEventArgs e) => _viewModel.PickNewVideoColor(_colors);

    private void LearningRadio_Click(object? sender, RoutedEventArgs e) => _uiMode.Set(advanced: false);

    private void AdvancedRadio_Click(object? sender, RoutedEventArgs e) => _uiMode.Set(advanced: true);
}
