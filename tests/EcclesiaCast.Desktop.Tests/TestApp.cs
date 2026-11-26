using Avalonia;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using EcclesiaCast.Desktop.Styles;
using EcclesiaCast.Desktop.Tests;

[assembly: AvaloniaTestApplication(typeof(TestApp))]

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// Headless Avalonia with real Skia drawing (not the headless stand-in): the
/// letters are measured by the same engine as on the screen, and a frame can
/// be captured and saved. Nothing is shown. The looks are the program's own
/// (App.axaml): Fluent in dark, then the program's theme.
/// </summary>
public sealed class TestApp : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.Add(new FluentTheme());
        Styles.Add(new EcclesiaCastTheme());
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
