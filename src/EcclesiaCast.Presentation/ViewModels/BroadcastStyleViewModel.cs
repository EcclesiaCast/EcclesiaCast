using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Remote;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// Backs «Transmisión → Estilos de la transmisión…»: the design of each of the
/// three layers of the stream. The operator works on a draft; nothing is written
/// until <see cref="Save"/>, and closing without saving puts the stream back on
/// what was saved when the window opened.
/// </summary>
public sealed class BroadcastStyleViewModel
{
    /// <summary>The layers, in the order they are written.</summary>
    private static readonly OutputChannel[] Channels =
        [OutputChannel.Bible, OutputChannel.Songs, OutputChannel.Announcements];

    private readonly ISettingsStore _settings;
    private readonly IColorPicker _colorPicker;

    /// <summary>What was saved when the window opened. The stream shows the draft while editing, and goes back to this on Cancelar.</summary>
    private readonly BroadcastStyles _saved = BroadcastStyleSettings.Current;
    private BroadcastStyles _draft = BroadcastStyleSettings.Current;
    private bool _wasSaved;

    public BroadcastStyleViewModel(ISettingsStore settings, IColorPicker colorPicker)
    {
        _settings = settings;
        _colorPicker = colorPicker;
    }

    /// <summary>The layer that is open.</summary>
    public OutputChannel Channel { get; set; } = OutputChannel.Bible;

    /// <summary>The style of the open layer, as drafted.</summary>
    public BroadcastStyle Current => _draft.For(Channel);

    /// <summary>The look of the open layer is the one the controls describe (clamped to what is allowed).</summary>
    public void Edit(BroadcastStyle fromControls) => _draft = _draft.With(Channel, fromControls.Clamp());

    /// <summary>The operator picked a ready-made look: it is applied over the open layer.</summary>
    public void ApplyPreset(BroadcastPreset preset) => _draft = _draft.With(Channel, preset.ApplyTo(Current));

    /// <summary>The open layer goes back to the factory design.</summary>
    public void ResetToDefaults() => _draft = _draft.With(Channel, BroadcastStyle.Default(Channel));

    /// <summary>The operator chose a picture or video for the background.</summary>
    public void SetBackgroundFile(string path) =>
        _draft = _draft.With(Channel, Current with { BackgroundFile = path, Background = BroadcastBackground.File });

    /// <summary>No file was picked after asking for «Imagen o video…»: the background goes back to transparent.</summary>
    public void ClearBackground() =>
        _draft = _draft.With(Channel, Current with { Background = BroadcastBackground.None });

    /// <summary>
    /// Opens the colour picker on one of the colours of the open layer
    /// («Text», «Accent», «Plate», «BoxBorder»; anything else is the background colour)
    /// and keeps the one chosen. False when the operator cancelled.
    /// </summary>
    public bool PickColor(string which)
    {
        var s = Current;
        var current = which switch
        {
            "Text" => s.TextColor,
            "Accent" => s.AccentColor,
            "Plate" => s.PlateColor,
            "BoxBorder" => s.BoxBorderColor,
            _ => s.BackgroundColor,
        };

        var picked = _colorPicker.Pick(current);
        if (picked is null)
            return false;

        _draft = _draft.With(Channel, (which switch
        {
            "Text" => s with { TextColor = picked },
            "Accent" => s with { AccentColor = picked },
            "Plate" => s with { PlateColor = picked },
            "BoxBorder" => s with { BoxBorderColor = picked },
            _ => s with { BackgroundColor = picked },
        }).Clamp());
        return true;
    }

    /// <summary>Writes the layers that changed since the window opened, and applies them to the stream.</summary>
    public void Save()
    {
        foreach (var channel in Channels)
        {
            // The stream already shows the draft, so it is compared with what was saved.
            if (_draft.For(channel) != _saved.For(channel))
                BroadcastStyleSettings.Set(channel, _draft.For(channel), _settings);
        }

        _wasSaved = true;
    }

    /// <summary>The window closed: if it was not saved, the stream goes back to what was saved.</summary>
    public void Closed()
    {
        if (!_wasSaved)
            BroadcastStyleSettings.Preview(_saved);
    }
}
