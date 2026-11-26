using Avalonia.Controls;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Desktop.Controls;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// The stage display, full screen on its own monitor, in Avalonia: the WPF
/// <c>StageWindow</c>. What it shows is the <see cref="StageView"/>; this
/// window only places it, keeps it out of the way of the keyboard and
/// offers the way out (a double click).
/// </summary>
public sealed class StageWindow : FullScreenWindow
{
    private readonly Border _help;

    public StageWindow(ProjectionViewModel projection, IOutputWindowNative native)
        : base(native)
    {
        Title = "EcclesiaCast — Escenario";
        View = new StageView { DataContext = projection };
        _help = Hint("Doble clic en esta pantalla para cerrar el escenario");

        var layers = new Grid();
        layers.Children.Add(View);
        layers.Children.Add(_help);
        Content = layers;

        DoubleTapped += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public StageView View { get; }

    /// <summary>Which arrangement is drawn; the operator picks it under ▾ Escenario.</summary>
    public void ApplyLayout(StageLayout layout) => View.Layout = layout;

    /// <summary>Raised when the operator double-clicks the stage screen to close it.</summary>
    public event EventHandler? CloseRequested;

    public void ShowOn(DisplayInfo display)
    {
        ShowOnDisplay(display);
        Flash(_help);
    }
}
