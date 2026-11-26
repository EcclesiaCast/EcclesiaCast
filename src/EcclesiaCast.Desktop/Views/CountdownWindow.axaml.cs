using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// Sets up the "we start in 5:00" screen. Everything it decides is in
/// <see cref="CountdownViewModel"/>.
/// </summary>
public sealed partial class CountdownWindow : Window
{
    private readonly CountdownViewModel _countdown;

    public CountdownWindow(CountdownViewModel countdown)
    {
        InitializeComponent();
        _countdown = countdown;
        DataContext = countdown;

        MinutesBox.AddHandler(TextInputEvent, Digits_Only, RoutingStrategies.Tunnel);
        ClockBox.GotFocus += (_, _) => _countdown.ByClock = true;
        Opened += (_, _) =>
        {
            MinutesBox.Focus();
            MinutesBox.SelectAll();
        };
    }

    /// <summary>True when the operator decided something: <see cref="CountdownViewModel.Choice"/> says what.</summary>
    public bool Accepted { get; private set; }

    private void Preset_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: string minutes })
            _countdown.UsePreset(minutes);
    }

    private void Digits_Only(object? sender, TextInputEventArgs e) =>
        e.Handled = !CountdownViewModel.IsDigits(e.Text ?? string.Empty);

    private void Start_Click(object? sender, RoutedEventArgs e)
    {
        if (_countdown.Start())
            Accept();
    }

    private void Stop_Click(object? sender, RoutedEventArgs e)
    {
        _countdown.Stop();
        Accept();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();

    private void Accept()
    {
        Accepted = true;
        Close();
    }
}
