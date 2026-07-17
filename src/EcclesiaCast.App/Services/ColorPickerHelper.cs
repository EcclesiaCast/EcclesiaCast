using System.Windows.Media;

namespace EcclesiaCast.App.Services;

/// <summary>Opens the native Windows color picker and returns a hex string.</summary>
public static class ColorPickerHelper
{
    public static string? Pick(string? currentHex)
    {
        using var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            AnyColor = true,
        };

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(currentHex ?? "#FFFFFF");
            dialog.Color = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
        }
        catch
        {
            // Hex inválido: el diálogo abre en su color por defecto.
        }

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return null;

        var picked = dialog.Color;
        return $"#{picked.R:X2}{picked.G:X2}{picked.B:X2}";
    }
}
