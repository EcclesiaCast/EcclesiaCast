using System.Globalization;
using System.Text;
using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Core.Feedback;

/// <summary>
/// The few numbers that say how much the program is used: how many times it
/// was opened and for how long. They stay on this computer; they only leave
/// inside a comment, when the person ticks «Adjuntar el resumen de uso» and
/// has seen the text first. No song, name or file of the library is in it.
/// </summary>
public sealed class UsageStats(ISettingsStore settings, Func<DateTime>? now = null)
{
    private const string IdKey = "usage.id";
    private const string FirstKey = "usage.first";
    private const string OpensKey = "usage.opens";
    private const string MinutesKey = "usage.minutes";

    private readonly Func<DateTime> _now = now ?? (() => DateTime.Now);

    public int Opens => ReadInt(OpensKey);

    public int Minutes => ReadInt(MinutesKey);

    public void RecordStart()
    {
        if (string.IsNullOrEmpty(settings.Get(IdKey)))
            settings.Set(IdKey, Guid.NewGuid().ToString("N")[..12]);
        if (string.IsNullOrEmpty(settings.Get(FirstKey)))
            settings.Set(FirstKey, _now().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        settings.Set(OpensKey, (Opens + 1).ToString(CultureInfo.InvariantCulture));
    }

    public void AddMinutes(int minutes)
    {
        if (minutes > 0)
            settings.Set(MinutesKey, (Minutes + minutes).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>The text that travels with a comment.</summary>
    public string BuildSummary(string version, string windows, int sessionMinutes, IReadOnlyList<string> recentErrors)
    {
        var text = new StringBuilder();
        text.AppendLine("Resumen de uso de EcclesiaCast");
        text.AppendLine($"Instalación (código al azar): {settings.Get(IdKey) ?? "-"}");
        text.AppendLine($"Versión: {version}");
        text.AppendLine($"Windows: {windows}");
        text.AppendLine($"Se abrió por primera vez: {settings.Get(FirstKey) ?? "-"}");
        text.AppendLine($"Veces que se abrió: {Opens}");
        text.AppendLine($"Minutos de uso (sin contar esta sesión): {Minutes}");
        text.AppendLine($"Minutos de esta sesión: {sessionMinutes}");
        text.AppendLine(recentErrors.Count == 0
            ? "Errores recientes: ninguno"
            : $"Errores recientes ({recentErrors.Count}):");
        foreach (var line in recentErrors)
            text.AppendLine($"  {line}");
        return text.ToString().TrimEnd();
    }

    private int ReadInt(string key) =>
        int.TryParse(settings.Get(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
}
