using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using EcclesiaCast.Desktop.Views;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>The keys of the windows of songs, Bibles and the library (batch 16).</summary>
public sealed partial class DialogKeyTests
{
    private static T Named<T>(Window window, string name) where T : Control => window.FindControl<T>(name)!;

    [AvaloniaTheory]
    [InlineData("NameBox")]
    [InlineData("AbbreviationBox")]
    [InlineData("LanguageBox")]
    public void Importing_a_bible_enter_does_nothing_and_escape_cancels(string box)
    {
        var window = new BibleImportWindow("Mi Biblia");
        var closed = Open(window);
        Named<TextBox>(window, "AbbreviationBox").Text = "MB";
        Named<TextBox>(window, box).Focus();

        Hit(window, PhysicalKey.Enter);
        // As in WPF, where «Importar» is not the default button.
        Assert.False(closed());

        Hit(window, PhysicalKey.Escape);
        Assert.True(closed());
        Assert.Null(window.Result);
    }
}
