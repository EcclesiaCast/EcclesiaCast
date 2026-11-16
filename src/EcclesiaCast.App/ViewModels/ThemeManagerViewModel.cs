using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// One theme in the list of the theme manager: the theme as it is saved, a
/// small picture of how it looks and what it is the default for.
/// </summary>
public sealed partial class ThemeRow(SlideTheme theme) : ObservableObject
{
    /// <summary>The theme as it is saved; the edits in progress live in the editor's fields.</summary>
    public SlideTheme Theme { get; } = theme;

    public string Name => Theme.Name;

    public string KindLabel => Theme.Kind == ThemeKind.Bible ? "Biblia" : "Canciones";

    /// <summary>«En uso: canciones», «En uso: Biblia»… or empty when nothing uses it by default.</summary>
    [ObservableProperty]
    private string _defaultLabel = string.Empty;

    /// <summary>What the small picture shows. For the theme being edited, it follows the edits.</summary>
    [ObservableProperty]
    private SlideContent? _preview;
}

/// <summary>A ready-made look with a small picture of it, as the gallery of looks shows it.</summary>
public sealed record PresetCard(ThemePreset Preset, SlideContent Preview);

/// <summary>Backs the theme manager window: list, editor fields, and live preview.</summary>
public sealed partial class ThemeManagerViewModel : ObservableObject
{
    /// <summary>The section of the editor that shows the Bible-only fields.</summary>
    private const int BibleSection = 5;

    /// <summary>The section a theme opens on: the lettering.</summary>
    private const int LetterSection = 0;

    private readonly IThemeRepository _themes;
    private readonly ISettingsStore _settings;
    private bool _loading;

    /// <summary>True while the list is filled again: selection changes in between ask nothing.</summary>
    private bool _reloadingList;

    /// <summary>True while a cancelled change of theme puts the selection back.</summary>
    private bool _restoringSelection;

    /// <summary>The fields as they were when the theme was loaded or saved; see <see cref="IsDirty"/>.</summary>
    private string _baseline = string.Empty;

    /// <summary>Bumped on each load, so a late <see cref="SettleBaseline"/> of an older load does nothing.</summary>
    private int _loadVersion;

    public ObservableCollection<ThemeRow> Rows { get; } = [];

    [ObservableProperty]
    private ThemeRow? _selectedRow;

    /// <summary>The saved theme being edited.</summary>
    public SlideTheme? SelectedTheme => SelectedRow?.Theme;

    [ObservableProperty]
    private SlideContent? _previewSlide;

    /// <summary>
    /// True only while the preview plays the transition example. The rest
    /// of the time it changes without movement, or every tweak of a colour
    /// or a size would make the preview slide around.
    /// </summary>
    [ObservableProperty]
    private bool _animatePreview;

    /// <summary>Which sample the preview shows: the first text, or the one that follows it.</summary>
    private bool _showingNextSample;

    /// <summary>Bumped on each example, so an older one stops when a newer one starts.</summary>
    private int _sampleRun;

    // ── Cómo se mira la vista previa (no es parte del tema) ───────

    /// <summary>Which group of fields is open: 0 letra · 1 sombra y franjas · 2 fondo · 3 leyenda · 4 transición · 5 Biblia.</summary>
    [ObservableProperty]
    private int _sectionIndex = LetterSection;

    /// <summary>
    /// What stands behind the preview where the theme lets the background
    /// through: 0 the chequered board · 1 a dark picture · 2 a light one. It
    /// answers «will the words read over my video?» without leaving the editor.
    /// </summary>
    [ObservableProperty]
    private int _backdropIndex;

    /// <summary>Whether the text box and its handles are drawn over the preview.</summary>
    [ObservableProperty]
    private bool _showBox = true;

    /// <summary>A long sample (four lines, a whole verse) instead of the short one, to see the letter shrink.</summary>
    [ObservableProperty]
    private bool _longSample;

    /// <summary>
    /// True when the fields no longer match the saved theme. The window
    /// shows it and asks before the changes are lost (another theme, close).
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RevertChangesCommand))]
    private bool _isDirty;

    /// <summary>
    /// Asks what to do with the unsaved changes of a theme, by its name:
    /// true saves them, false throws them away, null stays where it was.
    /// A property so the tests can answer without a dialog.
    /// </summary>
    public Func<string, bool?> AskToSave { get; set; } = AskWithDialog;

    partial void OnTransitionIndexChanged(int value) => PlayTransitionSampleCommand.Execute(null);

    partial void OnTransitionMsChanged(double value)
    {
        OnPropertyChanged(nameof(TransitionSeconds));
        PlayTransitionSampleCommand.Execute(null);
    }

    /// <summary>
    /// The transition's length in seconds, as the operator thinks of it. The
    /// theme keeps it in milliseconds (<see cref="TransitionMs"/>), so saved
    /// themes need no conversion.
    /// </summary>
    public double TransitionSeconds
    {
        get => TransitionMs / 1000.0;
        set => TransitionMs = Math.Round(Math.Clamp(value, 0, MaxTransitionMs / 1000.0) * 1000);
    }

    /// <summary>The longest a transition may take; also the most the slide view will play.</summary>
    private const double MaxTransitionMs = 5000;

    /// <summary>
    /// Shows the chosen transition on the preview: the next text comes in and
    /// the first one goes out, then back again, so both directions are seen.
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task PlayTransitionSample()
    {
        if (_loading)
            return;

        var run = ++_sampleRun;
        var pause = TimeSpan.FromMilliseconds(Math.Clamp(TransitionMs, 0, MaxTransitionMs) + 900);

        AnimatePreview = true;
        _showingNextSample = true;
        UpdatePreview();

        await Task.Delay(pause);
        if (run != _sampleRun)
            return;

        _showingNextSample = false;
        UpdatePreview();

        await Task.Delay(pause);
        if (run == _sampleRun)
            AnimatePreview = false;
    }

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
    [ObservableProperty] private int _caseIndex;                       // orden de TextCase
    [ObservableProperty] private int _transitionIndex = 1;             // orden de SlideTransition
    [ObservableProperty] private double _transitionMs = 280;
    [ObservableProperty] private bool _shadow = true;
    [ObservableProperty] private double _shadowOpacityPercent = 75;    // 0–100
    [ObservableProperty] private double _shadowBlur = 18;
    [ObservableProperty] private double _outlineWidth;
    [ObservableProperty] private string _outlineColor = "#000000";
    [ObservableProperty] private int _lineBandIndex;                   // 0 sin franjas · 1 al ancho del texto · 2 de borde a borde
    [ObservableProperty] private string _lineBandColor = "#000000";
    [ObservableProperty] private double _lineBandOpacityPercent = 100;
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
    [ObservableProperty] private string _captionFontFamily = "Segoe UI";
    [ObservableProperty] private string _captionColor = "#B9C6DE";
    [ObservableProperty] private bool _showVersionName = true;
    [ObservableProperty] private bool _showVerseNumbers;
    [ObservableProperty] private bool _showSelahAndTitles;

    // Segunda versión bíblica
    [ObservableProperty] private bool _secondaryMatchesPrimary;
    [ObservableProperty] private double _secondaryScalePercent = 62;   // 0–100
    [ObservableProperty] private string _secondaryColor = "#C9D4E8";
    [ObservableProperty] private bool _secondaryItalic = true;

    // ── Tema de una canción ──────────────────────────────────────

    /// <summary>An entry of «Partir de» in a song's theme: a theme the song can take.</summary>
    public sealed record SongThemeOption(int? Id, string Label, SlideTheme Theme);

    private readonly SongThemes _songThemes;

    /// <summary>The song whose theme is being edited; null when the window edits the shared themes.</summary>
    private readonly Song? _song;

    /// <summary>
    /// The look as the editor first showed the song's theme, to tell whether
    /// the operator changed anything. Built by the editor itself, so the small
    /// rounding of its fields is not mistaken for a change.
    /// </summary>
    private SlideTheme? _songBaseline;

    /// <summary>The theme the song's editor started from (null: the main one).</summary>
    private int? _startedFrom;

    /// <summary>True when the window edits one song's theme instead of the shared themes.</summary>
    public bool IsSongMode => _song is not null;

    public bool IsThemeMode => _song is null;

    public string WindowTitle => _song is null ? "Temas de proyección" : $"Tema de la canción «{_song.Title}»";

    /// <summary>The themes the song can start from: its own, the main one and the other shared ones.</summary>
    public ObservableCollection<SongThemeOption> SongThemeOptions { get; } = [];

    [ObservableProperty]
    private SongThemeOption? _selectedSongThemeOption;

    /// <summary>What saving will do to the song, said in words.</summary>
    [ObservableProperty]
    private string _songThemeNote = string.Empty;

    /// <summary>The song does not follow the main theme now, so «Volver al tema principal» means something.</summary>
    public bool CanReturnToMain { get; }

    /// <summary>The theme on the editor is the main song theme: «Aplicar a todas las canciones» is offered.</summary>
    [ObservableProperty]
    private bool _isMainSongTheme;

    public ThemeManagerViewModel(IThemeRepository themes, ISettingsStore settings, ISongRepository songs, Song? song = null)
    {
        _themes = themes;
        _settings = settings;
        _song = song;
        _songThemes = new SongThemes(themes, songs);

        if (song is null)
        {
            LoadThemes(null);
            return;
        }

        CanReturnToMain = _songThemes.StateOf(song) != SongThemeState.FollowsMain;
        LoadSongThemeOptions(song);
    }

    private void LoadSongThemeOptions(Song song)
    {
        var mainId = ThemeSeeder.GetDefaultId(_settings, ThemeSeeder.DefaultSongThemeKey);
        var main = (mainId is int id ? _themes.Get(id) : null) ?? SlideTheme.Fallback;

        if (_songThemes.OwnThemeOf(song) is { } own)
            SongThemeOptions.Add(new SongThemeOption(own.Id, "Tema propio de esta canción", own));
        SongThemeOptions.Add(new SongThemeOption(null, $"Tema principal ({main.Name})", main));

        // The other shared song themes, and the one chosen for this song even
        // when it is not a song theme.
        foreach (var theme in _themes.GetAll())
        {
            if (theme.Id != main.Id && (theme.Kind == ThemeKind.Song || theme.Id == song.ThemeId))
                SongThemeOptions.Add(new SongThemeOption(theme.Id, theme.Name, theme));
        }

        SelectedSongThemeOption =
            SongThemeOptions.FirstOrDefault(o => o.Id is not null && o.Id == song.ThemeId)
            ?? SongThemeOptions.First(o => o.Id is null);
    }

    partial void OnSelectedSongThemeOptionChanged(SongThemeOption? value)
    {
        if (value is null)
            return;

        // Picking another theme starts over from it: nothing is asked. The
        // theme goes on the editor as a row of its own, outside the list, so
        // everything that works on the selected row works on it.
        _startedFrom = value.Id;
        _songBaseline = null;
        _reloadingList = true;
        try
        {
            SelectedRow = new ThemeRow(value.Theme);
        }
        finally
        {
            _reloadingList = false;
        }

        _songBaseline = BuildTheme(0);
        UpdateSongThemeNote();
    }

    private bool HasSongEdits => _songBaseline is not null && !SongThemes.SameLook(BuildTheme(0), _songBaseline);

    private void UpdateSongThemeNote()
    {
        if (_song is null || _songBaseline is null || SelectedSongThemeOption is not { } option)
            return;

        SongThemeNote = HasSongEdits
            ? "Con cambios: al guardar, este pasa a ser el tema propio de la canción. "
              + "Cambiar el tema principal ya no la va a tocar."
            : option.Id is null
                ? "Sigue al tema principal: si cambiás el principal, cambia también acá. "
                  + "Si cambiás algo acá, la canción pasa a tener su tema propio."
                : option.Theme.OwnerSongId is not null
                    ? "Tiene tema propio: cambiar el tema principal no la toca. "
                      + "Para que lo vuelva a seguir, «Volver al tema principal»."
                    : $"Usa el tema «{option.Theme.Name}» y sigue sus cambios. "
                      + "Si cambiás algo acá, la canción pasa a tener su tema propio.";
    }

    private void UpdateIsMainSongTheme() =>
        IsMainSongTheme = _song is null
            && SelectedTheme is not null
            && SelectedTheme.Id == ThemeSeeder.GetDefaultId(_settings, ThemeSeeder.DefaultSongThemeKey);

    /// <summary>
    /// What changes without being part of the theme: it neither redraws the
    /// preview nor counts as an edit.
    /// </summary>
    private static readonly HashSet<string> ViewState =
    [
        nameof(PreviewSlide), nameof(SelectedRow), nameof(SelectedTheme), nameof(IsBibleKind),
        nameof(StatusText), nameof(AnimatePreview), nameof(IsDirty), nameof(SectionIndex),
        nameof(BackdropIndex), nameof(ShowBox), nameof(PresetCards),
        nameof(SongThemeNote), nameof(SelectedSongThemeOption), nameof(IsMainSongTheme),
    ];

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is null || ViewState.Contains(e.PropertyName))
            return;

        if (e.PropertyName == nameof(KindIndex))
        {
            OnPropertyChanged(nameof(IsBibleKind));
            OnPropertyChanged(nameof(PresetCards));

            // The Bible fields go away with the kind: don't leave an empty section open.
            if (!IsBibleKind && SectionIndex == BibleSection)
                SectionIndex = LetterSection;
        }

        if (_loading)
            return;

        // Any change of a field redraws the preview. The transition plays
        // its own example instead (PlayTransitionSample).
        if (e.PropertyName is not (nameof(TransitionIndex) or nameof(TransitionMs) or nameof(TransitionSeconds)))
            UpdatePreview();

        IsDirty = SelectedRow is not null && Fingerprint() != _baseline;
    }

    partial void OnSelectedRowChanged(ThemeRow? oldValue, ThemeRow? newValue)
    {
        OnPropertyChanged(nameof(SelectedTheme));
        SetDefaultForSongsCommand.NotifyCanExecuteChanged();
        SetDefaultForBibleCommand.NotifyCanExecuteChanged();

        if (_restoringSelection)
            return;

        // Leaving a theme with changes: they are not thrown away in silence.
        if (!_reloadingList && oldValue is not null && IsDirty)
        {
            switch (AskToSave(oldValue.Name))
            {
                case null:
                    // Stay on the theme being edited. The list is put back once
                    // it has finished its own change of selection.
                    _restoringSelection = true;
                    Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                    {
                        SelectedRow = oldValue;
                        _restoringSelection = false;
                    });
                    return;

                case true:
                    SaveEdits(oldValue);
                    break;

                default:
                    oldValue.Preview = Sample(oldValue.Theme);
                    break;
            }
        }

        if (newValue is not null)
            LoadEditorFrom(newValue.Theme);
        UpdateIsMainSongTheme();
    }

    private void LoadThemes(int? keepId)
    {
        _reloadingList = true;
        try
        {
            Rows.Clear();
            foreach (var theme in _themes.GetAll())
                Rows.Add(new ThemeRow(theme) { Preview = Sample(theme) });

            SelectedRow = null;
            SelectedRow = Rows.FirstOrDefault(r => r.Theme.Id == keepId) ?? Rows.FirstOrDefault();
        }
        finally
        {
            _reloadingList = false;
        }

        UpdateDefaults();
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
        CaseIndex = (int)theme.TextCase;
        TransitionIndex = (int)theme.Transition;
        TransitionMs = theme.TransitionMs;
        Shadow = theme.Shadow;
        ShadowOpacityPercent = theme.ShadowOpacity * 100;
        ShadowBlur = theme.ShadowBlur;
        OutlineWidth = theme.OutlineWidth;
        OutlineColor = theme.OutlineColor;
        LineBandIndex = (int)theme.LineBand;
        LineBandColor = theme.LineBandColor;
        LineBandOpacityPercent = theme.LineBandOpacity * 100;
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
        CaptionFontFamily = theme.CaptionFontFamily ?? theme.FontFamily;
        CaptionColor = theme.CaptionColor;
        ShowVersionName = theme.ShowVersionName;
        ShowVerseNumbers = theme.ShowVerseNumbers;
        ShowSelahAndTitles = theme.ShowSelahAndTitles;
        SecondaryMatchesPrimary = theme.SecondaryMatchesPrimary;
        SecondaryScalePercent = theme.SecondaryScale * 100;
        SecondaryColor = theme.SecondaryColor;
        SecondaryItalic = theme.SecondaryItalic;
        _loading = false;

        _showingNextSample = false;
        UpdatePreview();

        // The fields of the window may still round or clamp what they were
        // given (a size below its minimum): that is not an edit of the
        // operator's, so the starting point is taken again once they settle.
        _baseline = Fingerprint();
        IsDirty = false;
        var version = ++_loadVersion;
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (version == _loadVersion)
                SettleBaseline();
        });
    }

    /// <summary>
    /// Takes the fields as they are now for «nothing changed yet». Called
    /// once the window has finished filling its fields after a load.
    /// </summary>
    public void SettleBaseline()
    {
        _baseline = Fingerprint();
        IsDirty = false;

        if (_song is not null)
        {
            _songBaseline = BuildTheme(0);
            UpdateSongThemeNote();
        }
    }

    /// <summary>The fields as one text: two equal texts mean the same theme.</summary>
    private string Fingerprint() => JsonSerializer.Serialize(BuildTheme(0));

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
        TextCase = (TextCase)Math.Clamp(CaseIndex, 0, 4),
        Transition = (SlideTransition)Math.Clamp(TransitionIndex, 0, 4),
        TransitionMs = Math.Clamp(TransitionMs, 0, MaxTransitionMs),
        Shadow = Shadow,
        ShadowOpacity = Math.Clamp(ShadowOpacityPercent / 100, 0, 1),
        ShadowBlur = Math.Clamp(ShadowBlur, 0, 80),
        OutlineWidth = Math.Clamp(OutlineWidth, 0, 40),
        OutlineColor = string.IsNullOrWhiteSpace(OutlineColor) ? "#000000" : OutlineColor.Trim(),
        LineBand = (LineBand)Math.Clamp(LineBandIndex, 0, 2),
        LineBandColor = string.IsNullOrWhiteSpace(LineBandColor) ? "#000000" : LineBandColor.Trim(),
        LineBandOpacity = Math.Clamp(LineBandOpacityPercent / 100, 0, 1),
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
        CaptionFontFamily = string.IsNullOrWhiteSpace(CaptionFontFamily) ? null : CaptionFontFamily.Trim(),
        CaptionColor = CaptionColor,
        ShowVersionName = ShowVersionName,
        ShowVerseNumbers = ShowVerseNumbers,
        ShowSelahAndTitles = ShowSelahAndTitles,
        SecondaryMatchesPrimary = SecondaryMatchesPrimary,
        SecondaryScale = Math.Clamp(SecondaryScalePercent / 100, 0.2, 1.5),
        SecondaryColor = SecondaryColor,
        SecondaryItalic = SecondaryItalic,
    };

    private void UpdatePreview()
    {
        PreviewSlide = Sample(BuildTheme(0), _showingNextSample, LongSample);

        // The small picture in the list follows the edits of its theme.
        if (SelectedRow is not null)
            SelectedRow.Preview = PreviewSlide;

        UpdateSongThemeNote();
    }

    /// <summary>
    /// A slide to look at a theme with: a public-domain hymn for the songs,
    /// John 3:16 in two free versions for the Bible. The transition example
    /// swaps to the text that follows and back, so there is something to
    /// watch come in and go out; the long sample puts both together.
    /// </summary>
    private static SlideContent Sample(SlideTheme theme, bool next = false, bool longText = false)
    {
        var isBible = theme.Kind == ThemeKind.Bible;

        const string SongFirst = "Santo, santo, santo, Señor omnipotente,\nsiempre el labio mío loores te dará.";
        const string SongNext = "Santo, santo, santo, te adoro reverente,\nDios en tres personas, bendita Trinidad.";
        const string VerseFirst = "Porque de tal manera amó Dios al mundo, que ha dado á su Hijo unigénito";
        const string VerseNext = "para que todo aquel que en él cree, no se pierda, mas tenga vida eterna";
        const string SecondFirst = "Porque tanto amó Dios al mundo, que dio a su Hijo unigénito";
        const string SecondNext = "para que todo el que crea en él no perezca, sino que tenga vida eterna";

        var text = (isBible, longText, next) switch
        {
            (true, true, _) => $"{VerseFirst}, {VerseNext}.",
            (true, false, false) => VerseFirst,
            (true, false, true) => VerseNext,
            (false, true, _) => $"{SongFirst}\n{SongNext}",
            (false, false, false) => SongFirst,
            (false, false, true) => SongNext,
        };

        if (!isBible)
            return new SlideContent(text, "Santo, santo, santo — Reginald Heber", null, theme);

        if (theme.ShowVerseNumbers)
            text = "16 " + text;

        // A Bible theme shows a second version too, so its style is visible;
        // with two versions each text carries its abbreviation inline.
        var secondary = longText ? $"{SecondFirst}, {SecondNext}." : next ? SecondNext : SecondFirst;
        return new SlideContent($"[RV1909] {text}", "Juan 3:16", $"[BLM] {secondary}", theme);
    }

    /// <summary>Marks in the list which theme the songs and the Bible use by default.</summary>
    private void UpdateDefaults()
    {
        var songId = ThemeSeeder.GetDefaultId(_settings, ThemeSeeder.DefaultSongThemeKey);
        var bibleId = ThemeSeeder.GetDefaultId(_settings, ThemeSeeder.DefaultBibleThemeKey);

        foreach (var row in Rows)
        {
            var songs = row.Theme.Id == songId;
            var bible = row.Theme.Id == bibleId;
            row.DefaultLabel = (songs, bible) switch
            {
                (true, true) => "En uso: canciones y Biblia",
                (true, false) => "En uso: canciones",
                (false, true) => "En uso: Biblia",
                _ => string.Empty,
            };
        }

        SetDefaultForSongsCommand.NotifyCanExecuteChanged();
        SetDefaultForBibleCommand.NotifyCanExecuteChanged();
        UpdateIsMainSongTheme();
    }

    // ── Comandos ─────────────────────────────────────────────────

    /// <summary>Raised when the operator saves — the window closes in response.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Saves the theme on the editor. For a song, what it saves depends on
    /// what was done: any change becomes the song's own theme; otherwise the
    /// song takes the theme it started from.
    ///
    /// Saving a shared theme reaches only the songs that follow it: a song
    /// with its own theme stays as it is until «Aplicar a todas las canciones».
    /// </summary>
    [RelayCommand]
    private void SaveTheme()
    {
        if (SelectedRow is null)
            return;

        SaveEdits(SelectedRow);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Saves the fields over a theme of the list. The fields are the ones of
    /// the theme being left when the operator is moving to another.
    /// </summary>
    private void SaveEdits(ThemeRow row)
    {
        if (_song is not null)
        {
            // Never over the theme the song started from: that one is shared.
            _songThemes.Save(_song, _startedFrom, HasSongEdits ? BuildTheme(0) : null);
            ChangesMade = true;
            _baseline = Fingerprint();
            IsDirty = false;
            return;
        }

        var saved = _themes.Save(BuildTheme(row.Theme.Id));
        ChangesMade = true;

        if (row == SelectedRow)
        {
            // Still on it: what is saved is what the fields show.
            _baseline = Fingerprint();
            IsDirty = false;
        }
        else if (Rows.IndexOf(row) is var index and >= 0)
        {
            var fresh = new ThemeRow(saved) { Preview = Sample(saved) };
            Rows[index] = fresh;
            UpdateDefaults();
        }
    }

    /// <summary>The song drops its own (or chosen) theme and follows the main one again.</summary>
    [RelayCommand]
    private void ReturnToMainTheme()
    {
        if (_song is null)
            return;

        _songThemes.FollowMain(_song);
        ChangesMade = true;

        // What is on the editor is given up: closing asks nothing.
        _baseline = Fingerprint();
        IsDirty = false;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Before something that would lose the unsaved changes (closing, a new
    /// theme): asks, and saves if told to. False means «stay here».
    /// </summary>
    public bool ResolveUnsaved()
    {
        if (!IsDirty || SelectedRow is null)
            return true;

        switch (AskToSave(SelectedRow.Name))
        {
            case null:
                return false;
            case true:
                SaveEdits(SelectedRow);
                return true;
            default:
                return true;
        }
    }

    private static bool? AskWithDialog(string themeName)
    {
        return Notice.Choose(
            $"Cambiaste el tema «{themeName}» y todavía no lo guardaste.\n\n"
            + "Si no lo guardás, el tema queda como estaba.",
            "Guardar", "No guardar", "Seguir editando",
            "¿Guardar los cambios?");
    }

    /// <summary>Puts the fields back as the theme is saved.</summary>
    [RelayCommand(CanExecute = nameof(IsDirty))]
    private void RevertChanges()
    {
        if (SelectedRow is null)
            return;

        LoadEditorFrom(SelectedRow.Theme);
        BoxReplaced?.Invoke(this, EventArgs.Empty);
        StatusText = "El tema volvió a como estaba guardado.";
    }

    /// <summary>
    /// «Aplicar a todas las canciones»: saves the main theme as it is on the
    /// editor and puts every song back on it, after saying how many songs
    /// lose their own look. The one way an edit of the main theme reaches the
    /// songs that left it.
    /// </summary>
    [RelayCommand]
    private void ApplyToAllSongs()
    {
        if (_song is not null || SelectedRow is null || !IsMainSongTheme)
            return;

        var survey = _songThemes.Survey();
        var name = string.IsNullOrWhiteSpace(ThemeName) ? SelectedRow.Name : ThemeName.Trim();
        if (survey.Apart == 0 && survey.Designed == 0)
        {
            SaveSelectedKeepingOpen();
            StatusText = "Todas las canciones ya siguen este tema.";
            return;
        }

        var lines = new List<string>();
        if (survey.Own > 0)
            lines.Add($"• {survey.Own} canción(es) con tema propio lo pierden.");
        if (survey.Chosen > 0)
            lines.Add($"• {survey.Chosen} canción(es) con otro tema elegido pasan a este.");
        if (survey.Designed > 0)
            lines.Add($"• {survey.Designed} canción(es) tienen diapositivas diseñadas a mano "
                + "(letra, color o caja cambiados en «Editar canción (diseño)»).");

        var message =
            $"Todas las canciones van a seguir el tema principal «{name}», con lo que tenés en el editor.\n\n"
            + string.Join("\n", lines)
            + "\n\nNo se puede deshacer.";

        // Each answer says what it does; the last one always backs out.
        var options = new List<(string Label, bool? ClearDesigns)>();
        if (survey.Apart > 0)
            options.Add((survey.Designed > 0
                ? "Aplicar el tema (los diseños por diapositiva se quedan)"
                : "Aplicar el tema a todas las canciones", false));
        if (survey.Designed > 0)
            options.Add(("Aplicar el tema y borrar también los diseños por diapositiva", true));
        options.Add(("Cancelar", null));

        var app = Application.Current;
        var window = new Views.ChoiceWindow("Aplicar a todas las canciones", message, options.Select(o => o.Label).ToList())
        {
            Owner = app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? app.MainWindow,
        };
        if (window.ShowDialog() != true || window.Chosen < 0 || options[window.Chosen].ClearDesigns is not bool clear)
            return;

        SaveSelectedKeepingOpen();
        var changed = _songThemes.ApplyMainToAll(clear);
        StatusText = $"Listo: {changed} canción(es) vuelven a seguir este tema.";
    }

    /// <summary>Saves the theme on the editor without closing the window.</summary>
    private void SaveSelectedKeepingOpen()
    {
        if (SelectedRow is null)
            return;

        var saved = _themes.Save(BuildTheme(SelectedRow.Theme.Id));
        ChangesMade = true;
        LoadThemes(saved.Id);
    }

    /// <summary>Raised when a look moves the text box, so the window redraws its handles.</summary>
    public event EventHandler? BoxReplaced;

    /// <summary>
    /// The ready-made looks, each with a small picture of itself on a theme of
    /// the kind being edited (a song or a verse).
    /// </summary>
    public IReadOnlyList<PresetCard> PresetCards
    {
        get
        {
            var kind = IsBibleKind ? ThemeKind.Bible : ThemeKind.Song;
            if (_presetCards is null || _presetCardsKind != kind)
            {
                _presetCardsKind = kind;
                _presetCards =
                [
                    .. ThemePresets.All.Select(preset =>
                        new PresetCard(preset, Sample(preset.ApplyTo(new SlideTheme { Kind = kind })))),
                ];
            }

            return _presetCards;
        }
    }

    private IReadOnlyList<PresetCard>? _presetCards;
    private ThemeKind _presetCardsKind;

    /// <summary>Puts a ready-made look on the theme being edited. Nothing is saved until «Guardar».</summary>
    [RelayCommand]
    private void ApplyPreset(PresetCard? card)
    {
        if (card is null || SelectedRow is null)
            return;

        var look = card.Preset.ApplyTo(BuildTheme(SelectedRow.Theme.Id));
        LoadFieldsKeepingBaseline(look);
        StatusText = $"Plantilla «{card.Preset.Name}» puesta. Guardá para conservarla.";
    }

    /// <summary>The text box back to the whole screen, with the usual margins.</summary>
    [RelayCommand]
    private void ResetBox()
    {
        BoxX = 110;
        BoxY = 80;
        BoxWidth = 1700;
        BoxHeight = 920;
        BoxReplaced?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Fills the fields from a theme that is not the saved one (a look just
    /// applied): the saved theme stays the point the changes are counted from.
    /// </summary>
    private void LoadFieldsKeepingBaseline(SlideTheme theme)
    {
        var baseline = _baseline;
        LoadEditorFrom(theme);
        _loadVersion++; // the settle of that load would take the look for «saved»
        _baseline = baseline;
        IsDirty = Fingerprint() != _baseline;
        BoxReplaced?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void NewTheme()
    {
        if (!ResolveUnsaved())
            return;

        var created = _themes.Save(new SlideTheme { Name = "Nuevo tema", Kind = ThemeKind.Song, TransparentBackground = true });
        ChangesMade = true;
        LoadThemes(created.Id);
        StatusText = "Tema nuevo: elegí una plantilla y retocala.";
    }

    [RelayCommand]
    private void DuplicateTheme()
    {
        if (SelectedRow is null)
            return;

        // The copy takes what is on screen, changes included; the original
        // stays as it is saved.
        var copy = BuildTheme(0);
        copy.Name = $"{copy.Name} (copia)";
        var saved = _themes.Save(copy);
        ChangesMade = true;
        LoadThemes(saved.Id);
    }

    [RelayCommand]
    private void DeleteTheme()
    {
        if (SelectedRow is null)
            return;

        if (Rows.Count <= 1)
        {
            Notice.Show("Tiene que quedar al menos un tema.");
            return;
        }

        var confirm = Notice.Confirm(
            $"¿Eliminar el tema \"{SelectedRow.Name}\"? Las canciones que lo usen vuelven al tema por defecto.\n\n"
            + "Va a la papelera y se guarda 30 días (Edición → Papelera).",
            "Eliminar", destructive: true);
        if (!confirm)
            return;

        _themes.Delete(SelectedRow.Theme.Id);
        ThemeSeeder.EnsureDefaults(_themes, _settings);
        ChangesMade = true;
        LoadThemes(null);
    }

    private bool CanSetDefaultForSongs() =>
        SelectedRow is not null
        && ThemeSeeder.GetDefaultId(_settings, ThemeSeeder.DefaultSongThemeKey) != SelectedRow.Theme.Id;

    private bool CanSetDefaultForBible() =>
        SelectedRow is not null
        && ThemeSeeder.GetDefaultId(_settings, ThemeSeeder.DefaultBibleThemeKey) != SelectedRow.Theme.Id;

    [RelayCommand(CanExecute = nameof(CanSetDefaultForSongs))]
    private void SetDefaultForSongs()
    {
        if (SelectedRow is null)
            return;
        _settings.Set(ThemeSeeder.DefaultSongThemeKey, SelectedRow.Theme.Id.ToString());
        ChangesMade = true;
        UpdateDefaults();
        StatusText = $"Las canciones ahora usan «{SelectedRow.Name}».";
    }

    [RelayCommand(CanExecute = nameof(CanSetDefaultForBible))]
    private void SetDefaultForBible()
    {
        if (SelectedRow is null)
            return;
        _settings.Set(ThemeSeeder.DefaultBibleThemeKey, SelectedRow.Theme.Id.ToString());
        ChangesMade = true;
        UpdateDefaults();
        StatusText = $"La Biblia ahora usa «{SelectedRow.Name}».";
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
        var picked = ColorPickerHelper.Pick(BackgroundColor, allowTransparent: true);
        if (picked is null)
            return;

        if (picked == ColorPickerHelper.Transparent)
        {
            TransparentBackground = true;
        }
        else
        {
            TransparentBackground = false;
            BackgroundColor = picked;
        }
    }

    [RelayCommand]
    private void PickLineBandColor()
    {
        var picked = ColorPickerHelper.Pick(LineBandColor);
        if (picked is not null)
            LineBandColor = picked;
    }

    /// <summary>
    /// «Negras» o «Blancas»: the band and a letter that reads on it. Without
    /// bands yet, they come on as wide as the text.
    /// </summary>
    [RelayCommand]
    private void SetLineBandShade(string? shade)
    {
        var white = shade == "white";
        if (LineBandIndex == 0)
            LineBandIndex = 1;
        LineBandColor = white ? "#FFFFFF" : "#000000";
        TextColor = white ? "#111111" : "#FFFFFF";
        LineBandOpacityPercent = 100;
    }

    [RelayCommand]
    private void PickOutlineColor()
    {
        var picked = ColorPickerHelper.Pick(OutlineColor);
        if (picked is not null)
            OutlineColor = picked;
    }

    [RelayCommand]
    private void PickCaptionColor()
    {
        var picked = ColorPickerHelper.Pick(CaptionColor);
        if (picked is not null)
            CaptionColor = picked;
    }

    [RelayCommand]
    private void PickSecondaryColor()
    {
        var picked = ColorPickerHelper.Pick(SecondaryColor);
        if (picked is not null)
            SecondaryColor = picked;
    }
}
