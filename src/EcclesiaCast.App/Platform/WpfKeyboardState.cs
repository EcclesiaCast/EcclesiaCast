using System.Windows.Input;

namespace EcclesiaCast.App.Platform;

/// <summary>The keyboard of the WPF program, behind <see cref="IKeyboardState"/>.</summary>
public sealed class WpfKeyboardState : IKeyboardState
{
    public bool IsControlDown => (Keyboard.Modifiers & ModifierKeys.Control) != 0;
}
