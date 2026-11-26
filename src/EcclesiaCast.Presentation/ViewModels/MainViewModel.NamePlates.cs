using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// The «Placas» tab (GitHub #37): name plates, the «lower thirds» that say who
/// is speaking or singing. Each plate shown is kept in a list, so the next
/// Sunday the pastor's or the worship leader's is one double click away.
/// </summary>
public sealed partial class MainViewModel
{
    private const string NamePlateAutoHideKey = "nameplates.autohide";

    /// <summary>Seconds after which a plate goes by itself, by the index of the list in the tab (0: it stays).</summary>
    private static readonly int[] NamePlateAutoHideSeconds = [0, 5, 10, 15];

    /// <summary>The plates the church keeps ready, the last one used first.</summary>
    public ObservableCollection<NamePlate> SavedNamePlates { get; } = [];

    /// <summary>First line of the plate: the name.</summary>
    [ObservableProperty]
    private string _namePlateName = string.Empty;

    /// <summary>Second line, optional: the role or what they do («Predicación», «Alabanza»).</summary>
    [ObservableProperty]
    private string _namePlateRole = string.Empty;

    [ObservableProperty]
    private NamePlate? _selectedSavedNamePlate;

    /// <summary>The picture or short video beside the words; null for words only.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NamePlateImageLabel), nameof(NamePlateHasImage))]
    private string? _namePlateImagePath;

    public bool NamePlateHasImage => !string.IsNullOrWhiteSpace(NamePlateImagePath);

    /// <summary>«logo.png», «🎞 intro.mp4» or «Sin imagen», beside the «Elegir…» button.</summary>
    public string NamePlateImageLabel => NamePlateImagePath is { Length: > 0 } path
        ? GifImporter.IsConverted(path) ? "🎞 GIF animado"
        : (NamePlateMedia.IsVideo(path) ? "🎞 " : "🖼 ") + System.IO.Path.GetFileName(path)
        : "Sin imagen";

    /// <summary>0: the picture on the left of the words; 1: on the right.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NamePlateImageSideLabel))]
    private int _namePlateImageSide;

    /// <summary>The small button beside «Elegir…»: which side the picture goes, one click switches it.</summary>
    public string NamePlateImageSideLabel => NamePlateImageSide == 1 ? "◨" : "◧";

    [RelayCommand]
    private void ToggleNamePlateImageSide() => NamePlateImageSide = NamePlateImageSide == 1 ? 0 : 1;

    /// <summary>The names of the church's styles, for the list in the tab.</summary>
    public ObservableCollection<string> NamePlateStyleNames { get; } = [];

    /// <summary>The style of the plate being written (its look, its movement, where it is seen).</summary>
    [ObservableProperty]
    private string _namePlateStyleName = NamePlateStyle.Classic.Name;

    private void LoadNamePlateStyleNames()
    {
        var keep = NamePlateStyleName;
        NamePlateStyleNames.Clear();
        foreach (var style in NamePlateStyleSettings.Current)
            NamePlateStyleNames.Add(style.Name);
        NamePlateStyleName = NamePlateStyleSettings.Find(keep).Name;
    }

    private void FillNamePlateFields(NamePlate plate)
    {
        NamePlateName = plate.Name;
        NamePlateRole = plate.Role;
        NamePlateImagePath = plate.ImagePath;
        NamePlateImageSide = plate.ImageRight ? 1 : 0;
        NamePlateStyleName = NamePlateStyleSettings.Find(plate.StyleName).Name;
    }

    /// <summary>
    /// «✏ Diseñar…»: the window with every option of the styles. It opens on
    /// the style chosen in the tab, and leaves chosen the one closed on.
    /// </summary>
    [RelayCommand]
    private void DesignNamePlateStyles()
    {
        var sample = NamePlate.Clean(NamePlateName, NamePlateRole, NamePlateImagePath, NamePlateImageSide == 1, NamePlateStyleName)
            ?? new NamePlate("Juan Pérez", "Predicación", NamePlateImagePath, NamePlateImageSide == 1, NamePlateStyleName);
        if (_namePlateDesigner.Design(sample) is not { } design)
            return;

        NamePlateStyleSettings.Set(design.Styles);
        NamePlateStyleName = design.ChosenStyle;

        // A plate on the screen takes its style's new look at once.
        if (_presentation.NamePlate is { } shown)
            _presentation.ShowNamePlate(shown with { Style = NamePlateStyleSettings.Find(shown.StyleName) });
        StatusText = $"Estilos de placa guardados. Estilo elegido: «{NamePlateStyleName}».";
    }

    /// <summary>The logos of the library that are a picture or a video: the first choices for a plate.</summary>
    public IReadOnlyList<(string Name, string Path)> NamePlateLogoChoices() =>
        _logos.GetAll()
            .Where(l => !string.IsNullOrWhiteSpace(l.Path) && NamePlateMedia.CanCarry(l.Path) && System.IO.File.Exists(l.Path))
            .Select(l => (l.Name, l.Path!))
            .ToList();

    /// <summary>A logo or a file chosen for the plate (from the «Elegir…» menu).</summary>
    public void UseNamePlateImage(string? path)
    {
        if (path is not null && !NamePlateMedia.CanCarry(path))
        {
            StatusText = "La placa puede llevar una imagen (PNG, JPG, GIF…) o un video corto (MP4, WEBM…).";
            return;
        }

        // An animated GIF would stand still as a picture: it is made into a
        // looping video first (a few seconds), as the library does with GIFs.
        if (path is not null
            && System.IO.Path.GetExtension(path).Equals(".gif", StringComparison.OrdinalIgnoreCase)
            && System.IO.File.Exists(path) && EcclesiaCast.Core.Media.GifFile.IsAnimated(path)
            && GifImporter.IsAvailable)
        {
            _ = UseNamePlateGifAsync(path);
            return;
        }

        NamePlateImagePath = path;
        if (path is not null)
            StatusText = $"La placa va a llevar «{System.IO.Path.GetFileName(path)}». Tocá «Mostrar» para verla.";
    }

    private async Task UseNamePlateGifAsync(string gif)
    {
        var name = System.IO.Path.GetFileName(gif);
        StatusText = $"Preparando el GIF «{name}» para la placa…";
        var video = await GifImporter.ConvertAsync(gif);
        NamePlateImagePath = video ?? gif;
        StatusText = video is null
            ? $"El GIF «{name}» no se pudo convertir: la placa lo lleva quieto, como imagen."
            : $"La placa va a llevar el GIF «{name}», en movimiento. Tocá «Mostrar» para verla.";
    }

    /// <summary>«Otra imagen o video…»: a file from the computer.</summary>
    [RelayCommand]
    private void ChooseNamePlateFile()
    {
        var filter = NamePlateMedia.DialogFilter.Split('|');
        var file = _dialogs.OpenFile(new OpenFileRequest(
            "Imagen o video para la placa", [new FileFilter(filter[0], filter[1].Split(';'))]));
        if (file is not null)
            UseNamePlateImage(file);
    }

    [RelayCommand]
    private void ClearNamePlateImage() => NamePlateImagePath = null;

    /// <summary>0: stays until «Quitar»; 1, 2, 3: goes by itself after 5, 10 or 15 seconds.</summary>
    [ObservableProperty]
    private int _namePlateAutoHide;

    /// <summary>A plate is on the screen: the «Mostrar» button lights up.</summary>
    [ObservableProperty]
    private bool _isNamePlateActive;

    private IUiTimer _namePlateTimer = null!;

    private void LoadNamePlates()
    {
        LoadNamePlateStyleNames();
        NamePlateStyleSettings.Changed += (_, _) => LoadNamePlateStyleNames();

        foreach (var plate in NamePlateList.Parse(_settings.Get(NamePlateList.SettingKey)))
            SavedNamePlates.Add(plate);

        // The field, not the property: the setter would write the setting back.
#pragma warning disable MVVMTK0034
        _namePlateAutoHide = int.TryParse(_settings.Get(NamePlateAutoHideKey), out var index)
            ? Math.Clamp(index, 0, NamePlateAutoHideSeconds.Length - 1)
            : 2; // 10 seconds, about what a lower third usually lasts
#pragma warning restore MVVMTK0034

        // The interval is set each time a plate goes up, from the list in the tab.
        _namePlateTimer = _timers.Create(TimeSpan.FromSeconds(10));
        _namePlateTimer.Tick += (_, _) =>
        {
            _namePlateTimer.Stop();
            _presentation.HideNamePlate();
        };
        _presentation.Changed += (_, _) =>
        {
            IsNamePlateActive = _presentation.NamePlate is not null;
            if (!IsNamePlateActive)
                _namePlateTimer.Stop();
        };
    }

    partial void OnNamePlateAutoHideChanged(int value) =>
        _settings.Set(NamePlateAutoHideKey, value.ToString());

    /// <summary>A plate chosen in the list fills the two lines, to show it or change it.</summary>
    partial void OnSelectedSavedNamePlateChanged(NamePlate? value)
    {
        if (value is null)
            return;
        FillNamePlateFields(value);
    }

    /// <summary>«Mostrar»: the plate written in the two lines goes on the screen (and into the list).</summary>
    [RelayCommand]
    private void ShowNamePlate()
    {
        if (NamePlate.Clean(NamePlateName, NamePlateRole, NamePlateImagePath, NamePlateImageSide == 1, NamePlateStyleName) is not { } plate)
        {
            StatusText = "Escribí el nombre (y si querés, el cargo) para mostrar la placa.";
            return;
        }

        PutNamePlate(plate);
    }

    /// <summary>
    /// «Guardar»: the plate goes into the list without being shown, and the
    /// fields are cleared, ready for the next one.
    /// </summary>
    [RelayCommand]
    private void SaveNamePlate()
    {
        if (NamePlate.Clean(NamePlateName, NamePlateRole, NamePlateImagePath, NamePlateImageSide == 1, NamePlateStyleName) is not { } plate)
        {
            StatusText = "Escribí el nombre (y si querés, el cargo) para guardar la placa.";
            return;
        }

        KeepNamePlate(plate);
        SelectedSavedNamePlate = null;
        NamePlateName = string.Empty;
        NamePlateRole = string.Empty;
        NamePlateImagePath = null;
        StatusText = $"Placa «{plate}» guardada en la lista. Doble clic para mostrarla.";
    }

    /// <summary>Double click on a plate of the list: straight to the screen.</summary>
    [RelayCommand]
    private void ShowSavedNamePlate(NamePlate? plate)
    {
        if (plate is null)
            return;

        FillNamePlateFields(plate);
        PutNamePlate(plate);
    }

    private void PutNamePlate(NamePlate plate)
    {
        if (SelectedDisplay is null)
        {
            StatusText = "No hay pantalla de salida seleccionada.";
            return;
        }

        NoteAction($"Placa: {plate}");
        // The style goes along by name in the list, and whole to the screen.
        var style = NamePlateStyleSettings.Find(plate.StyleName);
        _presentation.ShowNamePlate(plate with { Style = style });
        ShowOutputWindow();
        IsProjecting = true;
        KeepNamePlate(plate);

        var seconds = NamePlateAutoHideSeconds[Math.Clamp(NamePlateAutoHide, 0, NamePlateAutoHideSeconds.Length - 1)];
        _namePlateTimer.Stop();
        if (seconds > 0)
        {
            _namePlateTimer.Interval = TimeSpan.FromSeconds(seconds);
            _namePlateTimer.Start();
        }

        var where = style.Target switch
        {
            NamePlateTarget.StreamOnly => "Placa sólo en la transmisión (OBS)",
            NamePlateTarget.ProjectorOnly => "Placa sólo en el proyector",
            _ => "Placa en pantalla",
        };
        StatusText = seconds > 0
            ? $"{where}: {plate}. Se va sola en {seconds} segundos."
            : $"{where}: {plate}. «Quitar» la saca.";
    }

    [RelayCommand]
    private void HideNamePlate()
    {
        _namePlateTimer.Stop();
        _presentation.HideNamePlate();
        StatusText = "Placa retirada.";
    }

    /// <summary>The 🗑 of a plate in the list.</summary>
    [RelayCommand]
    private void RemoveSavedNamePlate(NamePlate? plate)
    {
        if (plate is null)
            return;

        SetSavedNamePlates(NamePlateList.Removing(SavedNamePlates.ToList(), plate));
        StatusText = $"Placa «{plate}» borrada de la lista.";
    }

    private void KeepNamePlate(NamePlate plate) =>
        SetSavedNamePlates(NamePlateList.Saving(SavedNamePlates.ToList(), plate));

    private void SetSavedNamePlates(IReadOnlyList<NamePlate> plates)
    {
        _settings.Set(NamePlateList.SettingKey, NamePlateList.ToSetting(plates));
        SavedNamePlates.Clear();
        foreach (var plate in plates)
            SavedNamePlates.Add(plate);
    }
}
