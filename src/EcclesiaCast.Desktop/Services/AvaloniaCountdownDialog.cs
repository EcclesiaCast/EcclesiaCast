using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Services;

public sealed class AvaloniaCountdownDialog : ICountdownDialog
{
    public CountdownChoice? Show(CountdownSettings settings, bool isRunning)
    {
        var countdown = new CountdownViewModel(settings, isRunning);
        var window = new CountdownWindow(countdown);
        Modal.Run(window);
        return window.Accepted ? countdown.Choice : null;
    }
}
