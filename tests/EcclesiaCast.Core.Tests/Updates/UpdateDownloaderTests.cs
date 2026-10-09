using System.Security.Cryptography;
using EcclesiaCast.Core.Updates;

namespace EcclesiaCast.Core.Tests.Updates;

public class UpdateDownloaderTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"ecclesiacast-update-test-{Guid.NewGuid():N}");
    private readonly List<IDisposable> _disposables = [];

    public void Dispose()
    {
        foreach (var item in _disposables)
            item.Dispose();
        try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
    }

    private static byte[] Content(int length = 300_000)
    {
        var bytes = new byte[length];
        new Random(7).NextBytes(bytes);
        return bytes;
    }

    private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private LocalHttpServer Serve(Func<string, ServerResponse> handler)
    {
        var server = new LocalHttpServer(handler);
        _disposables.Add(server);
        return server;
    }

    private UpdateDownloader Downloader(TimeSpan? stall = null)
    {
        var client = new HttpClient();
        _disposables.Add(client);
        return new UpdateDownloader(client) { StallTimeout = stall ?? TimeSpan.FromSeconds(30) };
    }

    private static ReleaseAsset Asset(LocalHttpServer server, byte[] content, string name = "EcclesiaCast-1.0.1-setup.exe") =>
        new(name, server.BaseUrl + "/download/" + name, content.Length, Sha256(content));

    [Fact]
    public async Task A_correct_download_arrives_whole_and_passes_the_checks()
    {
        var content = Content();
        var server = Serve(_ => ServerResponse.Ok(content));
        var asset = Asset(server, content);

        var path = await Downloader().DownloadAsync(asset, _folder, null, CancellationToken.None);

        Assert.Equal(Path.Combine(_folder, asset.Name), path);
        Assert.Equal(content, await File.ReadAllBytesAsync(path));
        Assert.Null(await UpdateVerifier.CheckAsync(path, asset));
        Assert.Empty(Directory.GetFiles(_folder, "*.part"));
    }

    [Fact]
    public async Task The_progress_goes_from_zero_to_one_hundred()
    {
        var content = Content(1_000_000);
        var server = Serve(_ => ServerResponse.Ok(content));
        var reports = new List<double>();

        await Downloader().DownloadAsync(Asset(server, content), _folder, new SyncProgress(reports.Add), CancellationToken.None);

        Assert.Equal(0, reports.First());
        Assert.Equal(100, reports.Last());
        Assert.Equal(reports.OrderBy(r => r), reports);
    }

    [Fact]
    public async Task A_file_left_from_an_earlier_try_is_replaced()
    {
        var content = Content();
        var server = Serve(_ => ServerResponse.Ok(content));
        var asset = Asset(server, content);
        Directory.CreateDirectory(_folder);
        await File.WriteAllTextAsync(Path.Combine(_folder, asset.Name), "viejo");

        var path = await Downloader().DownloadAsync(asset, _folder, null, CancellationToken.None);

        Assert.Equal(content, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task A_file_with_a_different_size_does_not_pass()
    {
        var content = Content();
        var server = Serve(_ => ServerResponse.Ok(content));
        var asset = Asset(server, content) with { Size = content.Length + 10 };

        var path = await Downloader().DownloadAsync(asset, _folder, null, CancellationToken.None);

        var problem = await UpdateVerifier.CheckAsync(path, asset);
        Assert.NotNull(problem);
        Assert.Contains("tamaño", problem);
    }

    [Fact]
    public async Task A_file_with_a_different_sha256_does_not_pass()
    {
        var content = Content();
        var server = Serve(_ => ServerResponse.Ok(content));
        var asset = Asset(server, content) with { Sha256 = new string('0', 64) };

        var path = await Downloader().DownloadAsync(asset, _folder, null, CancellationToken.None);

        var problem = await UpdateVerifier.CheckAsync(path, asset);
        Assert.NotNull(problem);
        Assert.Contains("SHA-256", problem);
    }

    [Fact]
    public async Task One_changed_byte_is_enough_to_fail_the_sha256()
    {
        var content = Content();
        var tampered = (byte[])content.Clone();
        tampered[1234] ^= 0xFF;
        var server = Serve(_ => ServerResponse.Ok(tampered));
        var asset = Asset(server, content); // the size is the same, the hash is not

        var path = await Downloader().DownloadAsync(asset, _folder, null, CancellationToken.None);

        Assert.Contains("SHA-256", await UpdateVerifier.CheckAsync(path, asset));
    }

    [Fact]
    public async Task Without_a_digest_only_the_size_is_compared()
    {
        var content = Content();
        var server = Serve(_ => ServerResponse.Ok(content));
        var asset = Asset(server, content) with { Sha256 = null };

        var path = await Downloader().DownloadAsync(asset, _folder, null, CancellationToken.None);

        Assert.Null(await UpdateVerifier.CheckAsync(path, asset));
    }

    [Fact]
    public async Task The_hash_is_compared_without_caring_about_capitals_in_the_asset()
    {
        var content = Content();
        var asset = new ReleaseAsset("a.exe", "https://x/a.exe", content.Length,
            GitHubReleases.Sha256Of("sha256:" + Sha256(content).ToUpperInvariant()));
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "a.exe");
        await File.WriteAllBytesAsync(path, content);

        Assert.Null(await UpdateVerifier.CheckAsync(path, asset));
    }

    [Fact]
    public async Task A_download_cut_short_fails_and_leaves_nothing_behind()
    {
        var content = Content();
        // The server promises the whole file and closes after the first third.
        var server = Serve(_ => new ServerResponse(200, content[..(content.Length / 3)], DeclaredLength: content.Length));
        var asset = Asset(server, content);

        var ex = await Assert.ThrowsAsync<UpdateException>(
            () => Downloader().DownloadAsync(asset, _folder, null, CancellationToken.None));

        Assert.Equal(UpdateFailure.Download, ex.Failure);
        Assert.Equal("No se pudo descargar la actualización. Revisá la conexión a internet y probá de nuevo.", ex.Message);
        Assert.Empty(Directory.GetFiles(_folder));
    }

    [Fact]
    public async Task A_server_that_stops_answering_is_given_up_on()
    {
        var content = Content();
        var server = Serve(_ => new ServerResponse(200, content[..1000], DeclaredLength: content.Length, HangAfterBody: true));
        var asset = Asset(server, content);

        var ex = await Assert.ThrowsAsync<UpdateException>(
            () => Downloader(stall: TimeSpan.FromMilliseconds(500)).DownloadAsync(asset, _folder, null, CancellationToken.None));

        Assert.Equal(UpdateFailure.Download, ex.Failure);
        Assert.Empty(Directory.GetFiles(_folder));
    }

    [Fact]
    public async Task A_missing_file_on_the_server_fails_the_download()
    {
        var server = Serve(_ => new ServerResponse(404, []));
        var asset = new ReleaseAsset("EcclesiaCast-1.0.1-setup.exe", server.BaseUrl + "/nada", 10, null);

        var ex = await Assert.ThrowsAsync<UpdateException>(
            () => Downloader().DownloadAsync(asset, _folder, null, CancellationToken.None));

        Assert.Equal(UpdateFailure.Download, ex.Failure);
    }

    [Fact]
    public async Task A_server_that_is_not_there_fails_the_download()
    {
        var server = Serve(_ => ServerResponse.Ok(""));
        var address = server.BaseUrl;
        server.Dispose();

        var ex = await Assert.ThrowsAsync<UpdateException>(
            () => Downloader().DownloadAsync(new ReleaseAsset("a-setup.exe", address + "/a", 1, null), _folder, null, CancellationToken.None));

        Assert.Equal(UpdateFailure.Download, ex.Failure);
    }

    [Fact]
    public async Task Cancelling_stops_the_download_and_leaves_nothing_behind()
    {
        var content = Content();
        var server = Serve(_ => new ServerResponse(200, content[..1000], DeclaredLength: content.Length, HangAfterBody: true));
        using var cancel = new CancellationTokenSource();
        cancel.CancelAfter(400);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Downloader().DownloadAsync(Asset(server, content), _folder, null, cancel.Token));

        Assert.Empty(Directory.GetFiles(_folder));
    }

    [Theory]
    [InlineData("..\\..\\evil.exe")]
    [InlineData("../evil.exe")]
    public async Task A_file_name_that_walks_out_of_the_folder_lands_inside_it(string name)
    {
        var content = Content(10);
        var server = Serve(_ => ServerResponse.Ok(content));

        var path = await Downloader().DownloadAsync(Asset(server, content, name), _folder, null, CancellationToken.None);

        Assert.Equal(Path.Combine(_folder, "evil.exe"), path);
        Assert.Single(Directory.GetFiles(_folder));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a|b.exe")]
    public async Task A_file_name_that_is_not_a_name_is_refused(string name)
    {
        var content = Content(10);
        var server = Serve(_ => ServerResponse.Ok(content));

        var ex = await Assert.ThrowsAsync<UpdateException>(
            () => Downloader().DownloadAsync(Asset(server, content, name), _folder, null, CancellationToken.None));

        Assert.Equal(UpdateFailure.Invalid, ex.Failure);
    }

    [Fact]
    public void The_invalid_message_is_the_one_the_operator_is_told()
    {
        Assert.Equal(
            "La actualización descargada no es válida; no se instaló nada.",
            UpdateException.NotValid("motivo").Message);
    }

    /// <summary>Progress that reports on the spot, so a test can read it right after the await.</summary>
    private sealed class SyncProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }
}
