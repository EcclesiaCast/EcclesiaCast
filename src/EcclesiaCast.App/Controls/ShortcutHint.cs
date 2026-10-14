using System.Windows;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// The key that does the same as a button, written after its text («Black»
/// with «F1» beside it). It is only drawn in the learning layout: the button
/// template reads this and the layout switch in MainWindow.Hints.cs hides it.
/// A button without one keeps the size it always had: the template collapses
/// the empty hint.
/// </summary>
public static class ShortcutHint
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached("Text", typeof(string), typeof(ShortcutHint),
            new PropertyMetadata(string.Empty));

    public static string GetText(DependencyObject element) => (string)element.GetValue(TextProperty);

    public static void SetText(DependencyObject element, string value) => element.SetValue(TextProperty, value);
}
