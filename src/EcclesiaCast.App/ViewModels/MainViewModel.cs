using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.ViewModels;

/// <summary>A display plus the label shown to the operator.</summary>
public sealed record DisplayOption(DisplayInfo Info, string Label);

public sealed partial class MainViewModel : ObservableObject
{
    private const string OutputDisplayKey = "output.display";

    private readonly IDisplayProvider _displayProvider;
    private readonly IProjectionWindowService _projection;
    private readonly ISettingsStore _settings;
    private readonly IPresentationService _presentation;

    /// <summary>What the projector is showing; the Live box binds to it.</summary>
    public ProjectionViewModel Projection { get; }

    public ObservableCollection<DisplayOption> Displays { get; } = [];

    [ObservableProperty]
    private DisplayOption? _selectedDisplay;

    [ObservableProperty]
    private string _statusText =
        "Escribí un texto rápido y presioná Ctrl+Enter para proyectarlo.";

    [ObservableProperty]
    private bool _isProjecting;

    [ObservableProperty]
    private string _quickText = string.Empty;

    /// <summary>What is being prepared; the Preview box binds to it.</summary>
    [ObservableProperty]
    private SlideContent? _previewSlide;

    [ObservableProperty]
    private string _overlayText = string.Empty;

    [ObservableProperty]
    private bool _isOverlayActive;

    [ObservableProperty]
    private bool _isClearActive;

    [ObservableProperty]
    private bool _isBlackActive;

    [ObservableProperty]
    private bool _isLogoActive;

    public MainViewModel(
        IDisplayProvider displayProvider,
        IProjectionWindowService projection,
        ISettingsStore settings,
        IPresentationService presentation,
        ProjectionViewModel projectionViewModel)
    {
        _displayProvider = displayProvider;
        _projection = projection;
        _settings = settings;
        _presentation = presentation;
        Projection = projectionViewModel;

        _presentation.Changed += (_, _) => UpdateStateFlags();
        UpdateStateFlags();
        RefreshDisplays();
    }

    partial void OnQuickTextChanged(string value) =>
        PreviewSlide = string.IsNullOrWhiteSpace(value)
            ? null
            : new SlideContent(value.Trim());

    [RelayCommand]
    private void RefreshDisplays()
    {
        var saved = SelectedDisplay?.Info.DeviceName ?? _settings.Get(OutputDisplayKey);

        Displays.Clear();
        var all = _displayProvider.GetDisplays();
        for (var i = 0; i < all.Count; i++)
        {
            var d = all[i];
            var label = $"Pantalla {i + 1} · {d.Width}×{d.Height}{(d.IsPrimary ? " (principal)" : string.Empty)}";
            Displays.Add(new DisplayOption(d, label));
        }

        // Prefer the remembered display, then the first secondary one.
        SelectedDisplay =
            Displays.FirstOrDefault(o => o.Info.DeviceName == saved)
            ?? Displays.FirstOrDefault(o => !o.Info.IsPrimary)
            ?? Displays.FirstOrDefault();
    }

    [RelayCommand]
    private void GoLive()
    {
        if (PreviewSlide is null || SelectedDisplay is null)
            return;

        _presentation.GoLive(PreviewSlide);
        _projection.EnsureVisible(SelectedDisplay.Info);
        _settings.Set(OutputDisplayKey, SelectedDisplay.Info.DeviceName);
        IsProjecting = true;
        StatusText = "En vivo. F1 Clear · F2 Black · F3 Logo · Esc apaga la salida.";
    }

    [RelayCommand]
    private void ToggleClear() => _presentation.ToggleClear();

    [RelayCommand]
    private void ToggleBlack() => _presentation.ToggleBlack();

    [RelayCommand]
    private void ToggleLogo() => _presentation.ToggleLogo();

    [RelayCommand]
    private void ShowOverlay()
    {
        if (string.IsNullOrWhiteSpace(OverlayText) || SelectedDisplay is null)
            return;

        _presentation.ShowOverlay(OverlayText.Trim());
        _projection.EnsureVisible(SelectedDisplay.Info);
        IsProjecting = true;
        StatusText = "Aviso al pie visible sobre la salida.";
    }

    [RelayCommand]
    private void HideOverlay()
    {
        _presentation.HideOverlay();
        StatusText = "Aviso al pie retirado.";
    }

    [RelayCommand]
    private void HideOutput()
    {
        _projection.HideOutput();
        IsProjecting = false;
        StatusText = "Salida apagada.";
    }

    private void UpdateStateFlags()
    {
        IsClearActive = _presentation.State == OutputState.Clear;
        IsBlackActive = _presentation.State == OutputState.Black;
        IsLogoActive = _presentation.State == OutputState.Logo;
        IsOverlayActive = _presentation.OverlayMessage is not null;
    }
}
