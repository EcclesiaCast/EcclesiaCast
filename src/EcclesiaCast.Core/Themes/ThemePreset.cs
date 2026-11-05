namespace EcclesiaCast.Core.Themes;

/// <summary>
/// A ready-made look for the projector (songs or Bible): lettering, colours,
/// shadow, background and where the words and the caption sit, put on a theme
/// in one click and touched up after. It leaves alone what belongs to the
/// theme rather than to the look: its name, whether it is for songs or the
/// Bible, the transition and the Bible options (verse numbers, version name).
/// </summary>
public sealed record ThemePreset(string Name, string Description, Func<SlideTheme, SlideTheme> Change)
{
    /// <summary>A copy of the theme wearing this look.</summary>
    public SlideTheme ApplyTo(SlideTheme theme) => Change(theme.Clone());
}

/// <summary>The looks that come with the program, in the order the editor shows them.</summary>
public static class ThemePresets
{
    public static IReadOnlyList<ThemePreset> All { get; } =
    [
        new("Clásico", "Letra blanca y gruesa sobre azul noche, con sombra",
            t => Look(t, "Segoe UI", 92, bold: true, "#FFFFFF", "#10141E",
                outline: 0, shadow: 0.75)),

        new("Claro", "Letra oscura sobre fondo crema, para salas con luz",
            t => Look(t, "Georgia", 88, bold: false, "#1B1F2A", "#F4F1EA",
                outline: 0, shadow: 0, caption: "#5B6475")),

        new("Sobre video", "Fondo transparente y letra con contorno, para poner un video o una foto detrás",
            t => Look(t, "Segoe UI", 100, bold: true, "#FFFFFF", "#000000",
                outline: 4, shadow: 0.8, transparent: true)),

        new("Tercio inferior", "Sólo una franja abajo, para dejar libre el resto de la imagen",
            t =>
            {
                var look = Look(t, "Segoe UI", 64, bold: true, "#FFFFFF", "#000000",
                    outline: 3, shadow: 0.85, transparent: true);
                look.AlignV = VAlign.Bottom;
                look.BoxX = 160;
                look.BoxY = 720;
                look.BoxWidth = 1600;
                look.BoxHeight = 280;
                look.MinFontSize = 28;
                look.CaptionPosition = CaptionPosition.TopRight;
                return look;
            }),

        new("Elegante", "Serifa clara y delgada sobre casi negro",
            t => Look(t, "Georgia", 84, bold: false, "#F5EFE0", "#0D0F14",
                outline: 0, shadow: 0.6, caption: "#C8B98A")),

        new("Alto contraste", "Letra amarilla sobre negro: se lee desde el fondo de la sala",
            t => Look(t, "Arial", 96, bold: true, "#FFE45C", "#000000",
                outline: 0, shadow: 0, caption: "#FFFFFF")),

        new("A la izquierda", "Texto alineado a la izquierda, arriba, con la cita abajo",
            t =>
            {
                var look = Look(t, "Segoe UI", 80, bold: true, "#FFFFFF", "#12222F",
                    outline: 0, shadow: 0.7);
                look.AlignH = HAlign.Left;
                look.AlignV = VAlign.Top;
                look.CaptionPosition = CaptionPosition.BottomLeft;
                return look;
            }),
    ];

    /// <summary>Puts the common parts of a look on a theme that is already a copy.</summary>
    private static SlideTheme Look(
        SlideTheme t, string font, double size, bool bold, string text, string background,
        double outline, double shadow, bool transparent = false, string caption = "#B9C6DE")
    {
        t.FontFamily = font;
        t.MaxFontSize = size;
        t.MinFontSize = 36;
        t.Bold = bold;
        t.Italic = false;
        t.TextCase = TextCase.None;
        t.TextColor = text;
        t.Shadow = shadow > 0;
        t.ShadowOpacity = shadow > 0 ? shadow : 0.75;
        t.ShadowBlur = 18;
        t.OutlineWidth = outline;
        t.OutlineColor = "#000000";
        t.AlignH = HAlign.Center;
        t.AlignV = VAlign.Center;
        t.MarginHorizontal = 110;
        t.MarginVertical = 80;
        t.BoxX = t.BoxY = t.BoxWidth = t.BoxHeight = null;
        t.FitToWidth = false;
        t.BackgroundColor = background;
        t.BackgroundImagePath = null;
        t.TransparentBackground = transparent;
        t.BackgroundDim = 0;
        t.ShowCaption = true;
        t.CaptionPosition = CaptionPosition.BottomRight;
        t.CaptionFontSize = 40;
        t.CaptionFontFamily = null;
        t.CaptionColor = caption;
        t.SecondaryColor = caption;
        return t;
    }
}
