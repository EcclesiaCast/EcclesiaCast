using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Capture;

namespace EcclesiaCast.App.Views;

/// <summary>
/// The list of open windows the operator can put on the screen. It only
/// chooses: starting the capture is up to whoever opened it.
/// </summary>
public partial class WindowPickerWindow : Window
{
    private readonly IWindowCapture _capture;

    public WindowPickerWindow(IWindowCapture capture, bool showCursor)
    {
        InitializeComponent();
        _capture = capture;
        CursorBox.IsChecked = showCursor;
        Reload();
    }

    /// <summary>The window picked in the list; null when «Elegir con la ventana de Windows…» was used.</summary>
    public RawWindow? Chosen { get; private set; }

    /// <summary>The operator chose to use Windows' own picker instead.</summary>
    public bool UseWindowsPicker { get; private set; }

    public bool ShowCursor => CursorBox.IsChecked == true;

    /// <summary>«Guardar en medios»: keep the picked window as a card instead of projecting it.</summary>
    public bool SaveToMedia { get; private set; }

    private void Reload()
    {
        var previous = (WindowList.SelectedItem as WindowChoice)?.Window.Handle;
        var icons = new Dictionary<int, ImageSource?>();
        var choices = _capture.ListWindows()
            .Select(w =>
            {
                if (!icons.TryGetValue(w.ProcessId, out var icon))
                    icons[w.ProcessId] = icon = WindowNative.IconOf(w.ProcessId);
                return new WindowChoice(w, icon);
            })
            .ToList();

        WindowList.ItemsSource = choices;
        WindowList.SelectedItem = choices.FirstOrDefault(c => c.Window.Handle == previous);
        EmptyText.Visibility = choices.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        SetPickEnabled();
    }

    private void SetPickEnabled() =>
        ProjectButton.IsEnabled = SaveButton.IsEnabled = WindowList.SelectedItem is not null;

    private void Refresh_Click(object sender, RoutedEventArgs e) => Reload();

    private void WindowList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) =>
        SetPickEnabled();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (WindowList.SelectedItem is not WindowChoice choice)
            return;

        Chosen = choice.Window;
        SaveToMedia = true;
        DialogResult = true;
    }

    // A MouseBinding never fires on a ListBox row (it keeps the click), so the
    // double click is read from the first press.
    private void WindowList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || FindRow(e.OriginalSource as DependencyObject) is not { } row)
            return;

        WindowList.SelectedItem = row;
        Project_Click(sender, e);
        e.Handled = true;
    }

    private static WindowChoice? FindRow(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is FrameworkElement { DataContext: WindowChoice choice } and not System.Windows.Controls.ListBox)
                return choice;
            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private void Project_Click(object sender, RoutedEventArgs e)
    {
        if (WindowList.SelectedItem is not WindowChoice choice)
            return;

        Chosen = choice.Window;
        DialogResult = true;
    }

    private void UseWindowsPicker_Click(object sender, RoutedEventArgs e)
    {
        UseWindowsPicker = true;
        DialogResult = true;
    }

    /// <summary>One row of the list.</summary>
    private sealed record WindowChoice(RawWindow Window, ImageSource? Icon)
    {
        public string Title => Window.Title;

        public string ProcessLabel => Window.ProcessName;
    }
}
