using System.Windows.Input;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App;

public partial class MainWindow
{
    // Ctrl + rueda sobre la grilla o la barra de medios: cambia el tamaño de
    // las tarjetas, un paso por cada muesca. Sin Ctrl la rueda sigue
    // desplazando la lista como siempre.
    private void SlidesScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!IsCtrlTurn(e) || DataContext is not MainViewModel vm)
            return;

        vm.SlideCardScale = CardZoom.Nudge(vm.SlideCardScale, e.Delta, CardZoom.SlidesMin, CardZoom.SlidesMax);
        e.Handled = true;
    }

    private void MediaScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!IsCtrlTurn(e) || DataContext is not MainViewModel vm)
            return;

        vm.MediaCardScale = CardZoom.Nudge(vm.MediaCardScale, e.Delta, CardZoom.MediaMin, CardZoom.MediaMax);
        e.Handled = true;
    }

    private static bool IsCtrlTurn(MouseWheelEventArgs e) =>
        e.Delta != 0 && (Keyboard.Modifiers & ModifierKeys.Control) != 0;
}
