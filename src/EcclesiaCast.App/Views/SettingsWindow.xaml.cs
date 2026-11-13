using System.Windows;

namespace EcclesiaCast.App.Views;

/// <summary>
/// All the preferences in one place, sections on the left and their content on
/// the right. It holds no state of its own: every control is bound to the same
/// properties and commands the menus use. To add a section, add a pill and a
/// panel in the XAML (one panel per section).
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly MainWindow _main;

    public SettingsWindow(MainWindow main)
    {
        InitializeComponent();
        _main = main;
        LearningRadio.IsChecked = !main.IsAdvancedUi;
        AdvancedRadio.IsChecked = main.IsAdvancedUi;
    }

    // La ventana se abre sin ShowDialog: IsCancel sola no la cierra.
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void RestartSound_Click(object sender, RoutedEventArgs e) => _main.RestartSound();

    private void Listening_Click(object sender, RoutedEventArgs e)
    {
        // Its window is not modal and belongs to the main one: this one
        // closes so it is not left underneath.
        Close();
        _main.OpenListening();
    }

    private void Transitions_Click(object sender, RoutedEventArgs e) => _main.OpenTransitions(this);

    private void LearningRadio_Click(object sender, RoutedEventArgs e) => _main.SetUiMode(advanced: false);

    private void AdvancedRadio_Click(object sender, RoutedEventArgs e) => _main.SetUiMode(advanced: true);
}
