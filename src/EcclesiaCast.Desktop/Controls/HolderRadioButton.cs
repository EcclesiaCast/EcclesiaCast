using Avalonia.Controls;
using Avalonia.Input;

namespace EcclesiaCast.Desktop.Controls;

/// <summary>
/// An option button with other controls inside («Empezar en [5] minutos»).
/// Avalonia's takes Enter and the space bar even when they come from a box
/// inside it, so Enter there never reaches the default button; WPF's only
/// takes its own keys, and so does this one.
/// </summary>
public sealed class HolderRadioButton : RadioButton
{
    protected override Type StyleKeyOverride => typeof(RadioButton);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (ReferenceEquals(e.Source, this))
            base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (ReferenceEquals(e.Source, this))
            base.OnKeyUp(e);
    }
}
