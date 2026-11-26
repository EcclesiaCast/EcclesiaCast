using System.Windows;
using System.Windows.Input;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Sets up the "we start in 5:00" screen. Everything it decides is in
/// <see cref="CountdownViewModel"/>.
/// </summary>
public partial class CountdownWindow : Window
{
    private readonly CountdownViewModel _countdown;

    public CountdownWindow(CountdownViewModel countdown)
    {
        InitializeComponent();
        _countdown = countdown;
        DataContext = countdown;

        MinutesBox.Focus();
        MinutesBox.SelectAll();
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string minutes })
            _countdown.UsePreset(minutes);
    }

    private void Clock_GotFocus(object sender, RoutedEventArgs e) => _countdown.ByClock = true;

    private void Digits_Only(object sender, TextCompositionEventArgs e) =>
        e.Handled = !CountdownViewModel.IsDigits(e.Text);

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_countdown.Start())
            DialogResult = true;
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        _countdown.Stop();
        DialogResult = true;
    }
}
