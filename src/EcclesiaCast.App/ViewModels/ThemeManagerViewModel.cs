using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.App.ViewModels;

/// <summary>Backs the theme manager window: list, editor fields, and live preview.</summary>
public sealed partial class ThemeManagerViewModel : ObservableObject
{
    private readonly IThemeRepository _themes;
    private readonly ISettingsStore _settings;
    private bool _loading;

    public ObservableCollection<SlideTheme> Themes { get; } = [];

    [ObservableProperty]
    private SlideTheme? _selectedTheme;

    [ObservableProperty]
    private SlideContent? _previewSlide;

    [ObservableProperty]
    private string _defaultsText = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    /// <summary>True if anything was saved, deleted, or set as default.</summary>
    public bool ChangesMade { get; private set; }

    // ── Campos del editor ────────────────────────────────────────

    /// <summary>True when the theme is a Bible theme — gates the Bible-only fields.</summary>
    public bool IsBibleKind => KindIndex == 1;

    [ObservableProperty] private string _themeName = string.Empty;
    [ObservableProperty] private int _kindIndex;                       // 0 Canciones · 1 Biblia
    [ObservableProperty] private string _fontFamily = "Segoe UI";
    [ObservableProperty] private double _maxFontSize = 92;
    [ObservableProperty] private double _minFontSize = 36;
    [ObservableProperty] private bool _bold = true;
    [ObservableProperty] private bool _italic;
    [ObservableProperty] private bool _uppercase;
    [ObservableProperty] private bool _shadow = true;
    [ObservableProperty] private string _textColor = "#FFFFFF";
    [ObservableProperty] private int _alignHIndex = 1;                 // 0 Izq · 1 Centro · 2 Der
    [ObservableProperty] private int _alignVIndex = 1;                 // 0 Arriba · 1 Centro · 2 Abajo
    [ObservableProperty] private double _marginHorizontal = 110;
    [ObservableProperty] private double _marginVertical = 80;
    [ObservableProperty] private double _boxX = 110;
    [ObservableProperty] private double _boxY = 80;
    [ObservableProperty] private double _boxWidth = 1700;
    [ObservableProperty] private double _boxHeight = 920;
    [ObservableProperty] private bool _fitToWidth;
    [ObservableProperty] private string _backgroundColor = "#10141E";
    [ObservableProperty] private bool _transparentBackground;
    [ObservableProperty] private string? _backgroundImagePath;
    [ObservableProperty] private double _backgroundDimPercent;         // 0–100
    [ObservableProperty] private bool _showCaption = true;
    [ObservableProperty] private int _captionPositionIndex = 5;        // orden del enum
    [ObservableProperty] private double _captionFontSize = 40;
    [ObservableProperty] private bool _showVersionName = true;
    [ObservableProperty] private bool _showVerseNumbers;

    public ThemeManagerViewModel(IThemeRepository themes, ISettingsStore settings)
    {
        _themes = themes;
        _settings = settings;
        LoadThemes(null);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // Cualquier cambio de campo refresca la vista previa en vivo.
        if (!_loading
            && e.PropertyName is not (nameof(PreviewSlide) or nameof(SelectedTheme) or nameof(DefaultsText)
                or nameof(IsBibleKind) or nameof(StatusText)))
            UpdatePreview();

        if (e.PropertyName == nameof(KindIndex))
            OnPropertyChanged(nameof(IsBibleKind));
    }

    partial void OnSelectedThemeChanged(SlideTheme? value)
    {
        if (value is not null)
            LoadEditorFrom(value);
    }

    private void LoadThemes(int? keepId)
    {
        Themes.Clear();
        foreach (var theme in _themes.GetAll())
            Themes.Add(theme);

        SelectedTheme = Themes.FirstOrDefault(t => t.Id == keepId) ?? Themes.FirstOrDefault();
        UpdateDefaultsText();
    }

    private void LoadEditorFrom(SlideTheme theme)
    {
        _loading = true;
        ThemeName = theme.Name;
        KindIndex = theme.Kind == ThemeKind.Bible ? 1 : 0;
        FontFamily = theme.FontFamily;
        MaxFontSize = theme.MaxFontSize;
        MinFontSize = theme.MinFontSize;
        Bold = theme.Bold;
        Italic = theme.Italic;
        Uppercase = theme.Uppercase;
        Shadow = theme.Shadow;
        TextColor = theme.TextColor;
        AlignHIndex = (int)theme.AlignH;
        AlignVIndex = (int)theme.AlignV;
        MarginHorizontal = theme.MarginHorizontal;
        MarginVertical = theme.MarginVertical;

        // Use the theme's box if it has one; otherwise derive it from the margins.
        BoxX = theme.BoxX ?? theme.MarginHorizontal;
        BoxY = theme.BoxY ?? theme.MarginVertical;
        BoxWidth = theme.BoxWidth ?? (1920 - 2 * theme.MarginHorizontal);
        BoxHeight = theme.BoxHeight ?? (1080 - 2 * theme.MarginVertical);
        FitToWidth = theme.FitToWidth;

        BackgroundColor = theme.BackgroundColor;
        TransparentBackground = theme.TransparentBackground;
        BackgroundImagePath = theme.BackgroundImagePath;
        BackgroundDimPercent = theme.BackgroundDim * 100;
        ShowCaption = theme.ShowCaption;
        CaptionPositionIndex = (int)theme.CaptionPosition;
        CaptionFontSize = theme.CaptionFontSize;
        ShowVersionName = theme.ShowVersionName;
        ShowVerseNumbers = theme.ShowVerseNumbers;
        _loading = false;
        UpdatePreview();
    }

    private SlideTheme BuildTheme(int id) => new()
    {
        Id = id,
        Name = string.IsNullOrWhiteSpace(ThemeName) ? "Tema sin nombre" : ThemeName.Trim(),
        Kind = KindIndex == 1 ? ThemeKind.Bible : ThemeKind.Song,
        FontFamily = string.IsNullOrWhiteSpace(FontFamily) ? "Segoe UI" : FontFamily.Trim(),
        MaxFontSize = MaxFontSize,
        MinFontSize = Math.Min(MinFontSize, MaxFontSize),
        Bold = Bold,
        Italic = Italic,
        Uppercase = Uppercase,
        Shadow = Shadow,
        TextColor = TextColor,
        AlignH = (HAlign)Math.Clamp(AlignHIndex, 0, 2),
        AlignV = (VAlign)Math.Clamp(AlignVIndex, 0, 2),
        MarginHorizontal = MarginHorizontal,
        MarginVertical = MarginVertical,
        BoxX = BoxX,
        BoxY = BoxY,
        BoxWidth = BoxWidth,
        BoxHeight = BoxHeight,
        FitToWidth = FitToWidth,
        BackgroundColor = BackgroundColor,
        TransparentBackground = TransparentBackground,
        BackgroundImagePath = string.IsNullOrWhiteSpace(BackgroundImagePath) ? null : BackgroundImagePath,
        BackgroundDim = Math.Clamp(BackgroundDimPercent / 100, 0, 1),
        ShowCaption = ShowCaption,
        CaptionPosition = (CaptionPosition)Math.Clamp(CaptionPositionIndex, 0, 5),
        CaptionFontSize = CaptionFontSize,
        ShowVersionName = ShowVersionName,
        ShowVerseNumbers = ShowVerseNumbers,
    };

    private void UpdatePreview()
    {
        var theme = BuildTheme(0);
        var isBible = theme.Kind == ThemeKind.Bible;

        var text = isBible
            ? "Porque de tal manera amó Dios al mundo, que ha dado a su Hijo unigénito"
            : "Grande es tu fidelidad\noh Dios mi Padre";
        if (isBible && theme.ShowVerseNumbers)
            text = "16 " + text;

        var caption = isBible
            ? theme.ShowVersionName ? "Juan 3:16 · RVC" : "Juan 3:16"
            : "Grande es tu fidelidad — Marcos Witt";

        PreviewSlide = new SlideContent(text, caption, Theme: theme);
    }

    private void UpdateDefaultsText()
    {
        var songId = ThemeSeeder.GetDefaultId(_settings, ThemeSeeder.DefaultSongThemeKey);
        var bibleId = ThemeSeeder.GetDefaultId(_settings, ThemeSeeder.DefaultBibleThemeKey);
        var song = Themes.FirstOrDefault(t => t.Id == songId)?.Name ?? "—";
        var bible = Themes.FirstOrDefault(t => t.Id == bibleId)?.Name ?? "—";
        DefaultsText = $"Por defecto → Canciones: {song} · Biblia: {bible}";
    }

    // ── Comandos ─────────────────────────────────────────────────

    [RelayCommand]
    private void SaveTheme()
    {
        if (SelectedTheme is null)
            return;

        var saved = _themes.Save(BuildTheme(SelectedTheme.Id));
        ChangesMade = true;
        LoadThemes(saved.Id);
        StatusText = $"✓ \"{saved.Name}\" guardado.";
    }

    [RelayCommand]
    private void NewTheme()
    {
        var created = _themes.Save(new SlideTheme { Name = "Nuevo tema", Kind = ThemeKind.Song });
        ChangesMade = true;
        LoadThemes(created.Id);
    }

    [RelayCommand]
    private void DuplicateTheme()
    {
        if (SelectedTheme is null)
            return;

        var copy = BuildTheme(0);
        copy.Name = $"{copy.Name} (copia)";
        var saved = _themes.Save(copy);
        ChangesMade = true;
        LoadThemes(saved.Id);
    }

    [RelayCommand]
    private void DeleteTheme()
    {
        if (SelectedTheme is null)
            return;

        if (Themes.Count <= 1)
        {
            MessageBox.Show("Tiene que quedar al menos un tema.", "EcclesiaCast",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"¿Eliminar el tema \"{SelectedTheme.Name}\"? Las canciones que lo usen vuelven al tema por defecto.",
            "EcclesiaCast", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
            return;

        _themes.Delete(SelectedTheme.Id);
        ThemeSeeder.EnsureDefaults(_themes, _settings);
        ChangesMade = true;
        LoadThemes(null);
    }

    [RelayCommand]
    private void SetDefaultForSongs()
    {
        if (SelectedTheme is null)
            return;
        _settings.Set(ThemeSeeder.DefaultSongThemeKey, SelectedTheme.Id.ToString());
        ChangesMade = true;
        UpdateDefaultsText();
    }

    [RelayCommand]
    private void SetDefaultForBible()
    {
        if (SelectedTheme is null)
            return;
        _settings.Set(ThemeSeeder.DefaultBibleThemeKey, SelectedTheme.Id.ToString());
        ChangesMade = true;
        UpdateDefaultsText();
    }

    [RelayCommand]
    private void BrowseBackgroundImage()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Imagen de fondo",
            Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp;*.webp",
        };
        if (dialog.ShowDialog() == true)
            BackgroundImagePath = dialog.FileName;
    }

    [RelayCommand]
    private void ClearBackgroundImage() => BackgroundImagePath = null;

    [RelayCommand]
    private void PickTextColor()
    {
        var picked = ColorPickerHelper.Pick(TextColor);
        if (picked is not null)
            TextColor = picked;
    }

    [RelayCommand]
    private void PickBackgroundColor()
    {
        var picked = ColorPickerHelper.Pick(BackgroundColor);
        if (picked is not null)
            BackgroundColor = picked;
    }
}
