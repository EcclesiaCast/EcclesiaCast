namespace EcclesiaCast.Presentation.Services;

/// <summary>The program's colour picker window: opens over the window that asked and returns the colour as text.</summary>
public interface IColorPicker
{
    /// <summary>Returned by <see cref="Pick"/> when the operator picks «Transparente».</summary>
    string Transparent { get; }

    /// <summary>The colour chosen («#RRGGBB»), or null when the operator cancelled.</summary>
    string? Pick(string? currentHex, bool allowTransparent = false);
}
