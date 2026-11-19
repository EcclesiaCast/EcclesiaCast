using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.ViewModels;

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

    /// <summary>0: stays until «Quitar»; 1, 2, 3: goes by itself after 5, 10 or 15 seconds.</summary>
    [ObservableProperty]
    private int _namePlateAutoHide;

    /// <summary>A plate is on the screen: the «Mostrar» button lights up.</summary>
    [ObservableProperty]
    private bool _isNamePlateActive;

    private readonly DispatcherTimer _namePlateTimer = new();

    private void LoadNamePlates()
    {
        foreach (var plate in NamePlateList.Parse(_settings.Get(NamePlateList.SettingKey)))
            SavedNamePlates.Add(plate);

        _namePlateAutoHide = int.TryParse(_settings.Get(NamePlateAutoHideKey), out var index)
            ? Math.Clamp(index, 0, NamePlateAutoHideSeconds.Length - 1)
            : 2; // 10 seconds, about what a lower third usually lasts

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
        NamePlateName = value.Name;
        NamePlateRole = value.Role;
    }

    /// <summary>«Mostrar»: the plate written in the two lines goes on the screen (and into the list).</summary>
    [RelayCommand]
    private void ShowNamePlate()
    {
        if (NamePlate.Clean(NamePlateName, NamePlateRole) is not { } plate)
        {
            StatusText = "Escribí el nombre (y si querés, el cargo) para mostrar la placa.";
            return;
        }

        PutNamePlate(plate);
    }

    /// <summary>Double click on a plate of the list: straight to the screen.</summary>
    [RelayCommand]
    private void ShowSavedNamePlate(NamePlate? plate)
    {
        if (plate is null)
            return;

        NamePlateName = plate.Name;
        NamePlateRole = plate.Role;
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
        _presentation.ShowNamePlate(plate);
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

        StatusText = seconds > 0
            ? $"Placa en pantalla: {plate}. Se va sola en {seconds} segundos."
            : $"Placa en pantalla: {plate}. «Quitar» la saca.";
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
