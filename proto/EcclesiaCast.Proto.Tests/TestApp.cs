using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using EcclesiaCast.Proto.Tests;

[assembly: AvaloniaTestApplication(typeof(TestApp))]

namespace EcclesiaCast.Proto.Tests;

/// <summary>
/// Headless Avalonia with real Skia drawing (not the headless stand-in), so
/// a rendered frame can be captured and saved.
/// </summary>
public sealed class TestApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
