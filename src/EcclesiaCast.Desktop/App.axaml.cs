using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using EcclesiaCast.Desktop.Platform;
using EcclesiaCast.Desktop.Services;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EcclesiaCast.Desktop;

/// <summary>
/// Puts the program together, as <c>App.xaml.cs</c> does in WPF: the loading
/// screen first, then the library and the services (<see cref="Composition"/>),
/// then the operator's window. Closing that window quits: an output left on
/// its own is dangerous in a projection program (plan, section 13).
/// </summary>
public sealed partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            desktop.MainWindow = Open();
            desktop.Exit += (_, _) => ShutDown();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private TestOperatorWindow Open()
    {
        // The loading screen goes up first. Avalonia lists the screens
        // through a window, and the view model reads them when it is made.
        var splash = new SplashWindow();
        splash.Show();

        var screens = new AvaloniaDisplayProvider();
        screens.Attach(splash);
        var operatorScreen = new AvaloniaOperatorScreen();
        var notices = new AvaloniaNotices();
        var video = VlcEngine.Start();

        var platform = DesktopPlatform.ForThisSystem(screens, operatorScreen, notices, video);
        platform.Power.Start();

        var dbPath = Composition.PrepareLibrary(platform.Paths.DataFolder);
        _services = Composition.Build(platform, dbPath);
        var vm = _services.GetRequiredService<MainViewModel>();
        Composition.Start(_services);

        var main = new TestOperatorWindow(vm);
        screens.Attach(main);
        operatorScreen.Window = main;
        _services.GetRequiredService<AvaloniaClipboardText>().Window = main;
        _services.GetRequiredService<AvaloniaKeyboardState>().Track(main);
        main.Opened += (_, _) => splash.Close();

        // An error while the program is in use: a line in the status bar and
        // in the log, and the program goes on (as in WPF).
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            Log.Error(e.Exception, "Error sin atrapar");
            vm.StatusText = "Hubo un error; el programa sigue. Quedó anotado en el registro.";
            e.Handled = true;
        };

        Log.Information("Ventana del operador lista");
        return main;
    }

    private void ShutDown()
    {
        if (_services is null)
            return;

        // Stop listening on the network before the process goes away.
        var vm = _services.GetService<MainViewModel>();
        vm?.StopRemote();
        vm?.StopAudio();
        vm?.ShutDownListening();
        _services.GetService<IPowerGuard>()?.EndProjecting();
        _services.Dispose();
        _services = null;
    }
}
