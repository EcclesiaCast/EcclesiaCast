using System.Globalization;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Core.Timing;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Rendering;

/// <summary>
/// What one box of the stage screen says: the live words, the next slide, the
/// clock… or, in the editor (no projection), a sample of it. The stage box
/// controls of WPF and of Avalonia only draw it.
/// </summary>
public static class StageBoxText
{
    /// <summary>
    /// The kinds that show one short line. It is measured as wide as it
    /// needs, so the box can shrink it when it is too wide; the others are
    /// held to the width of the box and shrink only when they get too tall.
    /// </summary>
    public static bool IsSingleLine(StageBoxKind kind) =>
        kind is StageBoxKind.Clock or StageBoxKind.Timer or StageBoxKind.Countdown
            or StageBoxKind.SectionTimer or StageBoxKind.LiveName or StageBoxKind.Overlay;

    /// <summary>What the box says: from the live projection, or the sample when there is none.</summary>
    public static string For(StageBox box, ProjectionViewModel? projection) =>
        projection is { } vm ? Live(box, vm) : Sample(box);

    /// <summary>True for a section timer whose time is over: it turns red.</summary>
    public static bool IsLate(StageBox box, ProjectionViewModel? projection) =>
        box.Kind == StageBoxKind.SectionTimer
        && projection?.Clocks is { HasSection: true } clocks
        && clocks.Section.IsOvertime(DateTimeOffset.Now);

    /// <summary>
    /// True when the box has to be redrawn every second: the clock, the
    /// stopwatch and the countdown move on their own, and while a countdown
    /// runs it replaces the words on the other boxes.
    /// </summary>
    public static bool MovesEverySecond(StageBox box, ProjectionViewModel? projection) =>
        box.Kind is StageBoxKind.Clock or StageBoxKind.Timer or StageBoxKind.Countdown or StageBoxKind.SectionTimer
        || projection?.Countdown is not null;

    private static string Live(StageBox box, ProjectionViewModel vm)
    {
        var now = DateTimeOffset.Now;

        // While a countdown runs it is what the congregation sees, so it is
        // what the band and whoever is preaching need to see too: they are
        // the ones who have to be in place when it hits zero.
        if (vm.Countdown is { } countdown)
        {
            switch (box.Kind)
            {
                case StageBoxKind.LiveText:
                    return countdown.Format(now);
                case StageBoxKind.LiveName:
                    return "⏱  CUENTA REGRESIVA";
                case StageBoxKind.NextSlide:
                    return countdown.HasFinished(now) || string.IsNullOrWhiteSpace(countdown.Heading)
                        ? "—"
                        : countdown.Heading;
            }
        }

        return box.Kind switch
        {
            // Black and Clear hide the words from the congregation, but the
            // stage still needs to read them: that is what a stage display is for.
            StageBoxKind.LiveText => Transform(vm.Slide?.MainText ?? string.Empty, vm.Slide?.Theme),
            StageBoxKind.NextSlide => vm.NextSlide is { } next ? Transform(next.MainText, next.Theme) : "—",
            StageBoxKind.LiveName => vm.State switch
            {
                OutputState.Black => "⏸  PANTALLA EN NEGRO",
                OutputState.Logo => "🖼  LOGO",
                OutputState.Clear => vm.Slide is null ? "—" : "👁  SIN TEXTO EN PANTALLA",
                _ => vm.SlideLabel ?? string.Empty,
            },
            StageBoxKind.Clock => DateTime.Now.ToString("HH:mm", CultureInfo.CurrentCulture),
            StageBoxKind.Timer => TimeText.Elapsed(vm.Clocks.Stopwatch.Elapsed(now)),
            StageBoxKind.SectionTimer => vm.Clocks.HasSection ? TimeText.Clock(vm.Clocks.Section.Remaining(now)) : "—",
            StageBoxKind.Countdown => vm.Countdown?.Format(now) ?? string.Empty,
            StageBoxKind.Notes => vm.StageNotes ?? string.Empty,
            StageBoxKind.Overlay => vm.Overlay ?? string.Empty,
            StageBoxKind.FixedText => box.Text,
            _ => string.Empty,
        };
    }

    /// <summary>What the box shows in the editor, where there is no projection.</summary>
    public static string Sample(StageBox box) => box.Kind switch
    {
        StageBoxKind.LiveText => "Santo, santo, santo, Señor omnipotente,\nsiempre el labio mío loores te dará.",
        StageBoxKind.NextSlide => "Santo, santo, santo, te adoro reverente",
        StageBoxKind.LiveName => "Coro",
        StageBoxKind.Clock => "10:30",
        StageBoxKind.Timer => "12:45",
        StageBoxKind.Countdown => "5:00",
        StageBoxKind.SectionTimer => "25:00",
        StageBoxKind.Notes => "Los puntos del mensaje:\n1. La gracia\n2. La fe\n3. La respuesta",
        StageBoxKind.Overlay => "Aviso al pie",
        StageBoxKind.FixedText => string.IsNullOrEmpty(box.Text) ? "Texto fijo" : box.Text,
        _ => string.Empty,
    };

    /// <summary>
    /// Matches the projector's casing so the singers read what the
    /// congregation reads; everything else about the look stays plain.
    /// (Sentence case is not applied here, as it never was.)
    /// </summary>
    private static string Transform(string text, SlideTheme? theme) =>
        (theme?.TextCase ?? TextCase.None) switch
        {
            TextCase.Upper => text.ToUpper(CultureInfo.CurrentCulture),
            TextCase.Lower => text.ToLower(CultureInfo.CurrentCulture),
            TextCase.Title => CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(text.ToLower(CultureInfo.CurrentCulture)),
            _ => text,
        };
}
