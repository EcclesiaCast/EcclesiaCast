using System.Globalization;
using System.Windows;
using System.Windows.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Sets up the "we start in 5:00" screen. Two ways to say when, because both
/// come up: a stretch of minutes when the band finishes the sound check, and
/// a time on the clock when the service has a fixed start.
/// </summary>
public partial class CountdownWindow : Window
{
    public CountdownWindow(CountdownSettings settings, bool isRunning)
    {
        InitializeComponent();

        MinutesBox.Text = settings.Minutes.ToString(CultureInfo.InvariantCulture);
        ClockBox.Text = settings.Clock;
        HeadingBox.Text = settings.Heading;
        FinishedBox.Text = settings.FinishedMessage;
        StopButton.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;

        MinutesBox.Focus();
        MinutesBox.SelectAll();
    }

    /// <summary>The countdown to run, or null when the operator asked to stop.</summary>
    public Countdown? Result { get; private set; }

    /// <summary>What to remember for next Sunday.</summary>
    public CountdownSettings Settings { get; private set; } = CountdownSettings.Default;

    /// <summary>True when the operator pressed "stop" instead of starting one.</summary>
    public bool StopRequested { get; private set; }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string minutes })
        {
            MinutesBox.Text = minutes;
            ByMinutes.IsChecked = true;
        }
    }

    private void Clock_GotFocus(object sender, RoutedEventArgs e) => ByClock.IsChecked = true;

    private void Digits_Only(object sender, TextCompositionEventArgs e) =>
        e.Handled = !e.Text.All(char.IsDigit);

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        var heading = HeadingBox.Text.Trim();
        var finished = FinishedBox.Text.Trim();
        var now = DateTimeOffset.Now;

        if (ByClock.IsChecked == true)
        {
            if (!TimeOnly.TryParse(ClockBox.Text.Trim(), CultureInfo.CurrentCulture, out var clock))
            {
                Fail("Escribí la hora como 10:30 o 19:00.");
                return;
            }

            Result = Countdown.UntilClock(clock, now, heading, finished);
        }
        else
        {
            if (!int.TryParse(MinutesBox.Text.Trim(), out var minutes) || minutes <= 0)
            {
                Fail("Poné cuántos minutos faltan, por ejemplo 5.");
                return;
            }

            Result = Countdown.ForMinutes(minutes, now, heading, finished);
        }

        Settings = new CountdownSettings(
            int.TryParse(MinutesBox.Text.Trim(), out var saved) && saved > 0 ? saved : 5,
            ClockBox.Text.Trim(),
            heading,
            finished);

        DialogResult = true;
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        StopRequested = true;
        DialogResult = true;
    }

    private void Fail(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
