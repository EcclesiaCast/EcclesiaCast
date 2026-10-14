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
    // ── Logos ────────────────────────────────────────────────────

    private const string ActiveLogoKey = "logo.active";

    /// <summary>The church's logos, in the order set in the manager.</summary>
    public ObservableCollection<Logo> Logos { get; } = [];

    [ObservableProperty]
    private Logo? _selectedLogo;

    /// <summary>Name shown next to the Logo button; falls back when none is set up.</summary>
    public string ActiveLogoName => SelectedLogo?.Name ?? "sin logo";

    /// <summary>True when the active logo is a file that can go behind the lyrics.</summary>
    public bool CanUseLogoAsBackground => SelectedLogo?.CanBeBackground == true;

    /// <summary>Marks that the starter logo was created, so deleting it sticks.</summary>
    private const string LogosSeededKey = "logo.seeded";

    private void LoadLogos(int? keepId)
    {
        keepId ??= SelectedLogo?.Id
            ?? (int.TryParse(_settings.Get(ActiveLogoKey), out var saved) ? saved : null);

        // First run: leave one text logo in place so F3 shows something, and
        // so the manager opens with an example instead of an empty list.
        if (_settings.Get(LogosSeededKey) is null)
        {
            _settings.Set(LogosSeededKey, "1");
            if (_logos.GetAll().Count == 0)
            {
                var starter = _logos.Save(new Logo
                {
                    Name = "Reunión general",
                    Kind = LogoKind.Text,
                    Text = "Bienvenidos",
                    Order = 0,
                });
                keepId ??= starter.Id;
            }
        }

        Logos.Clear();
        foreach (var logo in _logos.GetAll())
            Logos.Add(logo);

        SelectedLogo = Logos.FirstOrDefault(l => l.Id == keepId) ?? Logos.FirstOrDefault();
    }

    partial void OnSelectedLogoChanged(Logo? value)
    {
        _presentation.SetActiveLogo(value);
        OnPropertyChanged(nameof(ActiveLogoName));
        OnPropertyChanged(nameof(CanUseLogoAsBackground));

        if (value is not null)
            _settings.Set(ActiveLogoKey, value.Id.ToString());
    }

    /// <summary>Picks a logo from the button's dropdown and shows it right away.</summary>
    [RelayCommand]
    private void SelectLogo(Logo? logo)
    {
        if (logo is null)
            return;

        SelectedLogo = logo;

        // Already on the logo? Switching one for the other is the whole point.
        if (!IsLogoActive)
            ToggleLogo();
        else
            StatusText = $"Logo: {logo.Name}.";
    }

    /// <summary>
    /// Right-click on a picture or video → «Usar como logo». It only adds the
    /// logo to the list: switching the screen to it is F3's job, and doing it
    /// here would blank the output in the middle of a service.
    /// </summary>
    [RelayCommand]
    private async Task UseMediaAsLogoAsync(MediaItem? item)
    {
        // A rendered PDF/PPT page, or the video made from a GIF, goes away
        // with its media, which would leave the logo pointing at a missing file.
        if (item is null
            || item.Type is not (MediaType.Image or MediaType.Video)
            || string.IsNullOrWhiteSpace(item.Path)
            || DocumentImporter.IsRenderedPage(item.Path)
            || GifImporter.IsConverted(item.Path))
            return;

        var existing = Logos.FirstOrDefault(l =>
            l.Kind != LogoKind.Text && string.Equals(l.Path, item.Path, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            SelectedLogo = existing;
            StatusText = $"«{existing.Name}» ya era un logo; quedó elegido (F3 lo muestra).";
            return;
        }

        var isVideo = item.Type == MediaType.Video;

        // A brand-new poster, not the card's thumbnail: deleting the logo
        // deletes its poster, and the card still needs its own.
        var poster = isVideo
            ? await Task.Run(() => MediaThumbnails.Create(item.Path, MediaType.Video, App.VideoEngine))
            : null;

        var created = _logos.Save(new Logo
        {
            Name = item.Name,
            Kind = isVideo ? LogoKind.Video : LogoKind.Image,
            Path = item.Path,
            PosterPath = poster,
            Scaling = item.Scaling,
            Muted = true,
            Order = Logos.Count,
        });

        LoadLogos(created.Id);
        RefreshLightCopies();
        StatusText = $"«{created.Name}» agregado como logo (F3 lo muestra).";
    }
}
