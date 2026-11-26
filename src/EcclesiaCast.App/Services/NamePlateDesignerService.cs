using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.Services;

public sealed class NamePlateDesignerService : INamePlateDesigner
{
    public NamePlateDesign? Design(NamePlate sample)
    {
        var window = new NamePlateDesignerWindow(sample) { Owner = Application.Current.MainWindow };
        return window.ShowDialog() == true ? new NamePlateDesign(window.Styles, window.ChosenStyle) : null;
    }
}
