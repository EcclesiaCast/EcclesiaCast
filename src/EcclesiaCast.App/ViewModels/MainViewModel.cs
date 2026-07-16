using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;

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
    private readonly ISongRepository _songs;
    private readonly ISongEditor _songEditor;

    /// <summary>What the projector is showing; the Live box binds to it.</summary>
    public ProjectionViewModel Projection { get; }

    public ObservableCollection<DisplayOption> Displays { get; } = [];
    public ObservableCollection<Song> Songs { get; } = [];
    public ObservableCollection<SlideItemViewModel> SongSlides { get; } = [];

    [ObservableProperty]
    private DisplayOption? _selectedDisplay;

    [ObservableProperty]
    private string _statusText =
        "Creá una canción con ➕, o escribí un texto rápido y presioná Ctrl+Enter.";

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

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Song? _selectedSong;

    [ObservableProperty]
    private int _liveSlideIndex = -1;

    public MainViewModel(
        IDisplayProvider displayProvider,
        IProjectionWindowService projection,
        ISettingsStore settings,
        IPresentationService presentation,
        ISongRepository songs,
        ISongEditor songEditor,
        ProjectionViewModel projectionViewModel)
    {
        _displayProvider = displayProvider;
        _projection = projection;
        _settings = settings;
        _presentation = presentation;
        _songs = songs;
        _songEditor = songEditor;
        Projection = projectionViewModel;

        _presentation.Changed += (_, _) => UpdateStateFlags();
        UpdateStateFlags();
        RefreshDisplays();
        LoadSongs();
    }

    // ── Pantallas ────────────────────────────────────────────────

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

    // ── Canciones ────────────────────────────────────────────────

    partial void OnSearchTextChanged(string value) => LoadSongs();

    partial void OnSelectedSongChanged(Song? value) => BuildSlides();

    private void LoadSongs()
    {
        var keepId = SelectedSong?.Id;
        Songs.Clear();
        foreach (var song in _songs.Search(SearchText))
            Songs.Add(song);
        SelectedSong = Songs.FirstOrDefault(s => s.Id == keepId) ?? SelectedSong;
    }

    private void BuildSlides()
    {
        SongSlides.Clear();
        LiveSlideIndex = -1;

        if (SelectedSong is null)
            return;

        var caption = string.IsNullOrWhiteSpace(SelectedSong.Artist)
            ? SelectedSong.Title
            : $"{SelectedSong.Title} — {SelectedSong.Artist}";

        foreach (var section in SelectedSong.Sections)
        {
            SongSlides.Add(new SlideItemViewModel(
                SongSlides.Count,
                section.Label,
                new SlideContent(section.Text, caption)));
        }

        PreviewSlide = SongSlides.FirstOrDefault()?.Slide;
    }

    [RelayCommand]
    private void NewSong()
    {
        var created = _songEditor.Edit(null);
        if (created is null)
            return;

        var saved = _songs.Save(created);
        LoadSongs();
        SelectedSong = Songs.FirstOrDefault(s => s.Id == saved.Id);
        StatusText = $"Canción \"{saved.Title}\" guardada.";
    }

    [RelayCommand]
    private void EditSong()
    {
        if (SelectedSong is null)
            return;

        var edited = _songEditor.Edit(SelectedSong);
        if (edited is null)
            return;

        var saved = _songs.Save(edited);
        LoadSongs();
        SelectedSong = Songs.FirstOrDefault(s => s.Id == saved.Id);
        StatusText = $"Canción \"{saved.Title}\" actualizada.";
    }

    [RelayCommand]
    private void DeleteSong()
    {
        if (SelectedSong is null)
            return;

        var confirm = MessageBox.Show(
            $"¿Eliminar \"{SelectedSong.Title}\" de la biblioteca?",
            "EcclesiaCast",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
            return;

        _songs.Delete(SelectedSong.Id);
        SelectedSong = null;
        LoadSongs();
        StatusText = "Canción eliminada.";
    }

    // ── Proyección de slides ─────────────────────────────────────

    [RelayCommand]
    private void ProjectSlide(SlideItemViewModel item) => GoLiveSlide(item.Index);

    [RelayCommand]
    private void NextSlide() => GoLiveSlide(LiveSlideIndex < 0 ? 0 : LiveSlideIndex + 1);

    [RelayCommand]
    private void PreviousSlide() => GoLiveSlide(LiveSlideIndex - 1);

    private void GoLiveSlide(int index)
    {
        if (SelectedDisplay is null || index < 0 || index >= SongSlides.Count)
            return;

        var item = SongSlides[index];
        _presentation.GoLive(item.Slide);
        _projection.EnsureVisible(SelectedDisplay.Info);
        _settings.Set(OutputDisplayKey, SelectedDisplay.Info.DeviceName);
        IsProjecting = true;

        LiveSlideIndex = index;
        foreach (var slide in SongSlides)
            slide.IsLive = slide.Index == index;

        PreviewSlide = index + 1 < SongSlides.Count ? SongSlides[index + 1].Slide : null;
        StatusText = $"En vivo: {item.Label}. Flechas ←→ para navegar · F1 Clear · F2 Black · F3 Logo.";
    }

    // ── Texto rápido ─────────────────────────────────────────────

    partial void OnQuickTextChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            PreviewSlide = new SlideContent(value.Trim());
    }

    [RelayCommand]
    private void GoLive()
    {
        if (string.IsNullOrWhiteSpace(QuickText) || SelectedDisplay is null)
            return;

        _presentation.GoLive(new SlideContent(QuickText.Trim()));
        _projection.EnsureVisible(SelectedDisplay.Info);
        _settings.Set(OutputDisplayKey, SelectedDisplay.Info.DeviceName);
        IsProjecting = true;

        LiveSlideIndex = -1;
        foreach (var slide in SongSlides)
            slide.IsLive = false;

        StatusText = "Texto rápido en vivo. F1 Clear · F2 Black · F3 Logo · Esc apaga la salida.";
    }

    // ── Aviso al pie ─────────────────────────────────────────────

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

    // ── Estados de salida ────────────────────────────────────────

    [RelayCommand]
    private void ToggleClear() => _presentation.ToggleClear();

    [RelayCommand]
    private void ToggleBlack() => _presentation.ToggleBlack();

    [RelayCommand]
    private void ToggleLogo() => _presentation.ToggleLogo();

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
