using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using EcclesiaCast.Desktop.Tests;

[assembly: AvaloniaTestApplication(typeof(TestApp))]

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// Headless Avalonia with real Skia drawing (not the headless stand-in): the
/// letters are measured by the same engine as on the screen, and a frame can
/// be captured and saved. Nothing is shown.
/// </summary>
public sealed class TestApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
