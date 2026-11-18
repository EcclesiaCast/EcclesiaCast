namespace EcclesiaCast.App.Services.Listening;

/// <summary>The recogniser's model (<see cref="SpeechEar"/>) behind <see cref="ISpeechEar"/>.</summary>
public sealed class SpeechEarModel : ISpeechEar
{
    public string ModelSizeText => SpeechEar.ModelSizeText;

    public bool HasModel => SpeechEar.HasModel;

    public Task DownloadModelAsync(IProgress<double>? progress, CancellationToken cancel) =>
        SpeechEar.DownloadModelAsync(progress, cancel);
}
