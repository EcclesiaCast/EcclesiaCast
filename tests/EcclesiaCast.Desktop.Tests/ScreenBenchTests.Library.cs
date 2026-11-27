using Avalonia.Headless.XUnit;
using EcclesiaCast.Desktop.Views;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// The windows of songs, Bibles and the library (batch 16), with the same
/// names and the same made-up data as in the WPF bench (LibraryScreens.cs).
/// </summary>
public sealed partial class ScreenBenchTests
{
    [AvaloniaFact]
    public void Importing_a_bible_is_drawn() => DrawWindow(new BibleImportWindow("Mi Biblia"), "importar-biblia");
}
