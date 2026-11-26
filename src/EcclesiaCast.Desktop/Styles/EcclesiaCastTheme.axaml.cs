using Avalonia;
using Avalonia.Markup.Xaml;
using EcclesiaCast.Desktop.Platform;

namespace EcclesiaCast.Desktop.Styles;

/// <summary>
/// The program's look (EcclesiaCastTheme.axaml), loaded after Fluent by the
/// program and by the tests that draw screens in memory, so both see the
/// same thing.
/// </summary>
public sealed class EcclesiaCastTheme : Avalonia.Styling.Styles
{
    public EcclesiaCastTheme()
    {
        AvaloniaXamlLoader.Load(this);

        // Segoe UI where it is installed (Windows), Selawik (its measures,
        // bundled) where it is not: the same layout on both systems.
        Resources["UiFont"] = AvaloniaFontCatalog.Current.Resolve("Segoe UI");
    }

    /// <summary>
    /// The learning layout (shortcuts and help texts in sight) or the
    /// advanced one (without them). What WPF's MainWindow.Hints.cs does with
    /// its two resources; everything that uses them follows at once.
    /// </summary>
    public static void ShowHints(Application app, bool learning)
    {
        app.Resources["ShortcutHintVisible"] = learning;
        app.Resources["LearningHintVisible"] = learning;
    }
}
