using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using EcclesiaCast.Core.Displays;
using Serilog;

namespace EcclesiaCast.Desktop.Platform;

/// <summary>The screens, and a word when one is plugged in or out.</summary>
public interface IScreenList : IDisplayProvider
{
    /// <summary>The list of screens changed; raised on the screen's thread.</summary>
    event EventHandler? Changed;
}

/// <summary>What Avalonia says about one screen, before it becomes a <see cref="DisplayInfo"/>.</summary>
internal sealed record ScreenData(string? Name, PixelRect Bounds, double Scaling, bool IsPrimary);

/// <summary>
/// The screens as Avalonia sees them, each with its scale (2 on a Retina
/// screen). Avalonia lists them through a window, so the list is empty until
/// <see cref="Attach"/> is given the operator's window.
/// </summary>
public sealed class AvaloniaDisplayProvider : IScreenList
{
    private Screens? _screens;

    public event EventHandler? Changed;

    /// <summary>Lists the screens through this window and hears when they change.</summary>
    public void Attach(TopLevel window)
    {
        if (window.Screens is not { } screens || ReferenceEquals(screens, _screens))
            return;

        _screens = screens;
        // macOS may tell while it is still moving windows around: the list
        // is read after that, on the screen's thread.
        screens.Changed += (_, _) => Dispatcher.UIThread.Post(OnChanged);
        Log.Information("Pantallas al abrir: {Screens}", Describe(GetDisplays()));
    }

    public IReadOnlyList<DisplayInfo> GetDisplays() =>
        _screens is null
            ? []
            : ToDisplays(_screens.All.Select(s => new ScreenData(s.DisplayName, s.Bounds, s.Scaling, s.IsPrimary)));

    private void OnChanged()
    {
        Log.Information("Cambiaron las pantallas: {Screens}", Describe(GetDisplays()));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// One <see cref="DisplayInfo"/> per screen, in pixels, with its scale.
    /// The name is what the system calls it («DELL U2415», «Pantalla
    /// integrada»); two that share it get «(2)», «(3)» after, so the chosen
    /// screen can be told apart and remembered.
    /// </summary>
    internal static IReadOnlyList<DisplayInfo> ToDisplays(IEnumerable<ScreenData> screens)
    {
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var displays = new List<DisplayInfo>();
        foreach (var screen in screens)
        {
            var name = string.IsNullOrWhiteSpace(screen.Name) ? "Pantalla" : screen.Name.Trim();
            var count = seen.TryGetValue(name, out var before) ? before + 1 : 1;
            seen[name] = count;
            if (count > 1)
                name = $"{name} ({count})";

            displays.Add(new DisplayInfo(
                name,
                screen.Bounds.X,
                screen.Bounds.Y,
                screen.Bounds.Width,
                screen.Bounds.Height,
                screen.IsPrimary,
                screen.Scaling > 0 ? screen.Scaling : 1));
        }

        return displays;
    }

    /// <summary>
    /// The screen of <paramref name="wanted"/> in a new list: the one with
    /// its name. Null when it is gone. Its size may have changed (the
    /// projector came back in another mode): the window then follows it.
    /// </summary>
    public static DisplayInfo? Find(DisplayInfo wanted, IReadOnlyList<DisplayInfo> now) =>
        now.FirstOrDefault(d => string.Equals(d.DeviceName, wanted.DeviceName, StringComparison.OrdinalIgnoreCase));

    internal static string Describe(IReadOnlyList<DisplayInfo> displays) =>
        displays.Count == 0
            ? "ninguna"
            : string.Join("; ", displays.Select(d =>
                $"{d.DeviceName} {d.Width}×{d.Height} en {d.X},{d.Y}, escala {d.Scale:0.##}{(d.IsPrimary ? " (principal)" : "")}"));
}
