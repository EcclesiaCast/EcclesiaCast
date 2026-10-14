using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Remote;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Diagnostics;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Performance;
using EcclesiaCast.Core.Playlists;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Core.Timing;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

public sealed partial class MainViewModel
{
    // ── Pantalla de escenario ────────────────────────────────────

    private const string StageDisplayKey = "stage.display";

    /// <summary>Displays offered for the stage screen, plus a "no usar" entry.</summary>
    public ObservableCollection<DisplayOption> StageDisplays { get; } = [];

    [ObservableProperty]
    private DisplayOption? _selectedStageDisplay;

    [ObservableProperty]
    private bool _isStageVisible;

    private readonly IStageDesigner _stageDesigner;

    private List<StageLayout> _stageLayouts = [];

    /// <summary>The saved arrangements of the stage screen, in the order they are offered.</summary>
    public IReadOnlyList<StageLayout> StageLayouts => _stageLayouts;

    /// <summary>
    /// The arrangement the stage screen (and its copy in the preview) is
    /// showing. The view redraws when this changes, so edits always replace
    /// the object instead of changing it in place.
    /// </summary>
    [ObservableProperty]
    private StageLayout _activeStageLayout = new();

    private void LoadStageLayouts()
    {
        var state = StageLayoutStore.Load(_settings);
        _stageLayouts = [.. state.Layouts];
        ActiveStageLayout = state.Active;
        OnPropertyChanged(nameof(StageLayouts));
    }

    /// <summary>Quick switch from the ▾ next to the stage button: takes effect on the open screen at once.</summary>
    [RelayCommand]
    private void SelectStageLayout(StageLayout? layout)
    {
        if (layout is null || ReferenceEquals(layout, ActiveStageLayout))
            return;

        ActiveStageLayout = layout;
        StageLayoutStore.Save(_settings, _stageLayouts, layout.Name);
        _stage.ApplyLayout(layout);
        StatusText = $"Escenario: {layout.Name}.";
    }

    /// <summary>Opens the editor; what it returns replaces the saved arrangements.</summary>
    [RelayCommand]
    private void EditStage()
    {
        var result = _stageDesigner.Edit(_stageLayouts, ActiveStageLayout.Name);
        if (result is null)
            return;

        _stageLayouts = [.. result.Layouts];
        StageLayoutStore.Save(_settings, _stageLayouts, result.ActiveName);
        OnPropertyChanged(nameof(StageLayouts));

        // A new object even when nothing about the active one changed: the
        // views compare by reference and must redraw after an edit.
        ActiveStageLayout = result.Active.Clone();
        _stageLayouts[_stageLayouts.FindIndex(l => l.Name == result.Active.Name)] = ActiveStageLayout;
        _stage.ApplyLayout(ActiveStageLayout);
        StatusText = "Escenarios guardados.";
    }

    private const string PreviewShowsStageKey = "layout.preview.stage";

    /// <summary>
    /// What the PREVIEW panel shows: the next slide (false) or the stage
    /// screen as the band sees it (true). Remembered between services.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewIsStage))]
    private bool _previewShowsStage;

    partial void OnPreviewShowsStageChanged(bool value) =>
        _settings.Set(PreviewShowsStageKey, value ? "1" : "0");

    // ── LIVE, PREVIEW and stage in one box ──
    // The operator window can show the three as tabs of a single box (the
    // default) or as two boxes. Whether it is merged is a layout choice that
    // MainWindow keeps and remembers; it hands it over here so the XAML can
    // bind to what each box has to show.

    /// <summary>LIVE, PREVIEW and the stage view are tabs of one box.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOutputSplit), nameof(ShowLivePanel), nameof(ShowPreviewPanel),
        nameof(PreviewIsStage), nameof(ShowTransportInPreview))]
    private bool _isOutputMerged = true;

    /// <summary>The tab on show while merged: 0 = LIVE, 1 = PREVIEW, 2 = ESCENARIO.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowLivePanel), nameof(ShowPreviewPanel), nameof(PreviewIsStage))]
    private int _mergedTab;

    public bool IsOutputSplit => !IsOutputMerged;

    /// <summary>The LIVE box is on show: always when split, only on its own tab when merged.</summary>
    public bool ShowLivePanel => !IsOutputMerged || MergedTab == 0;

    public bool ShowPreviewPanel => !IsOutputMerged || MergedTab != 0;

    /// <summary>
    /// The PREVIEW box shows the stage screen instead of the next slide: the
    /// ESCENARIO tab when merged, the Siguiente / Escenario toggle when split.
    /// </summary>
    public bool PreviewIsStage => IsOutputMerged ? MergedTab == 2 : PreviewShowsStage;

    /// <summary>
    /// The video controls follow the video, not the LIVE tab: with the box
    /// merged, someone looking at PREVIEW still needs to pause what is playing.
    /// </summary>
    public bool ShowTransportInPreview => IsOutputMerged && HasVideoPlayback;

    /// <summary>
    /// Turns the stage display on or off. It must not land on the same screen
    /// as the congregation's output — that would replace the projection with
    /// the musicians' view.
    /// </summary>
    [RelayCommand]
    private void ToggleStage()
    {
        if (IsStageVisible)
        {
            _stage.Hide();
            StatusText = "Pantalla de escenario apagada.";
            return;
        }

        if (SelectedStageDisplay is null)
        {
            StatusText = "Elegí en qué pantalla va el escenario.";
            return;
        }

        if (SelectedStageDisplay.Info.DeviceName == SelectedDisplay?.Info.DeviceName)
        {
            MessageBox.Show(
                "El escenario y la salida no pueden ir en la misma pantalla: la vista de los "
                + "músicos taparía lo que ve la congregación.\n\n"
                + "Elegí otro monitor para el escenario.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // With only two screens the stage can only land on the operator's own,
        // covering the panel it was started from. Say so, and say how to get
        // out, before it happens.
        if (IsOperatorDisplay(SelectedStageDisplay.Info))
        {
            var answer = MessageBox.Show(
                "El escenario va a ocupar esta misma pantalla y va a tapar el panel del operador.\n\n"
                + "Para cerrarlo, hacé doble clic sobre él.\n\n"
                + "¿Lo prendo igual?",
                "EcclesiaCast", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (answer != MessageBoxResult.Yes)
                return;
        }

        _stage.ShowOn(SelectedStageDisplay.Info, ActiveStageLayout);
        _settings.Set(StageDisplayKey, SelectedStageDisplay.Info.DeviceName);
        StatusText = $"Escenario en {SelectedStageDisplay.Label}.";
    }

    /// <summary>True when that display is the one holding the operator window.</summary>
    private static bool IsOperatorDisplay(DisplayInfo display)
    {
        var window = Application.Current?.MainWindow;
        if (window is null)
            return false;

        var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return false;

        var screen = System.Windows.Forms.Screen.FromHandle(handle);
        return string.Equals(screen.DeviceName, display.DeviceName, StringComparison.Ordinal);
    }

    [RelayCommand]
    private void ResetStageTimer()
    {
        _stage.ResetTimer();
        UpdateTimes();
        StatusText = "Cronómetro del escenario en cero.";
    }
}
