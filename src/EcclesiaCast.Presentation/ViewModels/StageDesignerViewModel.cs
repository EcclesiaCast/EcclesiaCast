using EcclesiaCast.Core.Stage;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// Backs the stage designer: the saved arrangements of the stage screen
/// (singers, preacher...) and the boxes of each, placed over a 1920×1080
/// canvas. It works on copies; nothing reaches the stage until the window
/// hands <see cref="Build"/> back on «Guardar». Moving a box with the mouse
/// stays in the window, which tells this class where the box ended up.
/// </summary>
public sealed class StageDesignerViewModel
{
    private const double CanvasW = StageLayout.CanvasWidth;
    private const double CanvasH = StageLayout.CanvasHeight;

    private readonly Func<string, string, string, string?> _ask;
    private readonly IColorPicker _colorPicker;
    private readonly List<StageLayout> _layouts;

    /// <param name="ask">Opens the text prompt over the window (title, message, initial text); null when cancelled.</param>
    public StageDesignerViewModel(
        IReadOnlyList<StageLayout> layouts, string activeName,
        Func<string, string, string, string?> ask, IColorPicker colorPicker, IFontCatalog fonts)
    {
        _ask = ask;
        _colorPicker = colorPicker;
        FontNames = fonts.InstalledFamilies();

        _layouts = [.. layouts.Select(l => l.Clone())];
        if (_layouts.Count == 0)
            _layouts.Add(new StageLayout { Name = StageLayoutStore.SingersName });

        InUse = _layouts.FirstOrDefault(l => string.Equals(l.Name, activeName, StringComparison.OrdinalIgnoreCase))
                ?? _layouts[0];
        Current = InUse;
    }

    /// <summary>The installed fonts, for the font box.</summary>
    public IReadOnlyList<string> FontNames { get; }

    /// <summary>The arrangements, in the order of the picker.</summary>
    public IReadOnlyList<StageLayout> Layouts => _layouts;

    /// <summary>The arrangement the stage screen uses.</summary>
    public StageLayout InUse { get; private set; }

    /// <summary>The arrangement that is open.</summary>
    public StageLayout Current { get; private set; }

    /// <summary>The box chosen in the open arrangement, if any.</summary>
    public StageBox? Selected { get; set; }

    /// <summary>The line of the picker for an arrangement.</summary>
    public string LayoutText(StageLayout layout) =>
        ReferenceEquals(layout, InUse) ? $"{layout.Name}  ·  en uso" : layout.Name;

    /// <summary>What the window says about the arrangement the stage uses.</summary>
    public string InUseText =>
        $"En la pantalla de escenario se usa: «{InUse.Name}». Se cambia también desde el ▾ junto al botón Escenario.";

    /// <summary>Whether «Usar este» does anything: the open arrangement is not the one in use.</summary>
    public bool CanUseCurrent => !ReferenceEquals(Current, InUse);

    /// <summary>The arrangements as they are now and the one in use: what «Guardar» hands back.</summary>
    public StageLayoutState Build() => new(_layouts, InUse.Name);

    // ── Arrangements ─────────────────────────────────────────────

    /// <summary>Opens another arrangement.</summary>
    public void OpenLayout(StageLayout layout) => Current = layout;

    /// <summary>Asks for a name and adds a new arrangement with one live-text box. False when cancelled.</summary>
    public bool NewLayout()
    {
        if (_ask("Nuevo escenario", "Nombre del escenario", "Escenario") is not { Length: > 0 } name)
            return false;

        var layout = new StageLayout
        {
            Name = StageLayoutStore.UniqueName(_layouts.Select(l => l.Name), name),
            Boxes =
            [
                NewBox(StageBoxKind.LiveText),
            ],
        };
        _layouts.Add(layout);
        Current = layout;
        return true;
    }

    /// <summary>Adds a copy of the open arrangement, with its boxes under new ids, and opens it.</summary>
    public void DuplicateLayout()
    {
        var copy = Current.Clone();
        copy.Name = StageLayoutStore.UniqueName(_layouts.Select(l => l.Name), $"{Current.Name} copia");
        foreach (var box in copy.Boxes)
            box.Id = NewBoxId();
        _layouts.Add(copy);
        Current = copy;
    }

    /// <summary>Asks for a new name for the open arrangement. False when cancelled.</summary>
    public bool RenameLayout()
    {
        if (_ask("Cambiar el nombre", "Nombre del escenario", Current.Name) is not { Length: > 0 } name)
            return false;

        Current.Name = StageLayoutStore.UniqueName(
            _layouts.Where(l => !ReferenceEquals(l, Current)).Select(l => l.Name), name);
        return true;
    }

    /// <summary>
    /// Removes the open arrangement after asking. The last one cannot be removed. When the
    /// one in use goes, the one that takes its place is used. True when it was removed.
    /// </summary>
    public bool DeleteLayout()
    {
        if (_layouts.Count <= 1)
        {
            Notice.Show("Tiene que quedar al menos un escenario.");
            return false;
        }

        if (!Notice.Confirm($"¿Eliminar el escenario «{Current.Name}»?", "Eliminar", destructive: true))
            return false;

        var index = _layouts.IndexOf(Current);
        _layouts.Remove(Current);
        if (ReferenceEquals(Current, InUse))
            InUse = _layouts[Math.Min(index, _layouts.Count - 1)];
        Current = _layouts[Math.Min(index, _layouts.Count - 1)];
        return true;
    }

    /// <summary>The stage screen uses the open arrangement.</summary>
    public void UseCurrent() => InUse = Current;

    // ── Boxes ────────────────────────────────────────────────────

    /// <summary>The line of the box list: the kind, and the start of the text of a fixed text.</summary>
    public static string BoxTitle(StageBox box)
    {
        var name = StageBox.KindName(box.Kind);
        if (box.Kind != StageBoxKind.FixedText || string.IsNullOrWhiteSpace(box.Text))
            return name;

        var oneLine = box.Text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return $"{name}: {(oneLine.Length > 22 ? oneLine[..22] + "…" : oneLine)}";
    }

    /// <summary>A box of this kind in the middle of the canvas, with a size that reads well; the operator moves and stretches it afterwards.</summary>
    public static StageBox NewBox(StageBoxKind kind)
    {
        var (w, h, size) = kind switch
        {
            StageBoxKind.LiveText => (1600d, 560d, 110d),
            StageBoxKind.NextSlide => (1600d, 200d, 48d),
            StageBoxKind.Clock or StageBoxKind.Timer or StageBoxKind.Countdown or StageBoxKind.SectionTimer => (460d, 150d, 120d),
            StageBoxKind.Notes => (1600d, 600d, 60d),
            StageBoxKind.Mirror => (960d, 540d, 60d),
            StageBoxKind.Overlay => (1600d, 70d, 36d),
            _ => (900d, 100d, 48d),
        };

        return new StageBox
        {
            Kind = kind,
            Width = w,
            Height = h,
            X = (CanvasW - w) / 2,
            Y = (CanvasH - h) / 2,
            FontSize = size,
            Align = kind is StageBoxKind.Notes ? StageAlign.Left : StageAlign.Center,
            Bold = kind is StageBoxKind.LiveText,
            Text = kind == StageBoxKind.FixedText ? "Texto" : string.Empty,
        };
    }

    /// <summary>Puts the box in the open arrangement and selects it.</summary>
    public void AddBox(StageBox box)
    {
        Current.Boxes.Add(box);
        Selected = box;
    }

    /// <summary>Adds a copy of the selected box, a little down and to the right. Null when none is selected.</summary>
    public StageBox? DuplicateSelected()
    {
        if (Selected is not { } box)
            return null;

        var copy = box.Clone();
        copy.Id = NewBoxId();
        copy.X = Math.Min(box.X + 40, CanvasW - copy.Width);
        copy.Y = Math.Min(box.Y + 40, CanvasH - copy.Height);
        AddBox(copy);
        return copy;
    }

    /// <summary>
    /// Removes the selected box. Returns it (so the window drops its drawing), or null when
    /// none was selected; <see cref="Selected"/> is left on the box that took its place.
    /// </summary>
    public StageBox? DeleteSelected()
    {
        if (Selected is not { } box)
            return null;

        var index = Current.Boxes.IndexOf(box);
        Current.Boxes.Remove(box);
        Selected = Current.Boxes.Count == 0 ? null : Current.Boxes[Math.Min(index, Current.Boxes.Count - 1)];
        return box;
    }

    private static string NewBoxId() => Guid.NewGuid().ToString("N")[..8];

    // ── Properties of the selected box ───────────────────────────

    public void SetKind(StageBoxKind kind)
    {
        if (Selected is { } box)
            box.Kind = kind;
    }

    public void SetText(string text)
    {
        if (Selected is { } box)
            box.Text = text;
    }

    /// <summary>The font typed in the font box; empty means the default one.</summary>
    public void SetFont(string typed)
    {
        if (Selected is not { } box)
            return;

        typed = typed.Trim();
        box.FontFamily = string.IsNullOrWhiteSpace(typed) ? null : typed;
    }

    public void SetFontSize(double size)
    {
        if (Selected is { } box)
            box.FontSize = Math.Clamp(size, 8, 600);
    }

    /// <summary>Opens the colour picker on the colour of the selected box and keeps the one chosen. Null when cancelled.</summary>
    public string? PickColor()
    {
        if (Selected is not { } box)
            return null;

        if (_colorPicker.Pick(box.Color) is not { } hex)
            return null;

        box.Color = hex;
        return hex;
    }

    /// <summary>Sets the alignment from its name («Left», «Center», «Right»).</summary>
    public void SetAlign(string name)
    {
        if (Selected is { } box)
            box.Align = Enum.Parse<StageAlign>(name);
    }

    public void SetBold(bool bold)
    {
        if (Selected is { } box)
            box.Bold = bold;
    }

    /// <summary>The numbers typed in the position and size fields, kept inside the canvas.</summary>
    public void SetGeometry(double x, double y, double width, double height)
    {
        if (Selected is not { } box)
            return;

        var w = Math.Clamp(width, 20, CanvasW);
        var h = Math.Clamp(height, 20, CanvasH);
        box.Width = w;
        box.Height = h;
        box.X = Math.Clamp(x, 0, CanvasW - w);
        box.Y = Math.Clamp(y, 0, CanvasH - h);
    }

    /// <summary>The box is dragged to this corner: it is rounded and kept inside the canvas.</summary>
    public void MoveTo(double x, double y)
    {
        if (Selected is not { } box)
            return;

        box.X = Math.Round(Math.Clamp(x, 0, CanvasW - box.Width));
        box.Y = Math.Round(Math.Clamp(y, 0, CanvasH - box.Height));
    }

    /// <summary>An arrow key: moves the box <paramref name="step"/> pixels in the direction (-1, 0 or 1 on each axis).</summary>
    public void Nudge(int dx, int dy, int step)
    {
        if (Selected is not { } box)
            return;

        if (dx < 0)
            box.X = Math.Max(0, box.X - step);
        else if (dx > 0)
            box.X = Math.Min(CanvasW - box.Width, box.X + step);

        if (dy < 0)
            box.Y = Math.Max(0, box.Y - step);
        else if (dy > 0)
            box.Y = Math.Min(CanvasH - box.Height, box.Y + step);
    }

    /// <summary>A handle («NW», «N», «NE», «W», «E», «SW», «S», «SE») is dragged by this much.</summary>
    public void Resize(string handle, double horizontalChange, double verticalChange)
    {
        if (Selected is not { } box)
            return;

        var (x, y, w, h) = BoxGeometry.Resize(
            handle, box.X, box.Y, box.Width, box.Height,
            horizontalChange, verticalChange, CanvasW, CanvasH);

        box.X = Math.Round(x);
        box.Y = Math.Round(y);
        box.Width = Math.Round(w);
        box.Height = Math.Round(h);
    }

    /// <summary>The topmost box under this point of the canvas (later boxes are drawn on top), or null.</summary>
    public StageBox? BoxAt(double x, double y) =>
        Current.Boxes.LastOrDefault(b =>
            x >= b.X && x <= b.X + b.Width && y >= b.Y && y <= b.Y + b.Height);

    /// <summary>What a kind of box shows, for the hint under the kind box.</summary>
    public static string KindHint(StageBoxKind kind) => kind switch
    {
        StageBoxKind.LiveText => "Las palabras que están en pantalla ahora. Durante una cuenta regresiva muestra la cuenta.",
        StageBoxKind.NextSlide => "La diapositiva que sigue, para que los cantantes se preparen.",
        StageBoxKind.Clock => "La hora de la computadora.",
        StageBoxKind.Timer => "Cuánto lleva el servicio. Se maneja desde la pestaña Tiempos o el menú Proyección.",
        StageBoxKind.Countdown => "Lo que falta de la cuenta regresiva que se proyecta antes de empezar. Queda vacío si no hay ninguna corriendo.",
        StageBoxKind.SectionTimer => "Lo que le queda a la sección en marcha (pestaña Tiempos). Pasado el tiempo sigue contando en negativo y se pone en rojo.",
        StageBoxKind.Notes => "Las notas que escribís en la pestaña Notas. La congregación nunca las ve.",
        StageBoxKind.LiveName => "El nombre de lo que está en vivo: «Coro», «Juan 3:16».",
        StageBoxKind.FixedText => "Un texto que no cambia: un título o un recordatorio.",
        StageBoxKind.Overlay => "El aviso al pie, el mismo que ve la congregación. Sólo aparece mientras hay uno.",
        StageBoxKind.Mirror => "Exactamente lo que se proyecta, con su fondo. El tamaño de letra no cuenta acá.",
        _ => string.Empty,
    };
}
