using System.Text.Json;

namespace EcclesiaCast.Presentation.Rendering;

/// <summary>
/// What a bench measured when it drew one case in one theme: where each text
/// ended up and how its lines were cut. The WPF bench writes it and the
/// reference copies are kept in <c>tests/casos-de-render/referencia-wpf/</c>;
/// the Avalonia bench will write the same thing and the two get compared.
/// </summary>
public sealed class RenderResult
{
    public string Case { get; set; } = string.Empty;

    /// <summary>Key of the theme it was drawn with.</summary>
    public string Theme { get; set; } = string.Empty;

    /// <summary>Width and height of the picture, in pixels.</summary>
    public int Width { get; set; }
    public int Height { get; set; }

    public List<RenderedText> Texts { get; set; } = [];

    /// <summary>The file name of the result: <c>case.theme</c>.</summary>
    public static string FileKey(string caseId, string themeKey) => $"{caseId}.{themeKey}";

    public string ToJson() => JsonSerializer.Serialize(this, RenderCase.Options);

    public static RenderResult Load(string path) =>
        JsonSerializer.Deserialize<RenderResult>(File.ReadAllText(path), RenderCase.Options)
        ?? throw new InvalidDataException($"{Path.GetFileName(path)} is empty.");
}

/// <summary>One block of text on the screen.</summary>
public sealed class RenderedText
{
    /// <summary>Which text it is: main, secondary, caption, overlay, countdown, logo, stage…</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The size the letters ended up with, in pixels over the 1920×1080 picture.</summary>
    public double FontSize { get; set; }

    /// <summary>The typeface that was really used, which is not the asked one when that is missing.</summary>
    public string FontUsed { get; set; } = string.Empty;

    /// <summary>The text of each line, as it was cut.</summary>
    public List<string> Lines { get; set; } = [];

    /// <summary>The block of text, in pixels over the picture.</summary>
    public RenderRect Rect { get; set; } = new();
}

public sealed class RenderRect
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
