using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.App.Platform;

/// <summary>The screen of the operator's WPF window, behind <see cref="IOperatorScreen"/>.</summary>
public sealed class WpfOperatorScreen : IOperatorScreen
{
    public bool Holds(DisplayInfo display) => OperatorScreen.Holds(display);
}
