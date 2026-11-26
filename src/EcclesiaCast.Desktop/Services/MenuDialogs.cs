using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Desktop.Services;

/// <summary>
/// The windows WPF's main window opens from its own code, without a view
/// model asking. The operator's window of Avalonia calls these for
/// <c>MenuRequest.About</c> and <c>MenuRequest.Transitions</c> (batch 19).
/// </summary>
public sealed class MenuDialogs(IShell shell, ISettingsStore settings)
{
    public void ShowAbout(bool firstRun = false) =>
        Modal.Run(new AboutWindow(shell, AppInfo.Version, firstRun));

    public void ShowTransitions() => Modal.Run(new TransitionsWindow(settings));
}
