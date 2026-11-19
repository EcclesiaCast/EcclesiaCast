namespace EcclesiaCast.Core.Media;

/// <summary>
/// What the operator is told when YouTube's player refuses a video while it is
/// projected «desde internet». The congregation sees black instead of YouTube's
/// own error card; the operator gets this, with what to do.
/// </summary>
/// <remarks>
/// The numbers are the IFrame player API's: 2 a bad video id, 5 the browser
/// cannot play it, 100 not found or private, 101 and 150 the owner does not
/// allow it outside YouTube (most often for copyright), 153 the player was
/// set up wrong.
/// </remarks>
public static class YouTubePlayerError
{
    /// <summary>The owner does not let the video play outside YouTube: only a download works.</summary>
    public static bool OnlyByDownloading(int code) => code is 101 or 150;

    public static string Title(int code) => code switch
    {
        101 or 150 => "Este video sólo se puede usar descargado",
        100 => "El video ya no está en YouTube",
        _ => "YouTube no pudo reproducir el video",
    };

    public static string Explain(int code) => code switch
    {
        101 or 150 =>
            "Su dueño no deja que se vea dentro de otros programas (suele ser por derechos de autor), así que «desde internet» no anda.\n\n"
            + "Descargalo y proyectá la copia: clic derecho sobre el video en Medios → «Descargar para uso local».\n\n"
            + "Mientras tanto, en la pantalla quedó negro.",
        100 =>
            "No existe más, o su dueño lo hizo privado.\n\nMientras tanto, en la pantalla quedó negro.",
        _ =>
            $"YouTube lo rechazó (error {code}). Probá descargarlo: clic derecho sobre el video en Medios → «Descargar para uso local».\n\n"
            + "Mientras tanto, en la pantalla quedó negro.",
    };
}
