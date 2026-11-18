using System.Text.Json;
using System.Text.Json.Serialization;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Presentation.Rendering;

/// <summary>
/// One fixed slide of the render test set (<c>tests/casos-de-render/</c>): the
/// words, what is on screen, and the complete themes it is drawn with. It is
/// plain data, read without WPF, so the WPF bench and the Avalonia one draw
/// exactly the same cases and their results can be compared.
/// </summary>
public sealed class RenderCase
{
    /// <summary>The file name without extension; also the name of its results.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>What the case is for, in a sentence, for whoever reads the folder.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>What the output shows: the words, the logo, black… Content when absent.</summary>
    public OutputState State { get; set; } = OutputState.Content;

    /// <summary>The words of the slide; null in the cases that are not a slide (logo, stage box).</summary>
    public RenderSlide? Slide { get; set; }

    /// <summary>A countdown over the slide, when the case is one.</summary>
    public RenderCountdown? Countdown { get; set; }

    /// <summary>The lower-third announcement, when the case has one.</summary>
    public RenderOverlay? Overlay { get; set; }

    /// <summary>A text logo (the Logo state), when the case is one.</summary>
    public RenderLogo? Logo { get; set; }

    /// <summary>One box of the stage screen, when the case is one.</summary>
    public StageBox? StageBox { get; set; }

    /// <summary>The themes the case is drawn with, each complete. Always the same three.</summary>
    public List<RenderTheme> Themes { get; set; } = [];

    // ── Building what the screen needs ───────────────────────────

    /// <summary>The slide in one of the case's themes; null when the case has no slide.</summary>
    public SlideContent? BuildSlide(RenderTheme theme) =>
        Slide is null
            ? null
            : new SlideContent(Slide.MainText, Slide.Caption, Slide.SecondaryText, theme.Theme, Slide.Override);

    /// <summary>The countdown of the case, pinned to <paramref name="now"/>.</summary>
    public Countdown? BuildCountdown(DateTimeOffset now) =>
        Countdown is null
            ? null
            : Core.Presentation.Countdown.ForMinutes(Countdown.Minutes, now, Countdown.Heading, Countdown.FinishedMessage);

    /// <summary>The text logo of the case, in the typeface of the theme unless the case has its own.</summary>
    public Logo? BuildLogo(RenderTheme theme) =>
        Logo is null
            ? null
            : new Logo
            {
                Kind = LogoKind.Text,
                Text = Logo.Text,
                FontFamily = Logo.FontFamily ?? theme.Theme.FontFamily,
                FontSize = Logo.FontSize,
                Bold = Logo.Bold,
                TextColor = Logo.TextColor,
                BackgroundColor = Logo.BackgroundColor,
            };

    /// <summary>The announcement's typeface: the case's own, or the theme's.</summary>
    public string? OverlayFont(RenderTheme theme) => Overlay?.FontFamily ?? theme.Theme.FontFamily;

    /// <summary>The stage box in the typeface and weight of one theme, since a stage box has no theme of its own.</summary>
    public StageBox? BuildStageBox(RenderTheme theme)
    {
        if (StageBox is null)
            return null;

        var box = StageBox.Clone();
        box.FontFamily = theme.Theme.FontFamily;
        box.Bold = theme.Theme.Bold;
        return box;
    }

    // ── Reading and writing ──────────────────────────────────────

    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
        // The accents stay readable in the files.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Reads one case file; the id is the file's name when the file does not say.</summary>
    public static RenderCase Load(string path)
    {
        var text = File.ReadAllText(path);
        var loaded = JsonSerializer.Deserialize<RenderCase>(text, Options)
                     ?? throw new InvalidDataException($"{Path.GetFileName(path)} is empty.");
        if (string.IsNullOrWhiteSpace(loaded.Id))
            loaded.Id = Path.GetFileNameWithoutExtension(path);
        return loaded;
    }

    /// <summary>Every case file of a folder (not the sub-folders), in the order of their names.</summary>
    public static IReadOnlyList<RenderCase> LoadAll(string folder) =>
        Directory.GetFiles(folder, "*.json")
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(Load)
            .ToList();

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    /// <summary>
    /// The folder with the cases, looked for upwards from <paramref name="start"/>
    /// (the output folder of the bench or of the tests); null when there is none.
    /// </summary>
    public static string? FindFolder(string start)
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "casos-de-render");
            if (Directory.Exists(candidate))
                return candidate;
        }
        return null;
    }
}

/// <summary>The words of a case; the theme is added when it is drawn.</summary>
public sealed class RenderSlide
{
    public string MainText { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? SecondaryText { get; set; }

    /// <summary>The slide's own changes over its theme (spacing, alignment…); null when it has none.</summary>
    public SlideOverride? Override { get; set; }
}

public sealed class RenderCountdown
{
    /// <summary>Minutes left when it is drawn; zero or less is a countdown that already ended.</summary>
    public double Minutes { get; set; } = 5.5;
    public string Heading { get; set; } = "Empezamos en";
    public string FinishedMessage { get; set; } = "¡Bienvenidos!";
}

public sealed class RenderOverlay
{
    public string Text { get; set; } = string.Empty;

    /// <summary>Null takes the typeface of the theme being drawn.</summary>
    public string? FontFamily { get; set; }

    public double FontSize { get; set; } = OverlayStyle.DefaultSize;
}

public sealed class RenderLogo
{
    public string Text { get; set; } = string.Empty;

    /// <summary>Null takes the typeface of the theme being drawn.</summary>
    public string? FontFamily { get; set; }

    public double FontSize { get; set; } = 120;
    public bool Bold { get; set; } = true;
    public string TextColor { get; set; } = "#FFFFFF";
    public string BackgroundColor { get; set; } = "#10141E";
}

/// <summary>A named complete theme: the factory song one, the factory Bible one, or Impact.</summary>
public sealed class RenderTheme
{
    /// <summary>Short and fixed: <c>cancion</c>, <c>biblia</c> or <c>impact</c>. It goes into the file names of the results.</summary>
    public string Key { get; set; } = string.Empty;

    public SlideTheme Theme { get; set; } = new();
}
