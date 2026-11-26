using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Themes;

/// <summary>
/// «Dividir versículos largos» as the bar over the verses and the phone change
/// it: it is the same setting of the Bible theme in use
/// (<see cref="SlideTheme.VerseSplitMaxChars"/>) the theme editor shows.
/// </summary>
public static class BibleVerseSplit
{
    /// <summary>What goes in the theme: 0 when off, else the limit kept within what makes sense.</summary>
    public static int MaxCharsFor(bool on, int chars) =>
        on ? Math.Clamp(chars, VerseSplitter.MinMaxChars, VerseSplitter.MaxMaxChars) : 0;

    /// <summary>
    /// What an editor that was open meanwhile writes: its own value only if it
    /// was changed there; otherwise the one in the library, which may be newer.
    /// </summary>
    public static int ValueToSave(int edited, int loaded, int? stored) =>
        edited == loaded ? stored ?? edited : edited;

    /// <summary>
    /// The number to show beside the checkbox: the theme's when it has one,
    /// else the last one used, else the one the editor proposes.
    /// </summary>
    public static int ShownChars(SlideTheme theme, int lastUsed = 0)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var chars = theme.VerseSplitMaxChars > 0 ? theme.VerseSplitMaxChars
            : lastUsed > 0 ? lastUsed
            : VerseSplitter.DefaultMaxChars;
        return MaxCharsFor(true, chars);
    }

    /// <summary>
    /// Saves the choice in the theme <paramref name="themeId"/>. False when
    /// there is nothing to save: no such theme (the built-in look, which is not
    /// one of the library's and is never written to), or it already said that.
    /// </summary>
    public static bool Save(IThemeRepository themes, int? themeId, bool on, int chars)
    {
        ArgumentNullException.ThrowIfNull(themes);
        if (themeId is not int id || themes.Get(id) is not { } theme)
            return false;

        var wanted = MaxCharsFor(on, chars);
        if (theme.VerseSplitMaxChars == wanted)
            return false;

        theme.VerseSplitMaxChars = wanted;
        themes.Save(theme);
        return true;
    }
}
