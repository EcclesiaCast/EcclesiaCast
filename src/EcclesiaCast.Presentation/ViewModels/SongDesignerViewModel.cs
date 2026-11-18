using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// What the format panel of the song designer holds, as the operator left it.
/// For one of the extra boxes only the first eight fields count (typeface,
/// size, bold, italic, colour and alignment); the rest belong to the song's words.
/// </summary>
public sealed record SongStyleForm(
    string FontText,
    double FontSize,
    bool Bold,
    bool Italic,
    string Color,
    HAlign AlignH,
    VAlign AlignV,
    bool Underline = false,
    bool Strikethrough = false,
    bool Shadow = false,
    double ShadowPercent = 0,
    double OutlineWidth = 0,
    string OutlineColor = "",
    int CaseIndex = 0,
    double LineSpacing = 1,
    bool FitToWidth = false);

/// <summary>What the format panel shows for the chosen box: the slide's own values, or the theme's where it has none.</summary>
public sealed record SongStylePanel(
    bool IsExtraBox,
    string FontFamily,
    double FontSize,
    bool Bold,
    bool Italic,
    string Color,
    HAlign AlignH,
    VAlign AlignV,
    bool Underline,
    bool Strikethrough,
    bool Shadow,
    double ShadowPercent,
    double OutlineWidth,
    string OutlineColor,
    int CaseIndex,
    double LineSpacing,
    bool FitToWidth);

/// <summary>How the text on the slide is written while the operator types on it (the sizes are those of the slide, not of the canvas).</summary>
public sealed record SongInlineEdit(
    string Text,
    string FontFamily,
    double FontSize,
    bool Bold,
    bool Italic,
    string Color,
    HAlign AlignH,
    VAlign AlignV,
    SlideContent PreviewMeanwhile);

/// <summary>A box other than the chosen one, drawn as a dotted outline with its name.</summary>
public sealed record SongGhostBox(string Label, double X, double Y, double Width, double Height);

/// <summary>
/// Backs the song designer: the slide list, the box on the canvas and the format
/// panel. It works on copies of the slides' texts and designs; the song is
/// written only by <see cref="Save"/>, so cancelling leaves it untouched.
/// Dragging, the handles, the zoom and the on-slide editor stay in the window,
/// which hands the box's final place to <see cref="SetBoxGeometry"/>.
/// </summary>
public sealed class SongDesignerViewModel
{
    /// <summary>The canvas of the window is half the size of the slide.</summary>
    public const double Scale = 0.5;

    /// <summary>Size of the canvas of the window.</summary>
    public const double CanvasW = 960;

    /// <summary>Size of the canvas of the window.</summary>
    public const double CanvasH = 540;

    private readonly Song _song;
    private readonly SlideTheme _theme;
    private readonly IColorPicker _colorPicker;
    private readonly List<SlideOverride?> _overrides;
    private readonly List<string> _texts;

    /// <summary>
    /// Slides the operator actually changed in this session. Only these get
    /// their format written out in full; the rest keep whatever they had.
    ///
    /// Selecting a slide is not editing it — opening the designer gives every
    /// slide a box so it can be dragged, and treating that as an edit would
    /// freeze the whole song against its theme.
    /// </summary>
    private readonly HashSet<int> _touched = [];

    public SongDesignerViewModel(Song song, SlideTheme theme, IColorPicker colorPicker, IFontCatalog fonts)
    {
        _song = song;
        _theme = theme;
        _colorPicker = colorPicker;
        FontNames = fonts.InstalledFamilies();
        _overrides = song.Sections.Select(s => s.GetOverride()).ToList();
        _texts = song.Sections.Select(s => s.Text).ToList();
    }

    /// <summary>The installed fonts, for the font box.</summary>
    public IReadOnlyList<string> FontNames { get; }

    /// <summary>True once the song was written.</summary>
    public bool Saved { get; private set; }

    /// <summary>
    /// What the box on the canvas is editing: -1 is the song's own words,
    /// 0 and up are the extra boxes of the selected slide. One box with a
    /// changing target, rather than a canvas full of boxes, keeps dragging,
    /// the handles and the arrow keys working exactly as they did.
    /// </summary>
    public int SelectedBox { get; set; } = -1;

    public int SlideCount => _song.Sections.Count;

    public string SlideLabel(int slide) => _song.Sections[slide].Label;

    /// <summary>The design of a slide (null: it follows the theme).</summary>
    public SlideOverride? OverrideOf(int slide) => _overrides[slide];

    /// <summary>The slide as the preview and the thumbnail draw it (no caption, so the box uses the whole canvas).</summary>
    public SlideContent BuildSlide(int slide) => new(_texts[slide], null, null, _theme, _overrides[slide]);

    private SlideOverride EnsureBox(SlideOverride? over)
    {
        if (over?.HasBox == true)
            return over;

        // Default box = the theme's margin area, made explicit for editing.
        var x = _theme.MarginHorizontal;
        var y = _theme.MarginVertical;
        var w = 1920 - 2 * _theme.MarginHorizontal;
        var h = 1080 - 2 * _theme.MarginVertical;
        return (over ?? new SlideOverride()) with { BoxX = x, BoxY = y, BoxWidth = w, BoxHeight = h };
    }

    // ── Slides and boxes ─────────────────────────────────────────

    /// <summary>
    /// A slide was chosen: changing slide always goes back to the words (the extra boxes
    /// of the slide you just left mean nothing on this one), and the slide gets a box
    /// so it can be dragged. That is not an edit.
    /// </summary>
    public SlideOverride SelectSlide(int slide)
    {
        SelectedBox = -1;
        _overrides[slide] = EnsureBox(_overrides[slide]);
        return _overrides[slide]!;
    }

    /// <summary>The names in the box picker: the song's words plus one entry per extra box.</summary>
    public IReadOnlyList<string> BoxNames(int slide)
    {
        var names = new List<string> { "Letra de la canción" };
        var count = slide >= 0 ? _overrides[slide]?.TextBoxes.Count ?? 0 : 0;
        for (var i = 0; i < count; i++)
            names.Add($"Cuadro {i + 1}");
        return names;
    }

    /// <summary>Remove and order buttons only mean something on an extra box, and the order ones need a neighbour.</summary>
    public (bool Remove, bool BringForward, bool SendBack) BoxButtons(int extraCount) =>
        (SelectedBox >= 0, SelectedBox >= 0 && SelectedBox < extraCount - 1, SelectedBox > 0);

    /// <summary>The box a Tab press goes to: the words first, then each extra box; Shift goes backwards.</summary>
    public int NextBox(int slide, bool backwards)
    {
        var count = (_overrides[slide]?.TextBoxes.Count ?? 0) + 1;
        var position = SelectedBox + 1;
        return (position + (backwards ? count - 1 : 1)) % count - 1;
    }

    /// <summary>Adds a box to the slide, away from the ones already there, and selects it.</summary>
    public void AddTextBox(int slide)
    {
        var over = _overrides[slide]!;
        var boxes = over.TextBoxes.ToList();
        // Not on top of the ones already there: two boxes on the same spot
        // cannot be told apart with the mouse.
        boxes.Add(SlideTextBox.FreshAvoiding(boxes));

        _touched.Add(slide);
        _overrides[slide] = over with { Boxes = boxes };
        SelectedBox = boxes.Count - 1;
    }

    /// <summary>Removes the chosen extra box and goes back to the words. False when there was none.</summary>
    public bool RemoveTextBox(int slide)
    {
        var over = _overrides[slide]!;
        var boxes = over.TextBoxes.ToList();
        if (SelectedBox < 0 || SelectedBox >= boxes.Count)
            return false;

        boxes.RemoveAt(SelectedBox);
        _touched.Add(slide);
        _overrides[slide] = over with { Boxes = boxes };
        SelectedBox = -1;
        return true;
    }

    /// <summary>Moves the chosen extra box one place in the drawing order: later boxes are drawn over earlier ones.</summary>
    public bool MoveBoxOrder(int slide, int direction)
    {
        var over = _overrides[slide]!;
        var boxes = over.TextBoxes.ToList();
        var target = SelectedBox + direction;
        if (SelectedBox < 0 || SelectedBox >= boxes.Count || target < 0 || target >= boxes.Count)
            return false;

        (boxes[SelectedBox], boxes[target]) = (boxes[target], boxes[SelectedBox]);
        _touched.Add(slide);
        _overrides[slide] = over with { Boxes = boxes };
        SelectedBox = target;
        return true;
    }

    /// <summary>Rewrites one extra box of an override, leaving the rest alone.</summary>
    private static SlideOverride ReplaceBox(SlideOverride over, int index, Func<SlideTextBox, SlideTextBox> change)
    {
        var boxes = over.TextBoxes.ToList();
        if (index < 0 || index >= boxes.Count)
            return over;

        boxes[index] = change(boxes[index]);
        return over with { Boxes = boxes };
    }

    // ── Format panel ─────────────────────────────────────────────

    /// <summary>What the panel shows for the chosen box of this slide.</summary>
    public SongStylePanel Panel(SlideOverride over)
    {
        // Editing one of the extra boxes: the panel shows that box's own few
        // settings instead of the song's.
        if (SelectedBox >= 0 && SelectedBox < over.TextBoxes.Count)
        {
            var extra = over.TextBoxes[SelectedBox];
            return new SongStylePanel(
                IsExtraBox: true,
                extra.FontFamily ?? _theme.FontFamily, extra.FontSize, extra.Bold, extra.Italic, extra.Color,
                extra.AlignH, extra.AlignV,
                Underline: false, Strikethrough: false, Shadow: false, ShadowPercent: 0, OutlineWidth: 0,
                OutlineColor: string.Empty, CaseIndex: 0, LineSpacing: 1, FitToWidth: false);
        }

        return new SongStylePanel(
            IsExtraBox: false,
            over.FontFamily ?? _theme.FontFamily,
            over.FontSize ?? _theme.MaxFontSize,
            over.Bold ?? _theme.Bold,
            over.Italic ?? _theme.Italic,
            over.TextColor ?? _theme.TextColor,
            over.AlignH ?? _theme.AlignH,
            over.AlignV ?? _theme.AlignV,
            over.Underline ?? false,
            over.Strikethrough ?? false,
            over.Shadow ?? _theme.Shadow,
            (over.ShadowOpacity ?? _theme.ShadowOpacity) * 100,
            over.OutlineWidth ?? _theme.OutlineWidth,
            over.OutlineColor ?? _theme.OutlineColor,
            (int)(over.Case ?? _theme.TextCase),
            over.LineSpacing is > 0 ? over.LineSpacing.Value : 1,
            over.FitToWidth ?? _theme.FitToWidth);
    }

    /// <summary>Writes what the panel holds into the chosen box of the slide, and marks the slide as edited.</summary>
    public void ApplyStyle(int slide, SongStyleForm form)
    {
        _touched.Add(slide);

        // An extra box carries only the handful of settings that make sense on
        // its own; the rest of the panel belongs to the song's words.
        if (SelectedBox >= 0)
        {
            _overrides[slide] = ReplaceBox(_overrides[slide]!, SelectedBox, b => b with
            {
                FontFamily = string.IsNullOrWhiteSpace(form.FontText) ? null : form.FontText.Trim(),
                FontSize = Math.Round(form.FontSize),
                Bold = form.Bold,
                Italic = form.Italic,
                Color = form.Color.Trim(),
                AlignH = form.AlignH,
                AlignV = form.AlignV,
            });
            return;
        }

        _overrides[slide] = _overrides[slide]! with
        {
            FontFamily = string.IsNullOrWhiteSpace(form.FontText) ? _theme.FontFamily : form.FontText.Trim(),
            FontSize = Math.Round(form.FontSize),
            Bold = form.Bold,
            Italic = form.Italic,
            Underline = form.Underline,
            Strikethrough = form.Strikethrough,
            Shadow = form.Shadow,
            ShadowOpacity = form.ShadowPercent / 100,
            OutlineWidth = form.OutlineWidth,
            OutlineColor = form.OutlineColor.Trim(),
            TextColor = form.Color.Trim(),
            AlignH = form.AlignH,
            AlignV = form.AlignV,
            Case = (TextCase)Math.Clamp(form.CaseIndex, 0, 4),
            LineSpacing = form.LineSpacing,
            FitToWidth = form.FitToWidth,
        };
    }

    /// <summary>
    /// A typeface written in the box but not confirmed would be lost, or worse, land on the
    /// next box. True when it differs from the one the chosen box has, so applying it is an
    /// edit; just looking at a slide never marks it as edited.
    /// </summary>
    public bool FontDiffers(int slide, string typed)
    {
        if (slide < 0 || string.IsNullOrWhiteSpace(typed))
            return false;

        var over = _overrides[slide]!;
        var current = SelectedBox >= 0 && SelectedBox < over.TextBoxes.Count
            ? over.TextBoxes[SelectedBox].FontFamily ?? _theme.FontFamily
            : over.FontFamily ?? _theme.FontFamily;

        return !string.Equals(typed.Trim(), current, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Opens the colour picker on the colour shown; null when the operator cancelled.</summary>
    public string? PickColor(string current) => _colorPicker.Pick(current.Trim());

    // ── Writing on the slide ─────────────────────────────────────

    /// <summary>What the on-slide editor starts with, and what the preview behind shows meanwhile (the box being written is blanked).</summary>
    public SongInlineEdit BeginEdit(int slide)
    {
        var over = _overrides[slide]!;

        // An extra box edits its own words, on the spot, with its own look.
        if (SelectedBox >= 0 && SelectedBox < over.TextBoxes.Count)
        {
            var extra = over.TextBoxes[SelectedBox];
            return new SongInlineEdit(
                extra.Text, extra.FontFamily ?? _theme.FontFamily, extra.FontSize, extra.Bold, extra.Italic,
                extra.Color, extra.AlignH, extra.AlignV,
                new SlideContent(_texts[slide], null, null, _theme,
                    ReplaceBox(over, SelectedBox, b => b with { Text = string.Empty })));
        }

        return new SongInlineEdit(
            _texts[slide], over.FontFamily ?? _theme.FontFamily, over.FontSize ?? _theme.MaxFontSize,
            over.Bold ?? _theme.Bold, over.Italic ?? _theme.Italic, over.TextColor ?? _theme.TextColor,
            over.AlignH ?? _theme.AlignH, over.AlignV ?? _theme.AlignV,
            // Hide the rendered text behind so only the editable copy shows.
            new SlideContent(string.Empty, null, null, _theme, over));
    }

    /// <summary>The operator finished writing on the slide: the words (or the extra box) take the text. False when the slide is not there.</summary>
    public bool CommitEdit(int slide, string text)
    {
        if (slide < 0 || slide >= _texts.Count)
            return false;

        if (SelectedBox >= 0)
        {
            _touched.Add(slide);
            _overrides[slide] = ReplaceBox(_overrides[slide]!, SelectedBox, b => b with { Text = text });
        }
        else
        {
            _texts[slide] = text;
        }

        return true;
    }

    // ── Where the box is ─────────────────────────────────────────

    /// <summary>Where the chosen box is, in pixels of the slide.</summary>
    public (double X, double Y, double Width, double Height) BoxRect(SlideOverride over) =>
        SelectedBox < 0 || SelectedBox >= over.TextBoxes.Count
            ? (over.BoxX ?? 0, over.BoxY ?? 0, over.BoxWidth ?? 1920, over.BoxHeight ?? 1080)
            : (over.TextBoxes[SelectedBox].X, over.TextBoxes[SelectedBox].Y,
               over.TextBoxes[SelectedBox].Width, over.TextBoxes[SelectedBox].Height);

    /// <summary>
    /// Every box that is not the chosen one, for the dotted outlines. With several boxes on a
    /// slide the one on top used to hide where the others were.
    /// </summary>
    public IReadOnlyList<SongGhostBox> Ghosts(int slide)
    {
        var ghosts = new List<SongGhostBox>();
        if (slide < 0 || _overrides[slide] is not { } over)
            return ghosts;

        if (over.HasBox && SelectedBox != -1)
            ghosts.Add(new SongGhostBox("Letra", over.BoxX ?? 0, over.BoxY ?? 0, over.BoxWidth ?? 1920, over.BoxHeight ?? 1080));

        for (var i = 0; i < over.TextBoxes.Count; i++)
        {
            if (i == SelectedBox)
                continue;

            var b = over.TextBoxes[i];
            ghosts.Add(new SongGhostBox($"Cuadro {i + 1}", b.X, b.Y, b.Width, b.Height));
        }

        return ghosts;
    }

    /// <summary>
    /// Which box lies under a point of the window's canvas: -1 is the song's words, 0
    /// and up the extra boxes, null is none. The smallest wins, so a small box
    /// sitting inside the big one of the words can still be reached.
    /// </summary>
    public int? BoxAt(int slide, double pointX, double pointY)
    {
        if (slide < 0 || _overrides[slide] is not { } over)
            return null;

        int? best = null;
        var bestArea = double.MaxValue;

        void Consider(int index, double x, double y, double w, double h)
        {
            var area = w * h;
            if (pointX >= x * Scale && pointX <= (x + w) * Scale && pointY >= y * Scale && pointY <= (y + h) * Scale
                && area <= bestArea)
            {
                best = index;
                bestArea = area;
            }
        }

        if (over.HasBox)
            Consider(-1, over.BoxX ?? 0, over.BoxY ?? 0, over.BoxWidth ?? 1920, over.BoxHeight ?? 1080);

        // Later boxes are drawn over earlier ones, so on a tie they win.
        for (var i = 0; i < over.TextBoxes.Count; i++)
        {
            var b = over.TextBoxes[i];
            Consider(i, b.X, b.Y, b.Width, b.Height);
        }

        return best;
    }

    /// <summary>The box was moved or stretched: its place, in pixels of the slide, is written and the slide counts as edited.</summary>
    public void SetBoxGeometry(int slide, double x, double y, double width, double height)
    {
        _touched.Add(slide);

        var over = _overrides[slide]!;
        _overrides[slide] = SelectedBox < 0
            ? over with { BoxX = x, BoxY = y, BoxWidth = width, BoxHeight = height }
            : ReplaceBox(over, SelectedBox, b => b with { X = x, Y = y, Width = width, Height = height });
    }

    /// <summary>The numbers the box fields show for a box on the canvas (left, top, width, height in canvas pixels).</summary>
    public static (double X, double Y, double Width, double Height, double Percent) BoxInfo(
        double left, double top, double width, double height)
    {
        var x = left / Scale;
        var y = top / Scale;
        var w = width / Scale;
        var h = height / Scale;
        return (Math.Round(x), Math.Round(y), Math.Round(w), Math.Round(h), Math.Round(w / CanvasW * Scale * 100));
    }

    /// <summary>
    /// The box resized to a percentage of the screen, keeping its current
    /// centre so stepping 80 → 81 → 82 % grows it in place instead of
    /// snapping back to the middle of the slide. In canvas pixels.
    /// </summary>
    public static (double Left, double Top, double Width, double Height) ResizeToPercent(
        double left, double top, double width, double height, double percentField)
    {
        var percent = Math.Clamp(percentField / 100, 0.1, 1);
        var w = CanvasW * percent;
        var h = CanvasH * percent;
        var centerX = left + width / 2;
        var centerY = top + height / 2;

        return (Math.Clamp(centerX - w / 2, 0, CanvasW - w), Math.Clamp(centerY - h / 2, 0, CanvasH - h), w, h);
    }

    /// <summary>The box for the X / Y / width / height typed (in pixels of the slide), in canvas pixels, kept inside the canvas.</summary>
    public static (double Left, double Top, double Width, double Height) FromTypedFields(
        double x, double y, double width, double height)
    {
        var w = Math.Clamp(width * Scale, 20, CanvasW);
        var h = Math.Clamp(height * Scale, 20, CanvasH);
        return (Math.Clamp(x * Scale, 0, CanvasW - w), Math.Clamp(y * Scale, 0, CanvasH - h), w, h);
    }

    /// <summary>
    /// Where a box goes for one of the alignment buttons («HL», «HC», «HR», «VT», «VC», «VB»,
    /// «CC»); an unknown name leaves it where it is. In canvas pixels.
    /// </summary>
    public static (double Left, double Top) AlignBox(string? tag, double left, double top, double width, double height)
    {
        double x = left, y = top;
        switch (tag)
        {
            case "HL": x = 0; break;
            case "HC": x = (CanvasW - width) / 2; break;
            case "HR": x = CanvasW - width; break;
            case "VT": y = 0; break;
            case "VC": y = (CanvasH - height) / 2; break;
            case "VB": y = CanvasH - height; break;
            case "CC": x = (CanvasW - width) / 2; y = (CanvasH - height) / 2; break;
        }

        return (x, y);
    }

    // ── Actions ──────────────────────────────────────────────────

    /// <summary>
    /// Gives every slide the design of this one. The extra boxes are words belonging to
    /// one slide — copying them everywhere would stamp "Serie: …" across the whole song —
    /// so each slide keeps its own. A slide under the theme has no format to hand out;
    /// the button then means "put them all back under the theme".
    /// </summary>
    public void ApplyToAll(int slide)
    {
        var source = _overrides[slide];
        for (var i = 0; i < _overrides.Count; i++)
        {
            _overrides[i] = source is null ? null : source with { Boxes = _overrides[i]?.Boxes };
            _touched.Add(i);
        }
    }

    /// <summary>Puts the slide back under the theme's control, dropping its own design.</summary>
    public SlideOverride ResetSlide(int slide)
    {
        _overrides[slide] = EnsureBox(null);
        _touched.Remove(slide);
        return _overrides[slide]!;
    }

    /// <summary>Writes the texts and the designs of the slides the operator worked on into the song.</summary>
    public void Save()
    {
        for (var i = 0; i < _song.Sections.Count; i++)
        {
            var text = _texts[i].Trim();
            if (text.Length > 0)
                _song.Sections[i].Text = text;

            // Slides the operator worked on keep exactly what the panel shows;
            // the rest are left alone so they keep following the theme.
            if (_touched.Contains(i))
                _song.Sections[i].SetOverride(Clean(_overrides[i]));
        }

        Saved = true;
    }

    /// <summary>
    /// Tidies an edited slide's design without second-guessing it. It used to
    /// null out every field matching the theme, which quietly threw away
    /// deliberate choices that happened to agree with it — pick the theme's own
    /// typeface in the designer and the slide was left with none, so changing
    /// the theme later moved a song that had been designed by hand.
    ///
    /// Only decorations that are off are dropped, since false and "not set"
    /// mean the same thing for them.
    /// </summary>
    private static SlideOverride? Clean(SlideOverride? over)
    {
        if (over is null)
            return null;

        var result = over with
        {
            Underline = over.Underline == true ? true : null,
            Strikethrough = over.Strikethrough == true ? true : null,
        };

        return result.IsEmpty ? null : result;
    }
}
