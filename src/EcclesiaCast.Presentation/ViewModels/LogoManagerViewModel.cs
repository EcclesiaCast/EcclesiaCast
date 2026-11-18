using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
using MediaType = EcclesiaCast.Core.Media.MediaType;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// What the logo window's fields hold, as the operator left them. The window
/// reads it from its controls and hands it to
/// <see cref="LogoManagerViewModel.ApplyFields"/>, which checks it and writes
/// it into the logo.
/// </summary>
public sealed record LogoForm(
    string Name,
    int KindIndex,
    string Path,
    int ScalingIndex,
    bool Muted,
    string Text,
    string FontFamily,
    double FontSize,
    bool Bold,
    string TextColor,
    string BackgroundColor,
    double BackgroundBlur,
    double ZoomPercent,
    double OffsetXPercent,
    double OffsetYPercent,
    bool FixedSize,
    double FrameWidth,
    double FrameHeight,
    int? FillMediaId)
{
    /// <summary>What the fields show for a logo.</summary>
    public static LogoForm Of(Logo logo) => new(
        logo.Name,
        (int)logo.Kind,
        logo.Path ?? string.Empty,
        (int)logo.Scaling,
        logo.Muted,
        logo.Text,
        logo.FontFamily,
        logo.FontSize,
        logo.Bold,
        logo.TextColor,
        logo.BackgroundColor,
        logo.BackgroundBlur,
        Math.Round((logo.Zoom <= 0 ? 1 : logo.Zoom) * 100),
        Math.Round(logo.OffsetX * 1000) / 10,
        Math.Round(logo.OffsetY * 1000) / 10,
        logo.FrameWidth is > 0 && logo.FrameHeight is > 0,
        logo.FrameWidth ?? 1920,
        logo.FrameHeight ?? 1080,
        logo.FillMediaId);
}

/// <summary>
/// Backs the logo window: the church's logos, one per kind of meeting, each an
/// image, a looping video or plain text. Edits are written as they happen, so
/// the picker in the main window is always up to date: a change in the fields
/// is written 400 ms after the last one (dragging the logo changes them many
/// times a second), and at once when the selection moves or the window closes.
/// </summary>
public sealed class LogoManagerViewModel
{
    private readonly ILogoRepository _logos;
    private readonly IThumbnailer _thumbnails;
    private readonly IColorPicker _colorPicker;
    private readonly IUiTimer _saveTimer;
    private Logo? _pendingSave;

    public LogoManagerViewModel(
        ILogoRepository logos, IThumbnailer thumbnails, IColorPicker colorPicker,
        IFontCatalog fonts, IUiTimerFactory timers)
    {
        _logos = logos;
        _thumbnails = thumbnails;
        _colorPicker = colorPicker;
        FontNames = fonts.InstalledFamilies();
        _saveTimer = timers.Create(TimeSpan.FromMilliseconds(400));
        _saveTimer.Tick += (_, _) => FlushSave();
        Reload();
    }

    /// <summary>The installed fonts, for the font box.</summary>
    public IReadOnlyList<string> FontNames { get; }

    /// <summary>The logos, in the order of the list.</summary>
    public List<Logo> Items { get; } = [];

    /// <summary>True if anything was added, edited or removed.</summary>
    public bool ChangesMade { get; private set; }

    /// <summary>A field changed: the preview shows this copy of the logo at once (it is not written yet).</summary>
    public event Action<Logo>? PreviewChanged;

    /// <summary>The logo was written: the list redraws its name and icon, and the preview follows.</summary>
    public event Action<Logo>? Saved;

    /// <summary>Reads the logos again.</summary>
    public void Reload()
    {
        Items.Clear();
        Items.AddRange(_logos.GetAll());
    }

    /// <summary>The logo with this id, or the first one when there is none (null for an empty list).</summary>
    public Logo? Pick(int? id) => Items.FirstOrDefault(l => l.Id == id) ?? Items.FirstOrDefault();

    // ── Writing ──────────────────────────────────────────────────

    /// <summary>Shows the change in the preview now and writes it when the operator stops.</summary>
    public void SaveSoon(Logo logo)
    {
        _pendingSave = logo;
        PreviewChanged?.Invoke(logo.Clone());
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    /// <summary>Writes the change that was waiting for the operator to stop, if any.</summary>
    public void FlushSave()
    {
        _saveTimer.Stop();
        if (_pendingSave is { } logo)
        {
            _pendingSave = null;
            Save(logo);
        }
    }

    private void Save(Logo logo)
    {
        _logos.Save(logo);
        ChangesMade = true;
        Saved?.Invoke(logo);
    }

    /// <summary>
    /// Checks what the fields hold and writes it into the logo, then schedules the write.
    /// <paramref name="form"/> carries the border fill that was chosen: it is kept only if
    /// the kind of logo still offers it (a video has no blurred copy of itself).
    /// </summary>
    public void ApplyFields(Logo logo, LogoForm form)
    {
        logo.Name = string.IsNullOrWhiteSpace(form.Name) ? "Logo" : form.Name.Trim();
        logo.Kind = (LogoKind)Math.Clamp(form.KindIndex, 0, 2);
        logo.Scaling = (MediaScaling)Math.Max(0, form.ScalingIndex);
        logo.Muted = form.Muted;
        logo.Text = form.Text;
        logo.FontFamily = string.IsNullOrWhiteSpace(form.FontFamily) ? "Segoe UI" : form.FontFamily.Trim();
        logo.FontSize = form.FontSize;
        logo.Bold = form.Bold;
        logo.TextColor = form.TextColor.Trim();
        logo.BackgroundColor = form.BackgroundColor.Trim();
        logo.BackgroundBlur = form.BackgroundBlur;

        logo.Zoom = Math.Clamp(form.ZoomPercent / 100, 0.1, 4);
        logo.OffsetX = Math.Clamp(form.OffsetXPercent / 100, -FillFraming.MaxOffset, FillFraming.MaxOffset);
        logo.OffsetY = Math.Clamp(form.OffsetYPercent / 100, -FillFraming.MaxOffset, FillFraming.MaxOffset);
        logo.FrameWidth = form.FixedSize ? (int)Math.Clamp(form.FrameWidth, 16, 1920) : null;
        logo.FrameHeight = form.FixedSize ? (int)Math.Clamp(form.FrameHeight, 16, 1080) : null;

        var chosen = FillOptions(logo.Kind)
            .FirstOrDefault(m => m.Id == (form.FillMediaId ?? 0));
        logo.FillMediaId = chosen is { Id: not 0 } ? chosen.Id : null;

        SaveSoon(logo);
    }

    /// <summary>The width and height a size preset of the box stands for (index 0 and unknown ones: none).</summary>
    public static (double Width, double Height)? PresetSize(int presetIndex) => presetIndex switch
    {
        1 => (1920, 1080),
        2 => (1280, 720),
        3 => (1440, 1080),
        4 => (1080, 1080),
        5 => (1080, 1920),
        _ => null,
    };

    /// <summary>
    /// The choices for the borders: the colour alone, the logo itself blurred
    /// (for a video, a still of it: a moving copy would need a second player)
    /// or an image of the media library.
    /// </summary>
    public IReadOnlyList<MediaItem> FillOptions(LogoKind kind)
    {
        var options = new List<MediaItem> { new() { Id = 0, Name = "El color de fondo" } };
        if (kind == LogoKind.Image)
            options.Add(new MediaItem { Id = MediaItem.FillWithSelfId, Name = "La misma imagen, desenfocada" });
        else if (kind == LogoKind.Video)
            options.Add(new MediaItem { Id = MediaItem.FillWithSelfId, Name = "Una imagen fija del video, desenfocada" });
        options.AddRange(MediaLookup.All()
            .Where(m => m.Type == MediaType.Image)
            .OrderBy(m => m.Name));
        return options;
    }

    /// <summary>Whether the logo can be moved and zoomed in the preview (an image or a video).</summary>
    public static bool CanFrame(Logo? logo) => logo is { Kind: LogoKind.Image or LogoKind.Video };

    // ── File and colours ─────────────────────────────────────────

    /// <summary>
    /// The operator chose a file for the logo: it is written down, a video gets a
    /// still for the preview boxes (the projector shows the real thing), and a
    /// logo that still has its starting name takes the name of the file.
    /// </summary>
    public void SetFile(Logo logo, string fileName)
    {
        var isVideo = logo.Kind == LogoKind.Video;
        logo.Path = fileName;

        // A video logo needs a still for the operator's preview boxes, where
        // no video plays.
        _thumbnails.Delete(logo.PosterPath);
        logo.PosterPath = isVideo
            ? _thumbnails.Create(fileName, MediaType.Video, PlatformServices.Video?.Instance)
            : null;

        if (string.IsNullOrWhiteSpace(logo.Name) || logo.Name == "Logo nuevo")
            logo.Name = System.IO.Path.GetFileNameWithoutExtension(fileName);

        Save(logo);
    }

    /// <summary>Opens the colour picker on the colour shown; null when the operator cancelled.</summary>
    public string? PickColor(string current) => _colorPicker.Pick(current);

    // ── Add, copy, remove and order ──────────────────────────────

    /// <summary>A new image logo at the end of the list.</summary>
    public Logo CreateNew()
    {
        FlushSave();
        var created = _logos.Save(new Logo
        {
            Name = "Logo nuevo",
            Kind = LogoKind.Image,
            Order = Items.Count,
        });
        ChangesMade = true;
        Reload();
        return created;
    }

    /// <summary>A copy of the logo at the end of the list.</summary>
    public Logo Duplicate(Logo logo)
    {
        FlushSave();
        var copy = logo.Clone();
        copy.Id = 0;
        copy.Name = $"{logo.Name} (copia)";
        copy.Order = Items.Count;
        var created = _logos.Save(copy);
        ChangesMade = true;
        Reload();
        return created;
    }

    /// <summary>Asks, and removes the logo if the operator says yes. True when it was removed.</summary>
    public bool Delete(Logo logo)
    {
        FlushSave();
        var confirm = Notice.Confirm(
            $"¿Eliminar el logo \"{logo.Name}\"?",
            "Eliminar", destructive: true);
        if (!confirm)
            return false;

        _thumbnails.Delete(logo.PosterPath);
        _logos.Delete(logo.Id);
        ChangesMade = true;
        Reload();
        return true;
    }

    /// <summary>Moves the logo one place up (-1) or down (+1). False when it was already at the end.</summary>
    public bool Move(Logo logo, int direction)
    {
        FlushSave();
        var index = Items.IndexOf(logo);
        var target = index + direction;
        if (index < 0 || target < 0 || target >= Items.Count)
            return false;

        (Items[index], Items[target]) = (Items[target], Items[index]);
        for (var i = 0; i < Items.Count; i++)
        {
            Items[i].Order = i;
            _logos.Save(Items[i]);
        }

        ChangesMade = true;
        Reload();
        return true;
    }
}
