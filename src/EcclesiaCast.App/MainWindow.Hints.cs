using System.Windows;
using EcclesiaCast.Core.Ui;

namespace EcclesiaCast.App;

/// <summary>
/// The learning layout (the default, and what the program always looked like)
/// writes the keyboard keys on the buttons and keeps the help texts in view.
/// The advanced layout takes them away, so the operator sees less text. Only
/// what is written changes: the keys themselves work in both.
/// </summary>
public partial class MainWindow
{
    private void RestoreUiMode() =>
        ApplyUiMode(OperatorLayout.IsAdvanced(_settings?.Get(OperatorLayout.SettingKey)));

    /// <summary>Changes the layout and remembers the choice for the next service.</summary>
    private void SetUiMode(bool advanced)
    {
        ApplyUiMode(advanced);
        _settings?.Set(OperatorLayout.SettingKey, OperatorLayout.ToSetting(advanced));
    }

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
