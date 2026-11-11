using System.IO;
using System.Text;
using Serilog;
using Whisper.net;
using Whisper.net.Ggml;

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

    /// <summary>
    /// Whisper works on half-minute blocks. For the few seconds listened to
    /// each time, a quarter of that is plenty and makes it four times faster.
    /// </summary>
    private const int ShortAudioContext = 448;

    private WhisperFactory? _factory;
    private WhisperProcessor? _processor;

    public static string ModelPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EcclesiaCast", "modelos", ModelFile);

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

        // Half the cores, so the video and the window keep theirs.
        var threads = Math.Clamp(Environment.ProcessorCount / 2, 2, 8);
        _factory = WhisperFactory.FromPath(ModelPath);
        _processor = _factory.CreateBuilder()
            .WithLanguage("es")
            .WithThreads(threads)
            .WithNoContext()
            .WithSingleSegment()
            .WithAudioContextSize(ShortAudioContext)
            .Build();
        Log.Information("Reconocedor de voz cargado ({Threads} hilos)", threads);
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
