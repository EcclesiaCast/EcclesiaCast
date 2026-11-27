using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.ScreenSamples;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>The keys of the windows of songs, Bibles and the library (batch 16).</summary>
public sealed partial class DialogKeyTests
{
    private static T Named<T>(Window window, string name) where T : Control => window.FindControl<T>(name)!;

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_help_with_the_bible_formats_closes_with_escape(bool insideAnExample)
    {
        var window = ScreenBenchTests.HelpWindow();
        var closed = Open(window);
        if (insideAnExample)
            window.GetVisualDescendants().OfType<TextBox>().First().Focus();

        Hit(window, PhysicalKey.Enter);
        Assert.False(closed());

        Hit(window, PhysicalKey.Escape);
        Assert.True(closed());
        Assert.False(window.ImportRequested);
    }

    [AvaloniaFact]
    public void The_list_of_bibles_enter_does_nothing_and_escape_cancels()
    {
        var window = new BiblePackageWindow(BiblePackageDialog.ForExport(LibrarySamples.InstalledVersions()));
        var closed = Open(window);
        window.GetVisualDescendants().OfType<CheckBox>().First().Focus();

        Hit(window, PhysicalKey.Enter);
        // As in WPF, where no button is the default.
        Assert.False(closed());

        Hit(window, PhysicalKey.Escape);
        Assert.True(closed());
        Assert.False(window.Accepted);
    }

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
