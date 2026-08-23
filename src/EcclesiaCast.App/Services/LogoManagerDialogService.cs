using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.App.Services;

public sealed class LogoManagerDialogService(ILogoRepository logos) : ILogoManagerDialog
{
    public bool Show()
    {
        var window = new LogoManagerWindow(logos)
        {
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();
        return window.ChangesMade;
    }
}
