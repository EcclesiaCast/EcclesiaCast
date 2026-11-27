using System.Windows;
using System.Windows.Controls;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.ScreenBench.Wpf;

/// <summary>
/// «Ajustes» (batch 16). The window only binds to names of the main view
/// model, so it is drawn over a made-up object with those names and the same
/// values the Avalonia bench gets from the real one.
/// </summary>
internal static class SettingsScreens
{
    public static IEnumerable<(string Name, Func<FrameworkElement> Make, double Width)> All()
    {
        yield return ("ajustes", () => Settings("ScreensPill"), 720);
        yield return ("ajustes-videos", () => Settings("VideosPill"), 720);
        yield return ("ajustes-contador", () => Settings("UpdatesPill"), 720);
    }

    private static FrameworkElement Settings(string pill)
    {
        var window = new SettingsWindow(new Host()) { DataContext = new Values() };
        ((RadioButton)window.FindName(pill)).IsChecked = true;
        return LibraryScreens.Take(window, 480 - 39);
    }

    private sealed class Host : ISettingsHost
    {
        public bool IsAdvancedUi => false;

        public void SetUiMode(bool advanced)
        {
        }

        public void RestartSound()
        {
        }

        public void OpenTransitions(Window owner)
        {
        }

        public string? PickColor(string current) => null;

        public void OpenListening()
        {
        }
    }

    /// <summary>Public and writable: the window binds to some of them both ways.</summary>
    public sealed class Values
    {
        public DisplayOption[] Displays { get; } =
        [
            new(new DisplayInfo("Pantalla integrada", 0, 0, 2880, 1800, IsPrimary: true),
                "Pantalla 1 · 2880×1800 (principal)"),
            new(new DisplayInfo("Proyector", 2880, 0, 1920, 1080, IsPrimary: false), "Pantalla 2 · 1920×1080"),
        ];

        public DisplayOption[] StageDisplays => Displays;

        public DisplayOption SelectedDisplay { get => Displays[1]; set { } }

        public DisplayOption SelectedStageDisplay { get => Displays[0]; set { } }

        public bool IsListeningAvailable => false;

        public string[] NewVideoFitChoices { get; } = ["Ajustar con los bordes desenfocados (recomendado)", "Ajustar con color de fondo"];

        public int NewVideoFitIndex { get; set; } = 1;

        public bool IsNewVideoFitColor => true;

        public string NewVideoFitColor => "#1E3A5F";

        public bool IsLowPerformance { get; set; }

        public bool CheckForUpdatesOnStartup { get; set; } = true;

        public bool IsUsageCountEnabled { get; set; } = true;

        public string UsageChurchName { get; set; } = string.Empty;
    }
}
