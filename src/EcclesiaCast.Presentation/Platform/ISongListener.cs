namespace EcclesiaCast.Presentation.Platform;

/// <summary>One place the program can listen to: a microphone or line input, or what this PC is playing.</summary>
/// <param name="Id">The system's id of the device; empty for the default input.</param>
/// <param name="Name">What the operator reads in the list.</param>
/// <param name="Loopback">True when it is an output being listened to (what the PC plays), for trying things out.</param>
public sealed record SoundInputDevice(string Id, string Name, bool Loopback)
{
    public override string ToString() => Name;
}

/// <summary>What the listener made of the last seconds of sound.</summary>
/// <param name="Text">The words that were plausibly sung; empty when there were none.</param>
/// <param name="Seconds">How long the stretch listened to was.</param>
/// <param name="Cost">How long the recogniser took: the singing went on meanwhile.</param>
/// <param name="Level">The loudest moment of the stretch, 0 to 1, before it was evened out.</param>
public readonly record struct HeardWindow(string Text, double Seconds, double Cost, float Level);

/// <summary>The sound inputs of the computer.</summary>
public interface ISoundInput
{
    /// <summary>The inputs there are now, the default one first, and the outputs to listen to for a trial.</summary>
    IReadOnlyList<SoundInputDevice> Devices();
}

/// <summary>The speech recogniser's model, which is downloaded once.</summary>
public interface ISpeechEar
{
    /// <summary>How big the download is, to tell the operator («unos 550 MB»).</summary>
    string ModelSizeText { get; }

    /// <summary>True when the model is already on this computer.</summary>
    bool HasModel { get; }

    /// <summary>Downloads the model; <paramref name="progress"/> gets the megabytes so far.</summary>
    Task DownloadModelAsync(IProgress<double>? progress, CancellationToken cancel);
}

/// <summary>
/// «Seguir la canción»: keeps listening to an input while it is on and tells
/// what was heard. The events are raised on the screen's thread.
/// </summary>
public interface ISongListener : IDisposable
{
    /// <summary>Quieter than this nothing is coming in at all (a muted input).</summary>
    public const float Silence = 0.002f;

    /// <summary>Raised for every stretch listened to.</summary>
    event Action<HeardWindow>? Heard;

    /// <summary>Raised when listening stops by itself, with why.</summary>
    event Action<string>? Failed;

    /// <summary>Raised once the recogniser is ready, with true when it runs on the video card.</summary>
    event Action<bool>? Ready;

    /// <summary>The level of the input since the last look, 0 to 1.</summary>
    float TakeLevel();

    void Start(SoundInputDevice device);

    void Stop();
}
