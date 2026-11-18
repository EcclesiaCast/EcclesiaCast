using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// The new-version button in the top bar and the two entries of the Ayuda
/// menu. The searching and the notice itself are <see cref="IUpdates"/>'s.
/// </summary>
public sealed partial class MainViewModel
{
    private readonly IUpdates _updates;

    /// <summary>
    /// A newer version is waiting, so the «⬆ Versión nueva» button shows. It
    /// stays until the version is installed or skipped.
    /// </summary>
    [ObservableProperty]
    private bool _hasUpdate;

    /// <summary>What the button says when the mouse rests on it.</summary>
    [ObservableProperty]
    private string _updateToolTip = string.Empty;

    /// <summary>«Buscar actualizaciones al abrir» in the Ayuda menu.</summary>
    public bool CheckForUpdatesOnStartup
    {
        get => _updates.CheckOnStartup;
        set
        {
            if (_updates.CheckOnStartup == value)
                return;

            _updates.CheckOnStartup = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// «Contar en forma anónima que se usó» in Ajustes → Actualizaciones.
    /// Turning it off forgets the installation's number.
    /// </summary>
    public bool IsUsageCountEnabled
    {
        get => EcclesiaCast.Core.Diagnostics.UsageCount.IsEnabled(_settings);
        set
        {
            if (IsUsageCountEnabled == value)
                return;

            EcclesiaCast.Core.Diagnostics.UsageCount.SetEnabled(_settings, value);
            if (!value)
                EcclesiaCast.Core.Diagnostics.UsageCount.Forget(_settings);
            OnPropertyChanged();
            OnPropertyChanged(nameof(UsageChurchName));
        }
    }

    /// <summary>The optional name of the church, told with the count of use (Ajustes → Actualizaciones).</summary>
    public string UsageChurchName
    {
        get => EcclesiaCast.Core.Diagnostics.UsageCount.ChurchName(_settings);
        set
        {
            EcclesiaCast.Core.Diagnostics.UsageCount.SetChurchName(_settings, value);
            OnPropertyChanged();
        }
    }

    private void InitializeUpdates()
    {
        // The search runs by itself, so the answer may come from any thread.
        _updates.PendingChanged += (_, _) => _dispatcher.Post(SyncUpdate);
        SyncUpdate();
    }

    private void SyncUpdate()
    {
        var pending = _updates.Pending;
        HasUpdate = pending is not null;
        UpdateToolTip = pending is null
            ? string.Empty
            : $"Hay una versión nueva de EcclesiaCast: la {pending.Version}. Tocá para ver qué trae y actualizar.";
    }

    [RelayCommand]
    private void ShowUpdate() => _updates.ShowPending();

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        var before = StatusText;
        StatusText = "Buscando actualizaciones…";
        try
        {
            await _updates.CheckNowAsync();
        }
        finally
        {
            // Only if nothing else has spoken in the meantime.
            if (StatusText == "Buscando actualizaciones…")
                StatusText = before;
        }
    }
}
