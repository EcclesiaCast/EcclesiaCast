using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.Services;

/// <summary>What the operator typed last time, so they do not type it again.</summary>
public sealed record CountdownSettings(int Minutes, string Clock, string Heading, string FinishedMessage)
{
    public static readonly CountdownSettings Default = new(5, "10:30", "Empezamos en", "¡Bienvenidos!");
}

/// <summary>What the operator decided in the countdown window.</summary>
public sealed record CountdownChoice(Countdown? Countdown, CountdownSettings Settings, bool Stop);

/// <summary>Setting up the pre-service countdown.</summary>
public interface ICountdownDialog
{
    /// <summary>Returns null when the operator closed the window without deciding.</summary>
    CountdownChoice? Show(CountdownSettings settings, bool isRunning);
}
