using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

public sealed class CountdownDialogService : ICountdownDialog
{
    public CountdownChoice? Show(CountdownSettings settings, bool isRunning)
    {
        var window = new CountdownWindow(settings, isRunning)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        if (window.ShowDialog() != true)
            return null;

        return new CountdownChoice(window.Result, window.Settings, window.StopRequested);
    }
}
