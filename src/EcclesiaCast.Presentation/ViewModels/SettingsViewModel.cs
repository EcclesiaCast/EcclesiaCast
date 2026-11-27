namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// What «Ajustes» needs beyond the main view model, which holds every
/// preference: which sections and buttons this system has and the texts that
/// name it. The window is bound to <see cref="Main"/> for everything else.
/// </summary>
public sealed class SettingsViewModel(MainViewModel main, IPlatformInfo platform)
{
    public MainViewModel Main { get; } = main;

    private bool IsMac => platform.System == PlatformSystem.Mac;

    /// <summary>The «Seguir la canción» section.</summary>
    public bool ShowFollowSong => platform.IsAvailable(Feature.FollowSong) && Main.IsListeningAvailable;

    /// <summary>
    /// «Buscar actualizaciones»: where the program cannot update itself, the
    /// section keeps only the count of use and the name of the church.
    /// </summary>
    public bool ShowUpdateCheck => platform.IsAvailable(Feature.Updates);

    /// <summary>The name of that section: its pill and its title.</summary>
    public string UpdatesTitle => ShowUpdateCheck ? "Actualizaciones" : "Contador de uso";

    public string SoundText => IsMac
        ? "Si no suena nada después de cambiar la salida de audio de la Mac (parlantes, auriculares, "
          + "Bluetooth): vuelve a abrir el sonido de la música y de los videos, en el mismo lugar."
        : "Si no suena nada después de cambiar la salida de audio de Windows (parlantes, auriculares, "
          + "Bluetooth): vuelve a abrir el sonido de la música y de los videos, en el mismo lugar. "
          + "Los videos de YouTube siguen solos la salida de Windows.";

    /// <summary>«Color de fondo…» of the new videos.</summary>
    public void PickNewVideoColor(IColorPicker colors)
    {
        if (colors.Pick(Main.NewVideoFitColor) is { } picked)
            Main.NewVideoFitColor = picked;
    }
}
