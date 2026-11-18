using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// Ajustes → Videos: how a video that comes into the library is fitted to
/// the screen. It is written into each new video as it is added, so changing
/// it leaves the videos already in the library as they are, and each one can
/// still be changed by hand in Propiedades.
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>The choices, in the order of <see cref="NewVideoFitMode"/>.</summary>
    public IReadOnlyList<string> NewVideoFitChoices { get; } =
    [
        "Ajustar con los bordes desenfocados (recomendado)",
        "Ajustar con color de fondo",
        "Rellenar (recorta lo que sobra)",
        "Estirar",
    ];

    private NewVideoFit CurrentNewVideoFit => NewVideoFit.Parse(_settings.Get(NewVideoFit.SettingKey));

    public int NewVideoFitIndex
    {
        get => (int)CurrentNewVideoFit.Mode;
        set
        {
            if (value < 0 || !Enum.IsDefined((NewVideoFitMode)value) || value == NewVideoFitIndex)
                return;

            _settings.Set(NewVideoFit.SettingKey, (CurrentNewVideoFit with { Mode = (NewVideoFitMode)value }).ToSetting());
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNewVideoFitColor));
        }
    }

    /// <summary>The colour of «Ajustar con color de fondo».</summary>
    public string NewVideoFitColor
    {
        get => CurrentNewVideoFit.Color;
        set
        {
            if (!NewVideoFit.IsColor(value) || value == NewVideoFitColor)
                return;

            _settings.Set(NewVideoFit.SettingKey, (CurrentNewVideoFit with { Color = value }).ToSetting());
            OnPropertyChanged();
        }
    }

    /// <summary>The colour button only matters for «Ajustar con color de fondo».</summary>
    public bool IsNewVideoFitColor => CurrentNewVideoFit.Mode == NewVideoFitMode.Color;

    /// <summary>Gives a video that is just coming into the library the framing chosen in Ajustes.</summary>
    private void ApplyNewVideoFit(MediaItem item) => CurrentNewVideoFit.ApplyTo(item);
}
