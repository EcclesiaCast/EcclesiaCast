using System.Windows;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.App;

/// <summary>
/// The learning layout (the default, and what the program always looked like)
/// writes the keyboard keys on the buttons and keeps the help texts in view.
/// The advanced layout takes them away, so the operator sees less text. Only
/// what is written changes: the keys themselves work in both.
/// </summary>
public partial class MainWindow
{
    private UiModeChoice? _uiMode;

    private UiModeChoice UiMode => _uiMode ??= new UiModeChoice(() => _settings, ApplyUiMode);

    /// <summary>True while the «avanzado» layout (no shortcut hints) is on.</summary>
    public bool IsAdvancedUi => UiMode.IsAdvanced;

    private void RestoreUiMode() => UiMode.Restore();

    /// <summary>Changes the layout and remembers the choice for the next service.</summary>
    public void SetUiMode(bool advanced) => UiMode.Set(advanced);

    /// <summary>
    /// The two visibilities live in the application's resources: the button
    /// template and the help texts read them as DynamicResource, so flipping
    /// them here updates every one of them at once. Only the menu ticks are
    /// set by hand.
    /// </summary>
    private void ApplyUiMode(bool advanced)
    {
        var visibility = advanced ? Visibility.Collapsed : Visibility.Visible;
        var resources = Application.Current.Resources;
        resources["ShortcutHintVisibility"] = visibility;
        resources["LearningHintVisibility"] = visibility;

        LearningModeItem.IsChecked = !advanced;
        AdvancedModeItem.IsChecked = advanced;
    }

    private void LearningMode_Click(object sender, RoutedEventArgs e) => SetUiMode(advanced: false);

    private void AdvancedMode_Click(object sender, RoutedEventArgs e) => SetUiMode(advanced: true);
}
