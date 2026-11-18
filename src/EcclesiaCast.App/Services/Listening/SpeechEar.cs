using System.IO;
using System.Text;
using Serilog;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.LibraryLoader;

namespace EcclesiaCast.App.Services.Listening;

/// <summary>
/// The speech recogniser (Whisper, running on this PC: nothing is sent
/// anywhere). Its model is a big file that is downloaded once, the first time
/// the operator turns listening on, like a Bible.
/// </summary>
public sealed class SpeechEar : IDisposable
{
    /// <summary>
    /// The model that understood singing over a band in the trials with real
    /// services (the small ones wrote nonsense); this is its compact form.
    /// </summary>
    private const string ModelFile = "ggml-large-v3-turbo-q5_0.bin";

    /// <summary>About how much is downloaded, for the operator to know before starting.</summary>
    public const string ModelSizeText = "unos 550 MB";

    /// <summary>More pieces of words than this in a few seconds is the recogniser looping, not singing.</summary>
    private const int MaxTokens = 64;

    private readonly int _audioContext;
    private WhisperFactory? _factory;
    private WhisperProcessor? _processor;

    /// <param name="window">How much sound is handed over each time.</param>
    public SpeechEar(TimeSpan window) => _audioContext = ContextFor(window);

    /// <summary>
    /// Whisper works on half-minute blocks and looks at 50 steps a second.
    /// For the few seconds listened to each time only that much is looked at,
    /// with a little room: the shorter, the faster.
    /// </summary>
    public static int ContextFor(TimeSpan window) => (int)Math.Ceiling((window.TotalSeconds * 50 + 64) / 64) * 64;

    /// <summary>True when the video card does the work: about five times faster than the processor on the church notebook.</summary>
    public bool OnVideoCard { get; private set; }

    public static string ModelPath => Path.Combine(PlatformServices.Paths.ModelsFolder, ModelFile);

    public static bool HasModel => File.Exists(ModelPath);

    /// <summary>Downloads the model. <paramref name="progress"/> gets megabytes so far.</summary>
    public static async Task DownloadModelAsync(IProgress<double>? progress, CancellationToken cancel)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ModelPath)!);
        var partial = ModelPath + ".part";

        using (var source = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(
                   GgmlType.LargeV3Turbo, QuantizationType.Q5_0, cancel))
        await using (var file = File.Create(partial))
        {
            var buffer = new byte[1 << 20];
            long total = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancel)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read), cancel);
                total += read;
                progress?.Report(total / 1048576.0);
            }
        }

        File.Move(partial, ModelPath, overwrite: true);
        Log.Information("Modelo de voz descargado en {Path}", ModelPath);
    }

    /// <summary>Loads the model (a few seconds). Throws when this PC cannot run it.</summary>
    public void Load()
    {
        if (_processor is not null)
            return;

        // The video card first (any brand, through Vulkan); the processor when
        // this PC has no Vulkan. Has to be said before the first load.
        RuntimeOptions.RuntimeLibraryOrder = [RuntimeLibrary.Vulkan, RuntimeLibrary.Cpu];

        // Half the cores, so the video and the window keep theirs.
        var threads = Math.Clamp(Environment.ProcessorCount / 2, 2, 8);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        _factory = WhisperFactory.FromPath(ModelPath);
        OnVideoCard = RuntimeOptions.LoadedLibrary == RuntimeLibrary.Vulkan;
        _processor = _factory.CreateBuilder()
            .WithLanguage("es")
            .WithThreads(threads)
            .WithNoContext()
            .WithSingleSegment()
            .WithAudioContextSize(_audioContext)
            .WithMaxTokensPerSegment(MaxTokens)
            .Build();

        // The first pass on a video card prepares it and takes seconds: better
        // now than on the first line sung.
        Transcribe(new float[SoundInput.Rate], CancellationToken.None);
        Log.Information("Reconocedor de voz cargado en {Engine} ({Threads} hilos, listo en {Seconds:0.0} s)",
            OnVideoCard ? "la placa de video" : "el procesador", threads, clock.Elapsed.TotalSeconds);
    }

    /// <summary>What was said or sung in a few seconds of sound, as the recogniser wrote it.</summary>
    public string Transcribe(float[] samples, CancellationToken cancel)
    {
        if (_processor is null || samples.Length == 0)
            return string.Empty;

        var text = new StringBuilder();
        var segments = _processor.ProcessAsync(samples, cancel).GetAsyncEnumerator(cancel);
        try
        {
            while (segments.MoveNextAsync().AsTask().GetAwaiter().GetResult())
                text.Append(segments.Current.Text).Append(' ');
        }
        finally
        {
            segments.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        return text.ToString();
    }

    public void Dispose()
    {
        _processor?.Dispose();
        _processor = null;
        _factory?.Dispose();
        _factory = null;
    }
}
