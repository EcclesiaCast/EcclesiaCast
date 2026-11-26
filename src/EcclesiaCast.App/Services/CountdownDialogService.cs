using EcclesiaCast.App.Views;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Services;

public sealed class CountdownDialogService : ICountdownDialog
{
    public CountdownChoice? Show(CountdownSettings settings, bool isRunning)
    {
        var countdown = new CountdownViewModel(settings, isRunning);
        var window = new CountdownWindow(countdown)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        return window.ShowDialog() == true ? countdown.Choice : null;
    }
}
