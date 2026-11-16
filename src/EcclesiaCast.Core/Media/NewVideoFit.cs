namespace EcclesiaCast.Core.Media;

/// <summary>How a video that has just been added is fitted to the screen (Ajustes → Videos).</summary>
public enum NewVideoFitMode
{
    /// <summary>Whole, with the bars filled by the same video, enlarged and blurred.</summary>
    BlurredEdges,

    /// <summary>Whole, with the bars painted a flat colour.</summary>
    Color,

    /// <summary>Covers the screen and crops what does not fit (what every video had before).</summary>
    Fill,

    /// <summary>Stretched to the screen, ignoring its proportions.</summary>
    Stretch,
}

/// <summary>
/// The framing a new video gets when it comes into the library. It is
/// written into the video when it is added, like any choice made by hand in
/// Propiedades: changing this setting later leaves the videos already there
/// as they are.
/// </summary>
public sealed record NewVideoFit(NewVideoFitMode Mode, string Color)
{
    public const string SettingKey = "media.newVideoFit";

    public const string DefaultColor = "#000000";

    /// <summary>Whole and with blurred edges: a video of any shape looks like one picture with no border.</summary>
    public static NewVideoFit Default { get; } = new(NewVideoFitMode.BlurredEdges, DefaultColor);

    /// <summary>Reads the saved value («Color|#102030»); the default for nothing or anything it cannot read.</summary>
    public static NewVideoFit Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Default;

        var parts = value.Split('|');
        if (!Enum.TryParse<NewVideoFitMode>(parts[0].Trim(), ignoreCase: true, out var mode)
            || !Enum.IsDefined(mode))
            return Default;

        var color = parts.Length > 1 && IsColor(parts[1].Trim()) ? parts[1].Trim() : DefaultColor;
        return new NewVideoFit(mode, color);
    }

    public string ToSetting() => $"{Mode}|{(IsColor(Color) ? Color : DefaultColor)}";

    /// <summary>Gives a video just added the framing this setting says. Anything that is not a video is left alone.</summary>
    public void ApplyTo(MediaItem item)
    {
        if (item.Type != MediaType.Video)
            return;

        switch (Mode)
        {
            case NewVideoFitMode.BlurredEdges:
                item.Scaling = MediaScaling.Fit;
                item.FillMediaId = MediaItem.FillWithSelfId;
                break;
            case NewVideoFitMode.Color:
                item.Scaling = MediaScaling.Fit;
                item.FillMediaId = null;
                item.FillColor = IsColor(Color) ? Color : DefaultColor;
                break;
            case NewVideoFitMode.Stretch:
                item.Scaling = MediaScaling.Stretch;
                item.FillMediaId = null;
                break;
            default:
                item.Scaling = MediaScaling.Fill;
                item.FillMediaId = null;
                break;
        }
    }

    /// <summary>«#RRGGBB» or «#AARRGGBB».</summary>
    public static bool IsColor(string? value) =>
        value is { Length: 7 or 9 } && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);
}
