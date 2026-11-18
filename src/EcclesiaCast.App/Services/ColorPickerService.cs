namespace EcclesiaCast.App.Services;

/// <summary>The colour picker window (<see cref="ColorPickerHelper"/>) behind <see cref="IColorPicker"/>.</summary>
public sealed class ColorPickerService : IColorPicker
{
    public string Transparent => ColorPickerHelper.Transparent;

    public string? Pick(string? currentHex, bool allowTransparent = false) =>
        ColorPickerHelper.Pick(currentHex, allowTransparent);
}
