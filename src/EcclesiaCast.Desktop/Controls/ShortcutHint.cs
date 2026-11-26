using Avalonia;
using Avalonia.Controls;

namespace EcclesiaCast.Desktop.Controls;

/// <summary>
/// The shortcut a button shows next to its text in the learning layout
/// (controls:ShortcutHint.Text="F1"), as in WPF. The DarkButton look draws it.
/// </summary>
public sealed class ShortcutHint
{
    private ShortcutHint()
    {
    }

    public static readonly AttachedProperty<string> TextProperty =
        AvaloniaProperty.RegisterAttached<ShortcutHint, Control, string>("Text", string.Empty);

    public static string GetText(Control element) => element.GetValue(TextProperty);

    public static void SetText(Control element, string value) => element.SetValue(TextProperty, value);
}
