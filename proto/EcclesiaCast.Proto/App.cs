using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Serilog;

namespace EcclesiaCast.Proto;

public sealed class App : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = ThemeVariant.Light;
        Name = "EcclesiaCast Proto";
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Closing the operator's window quits, as the plan asks for a
            // projection program (an output left alone is dangerous).
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

            ProtoData data;
            try
            {
                data = ProtoData.Open(ProtoData.DefaultFolder(), ProtoData.BiblesFolder());
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "No se pudo abrir la biblioteca del prototipo");
                throw;
            }

            var main = new OperatorWindow(data);
            desktop.MainWindow = main;
            desktop.Exit += (_, _) => main.ShutDown();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
