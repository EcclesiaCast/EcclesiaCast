using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Ui;

namespace EcclesiaCast.Presentation.Services;

/// <summary>The size of the main window and whether it was maximized, as left at the last close.</summary>
public sealed record WindowBounds(double Width, double Height, bool Maximized);

/// <summary>
/// What the main window remembers between services: the size of its panels, where
/// each block sits, which ones are folded, the size of the window itself and
/// whether the learning or the advanced layout is on. The window decides
/// <em>what</em> to keep and when; this class knows the names under which it is
/// written and how each value is stored, so every interface reads the same settings.
/// </summary>
public sealed class MainWindowSettings(ISettingsStore settings)
{
    // Sizes of the blocks, whichever slot they sit in.
    public const string LibraryWidthKey = "layout.library.width";
    public const string BibleGridWidthKey = "layout.biblegrid.width";
    public const string PreviewWidthKey = "layout.preview.width";
    public const string PlaylistHeightKey = "layout.playlist.height";
    public const string MediaHeightKey = "layout.media.height";

    /// <summary>Size and state of the window: «width;height;normal» or «width;height;max».</summary>
    public const string WindowKey = "layout.window";

    // Where the blocks sit.
    public const string MirroredKey = "layout.mirrored";
    public const string PlaylistTopKey = "layout.playlist.top";
    public const string MediaTopKey = "layout.media.top";
    public const string LiveBottomKey = "layout.live.bottom";
    public const string OutputSplitKey = "layout.output.split";

    // The sections that fold away.
    public const string LibraryCollapsedKey = "layout.library.collapsed";
    public const string PlaylistCollapsedKey = "layout.playlist.collapsed";
    public const string MediaCollapsedKey = "layout.media.collapsed";
    public const string ToolsCollapsedKey = "layout.tools.collapsed";
    public const string LeftRailKey = "layout.left.rail";
    public const string AudioListCollapsedKey = "layout.audiolist.collapsed";

    /// <summary>A size saved before; null when there is none or it makes no sense (not a number, zero or less).</summary>
    public double? ReadSize(string key) =>
        double.TryParse(settings.Get(key), out var value) && value > 0 ? value : null;

    /// <summary>Saves a size, rounded to whole pixels.</summary>
    public void WriteSize(string key, double value) => settings.Set(key, value.ToString("0"));

    /// <summary>A yes/no choice saved before; nothing saved means no.</summary>
    public bool ReadFlag(string key) => settings.Get(key) == "1";

    public void WriteFlag(string key, bool value) => settings.Set(key, value ? "1" : "0");

    /// <summary>The window as it was closed last time; null when nothing usable was saved.</summary>
    public WindowBounds? ReadWindow()
    {
        if (settings.Get(WindowKey)?.Split(';') is [var w, var h, var state]
            && double.TryParse(w, out var width) && double.TryParse(h, out var height))
            return new WindowBounds(width, height, state == "max");

        return null;
    }

    /// <summary>Saves the size of the window (when it is maximized, the size it has to go back to).</summary>
    public void WriteWindow(WindowBounds bounds) =>
        settings.Set(WindowKey, bounds.Maximized
            ? $"{bounds.Width:0};{bounds.Height:0};max"
            : $"{bounds.Width:0};{bounds.Height:0};normal");

    /// <summary>True when the advanced layout (no shortcut hints) was chosen; the learning layout otherwise.</summary>
    public bool ReadAdvancedUi() => OperatorLayout.IsAdvanced(settings.Get(OperatorLayout.SettingKey));

    public void WriteAdvancedUi(bool advanced) =>
        settings.Set(OperatorLayout.SettingKey, OperatorLayout.ToSetting(advanced));
}
