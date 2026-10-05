namespace EcclesiaCast.Core.Media;

/// <summary>What a video's own stream looks like, as read from the file.</summary>
public sealed record VideoStreamInfo(int Width, int Height, int FrameRateNumerator, int FrameRateDenominator)
{
    public double FrameRate => FrameRateDenominator > 0 ? FrameRateNumerator / (double)FrameRateDenominator : 0;
}

/// <summary>
/// How a light copy of a video is made: the picture shrunk to what the
/// projector shows, the frame rate halved, or both.
/// </summary>
/// <param name="Shrink">True when the picture is bigger than a 1920×1080 screen needs.</param>
/// <param name="FrameRateNumerator">The copy's frame rate, or 0 to keep the original.</param>
public sealed record LightCopyPlan(bool Shrink, int FrameRateNumerator, int FrameRateDenominator)
{
    public bool ChangesFrameRate => FrameRateNumerator > 0;
}

/// <summary>
/// Decides which videos are worth a light copy. Only the heavy ones: a 4K
/// file or a 60-frame one costs the projection PC several times the work of
/// a 1080p, 30-frame one and looks the same on a 1080p projector. Anything
/// else plays from the original — converting it only filled the disk without
/// making playback any lighter (measured on the church's HEVC loops).
/// </summary>
public static class LightCopyPolicy
{
    /// <summary>The screen the program designs for, the same canvas the framing uses.</summary>
    public const int ScreenWidth = 1920;
    public const int ScreenHeight = 1080;

    /// <summary>The highest frame rate kept as is; 30 and 29.97 stay, 50 and 60 are halved.</summary>
    public const double MaxFrameRate = 31;

    /// <summary>
    /// Shrinking a picture only a little is not worth a re-encode: a 2048×1080
    /// cinema file is close enough to 1920×1080.
    /// </summary>
    private const double MinWorthwhileShrink = 0.9;

    /// <summary>The copy to make, or null when the original is already light enough.</summary>
    public static LightCopyPlan? Plan(VideoStreamInfo video)
    {
        var shrink = CoverScale(video.Width, video.Height) < MinWorthwhileShrink;

        int num = 0, den = 0;
        if (video.FrameRate > MaxFrameRate)
        {
            // Halve until it fits: 60 → 30, 50 → 25, 59.94 → 29.97, 120 → 30.
            // Halving keeps every other frame, so the motion stays even.
            num = video.FrameRateNumerator;
            den = video.FrameRateDenominator;
            while (num / (double)den > MaxFrameRate)
                den *= 2;
        }

        return shrink || num > 0 ? new LightCopyPlan(shrink, num, den) : null;
    }

    /// <summary>
    /// How much the picture can shrink and still cover the whole screen —
    /// the larger of the two ratios, so neither side ends up softer than the
    /// screen. 1 or more means it is not bigger than the screen at all.
    /// </summary>
    public static double CoverScale(int width, int height) =>
        width <= 0 || height <= 0
            ? 1
            : Math.Max(ScreenWidth / (double)width, ScreenHeight / (double)height);
}
