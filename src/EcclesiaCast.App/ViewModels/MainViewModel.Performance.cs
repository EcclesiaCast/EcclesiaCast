using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Remote;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Diagnostics;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Performance;
using EcclesiaCast.Core.Playlists;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Core.Timing;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

public sealed partial class MainViewModel
{
    // ── Modo PC de bajo rendimiento ──────────────────────────────

    /// <summary>
    /// Lightens the operator's panels and cuts the projector's transitions for
    /// computers without a graphics card. See <see cref="PerformanceMode"/>.
    /// </summary>
    [ObservableProperty]
    private bool _isLowPerformance;

    partial void OnIsLowPerformanceChanged(bool value)
    {
        _settings.Set(LowPerformance.SettingKey, value ? "1" : "0");
        PerformanceMode.Set(value);

        // The message is for a change the operator makes, not for starting up.
        if (!_startingUp)
            StatusText = value
                ? "Modo PC de bajo rendimiento: prendido."
                : "Modo PC de bajo rendimiento: apagado.";
    }

    private bool _startingUp = true;

    private void LoadLowPerformance()
    {
        // The installer asks once and leaves its answer in the registry; it
        // only counts until the operator has chosen something here.
        var initial = LowPerformance.InitialSetting(_settings.Get(LowPerformance.SettingKey), ReadInstallerChoice());
        if (initial is not null)
            _settings.Set(LowPerformance.SettingKey, initial);

        IsLowPerformance = LowPerformance.IsOn(_settings.Get(LowPerformance.SettingKey));
        _startingUp = false;
    }

    private static string? ReadInstallerChoice()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(LowPerformance.InstallerRegistryKey);
            return key?.GetValue(LowPerformance.InstallerValueName)?.ToString();
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            Log.Debug(ex, "No se pudo leer la respuesta del instalador");
            return null;
        }
    }
}
