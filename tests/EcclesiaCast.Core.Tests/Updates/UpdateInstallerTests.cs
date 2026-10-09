using System.Security.Cryptography;
using EcclesiaCast.Core.Updates;

namespace EcclesiaCast.Core.Tests.Updates;

/// <summary>
/// The steps between «Actualizar ahora» and the installer. The installer is
/// never started here: the launcher is a fake that only writes down what it
/// was asked.
/// </summary>
public class UpdateInstallerTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"ecclesiacast-update-test-{Guid.NewGuid():N}");
    private readonly List<IDisposable> _disposables = [];
    private readonly List<string> _log = [];

    public void Dispose()
    {
        foreach (var item in _disposables)
            item.Dispose();
        try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
    }

    private sealed class FakeSignatures(Dictionary<string, SignatureInfo>? byFileName = null) : ISignatureReader
    {
        public SignatureInfo Read(string path) =>
            byFileName is not null && byFileName.TryGetValue(Path.GetFileName(path), out var info)
                ? info
                : SignatureInfo.Unsigned;
    }

    private sealed class FakeLauncher(List<string> log) : IInstallerLauncher
    {
        public void Launch(string installerPath, string arguments) => log.Add($"launch {Path.GetFileName(installerPath)} {arguments}");
    }

    private static readonly byte[] Content = new byte[200_000];

    private static UpdateOffer Offer(LocalHttpServer server, long? size = null, string? sha = null)
    {
        new Random(3).NextBytes(Content);
        var asset = new ReleaseAsset(
            "EcclesiaCast-1.0.1-setup.exe", server.BaseUrl + "/setup.exe",
            size ?? Content.Length, sha ?? Convert.ToHexString(SHA256.HashData(Content)).ToLowerInvariant());
        return new UpdateOffer(AppVersion.TryParse("1.0.1")!, "v1.0.1", false, "", asset);
    }

    private (UpdateInstaller Installer, LocalHttpServer Server) Build(
        SignatureInfo? installed = null,
        SignatureInfo? candidate = null,
        Action<AppVersion>? backup = null)
    {
        var server = new LocalHttpServer(_ => ServerResponse.Ok(Content));
        _disposables.Add(server);
        var client = new HttpClient();
        _disposables.Add(client);

        var signatures = new FakeSignatures(candidate is null
            ? null
            : new Dictionary<string, SignatureInfo> { ["EcclesiaCast-1.0.1-setup.exe"] = candidate });

        var installer = new UpdateInstaller(
            new UpdateDownloader(client),
            signatures,
            () => installed ?? SignatureInfo.Unsigned,
            backup ?? (version => _log.Add($"backup {version}")),
            new FakeLauncher(_log),
            _folder);
        return (installer, server);
    }

    [Fact]
    public async Task The_steps_go_download_verify_backup_and_nothing_is_launched()
    {
        var (installer, server) = Build();
        var steps = new List<UpdateStep>();

        var path = await installer.PrepareAsync(Offer(server), new SyncProgress(p =>
        {
            if (steps.Count == 0 || steps[^1] != p.Step)
                steps.Add(p.Step);
        }), CancellationToken.None);

        Assert.Equal([UpdateStep.Downloading, UpdateStep.Verifying, UpdateStep.BackingUp], steps);
        Assert.Equal(["backup 1.0.1"], _log);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task Launching_gives_the_installer_its_silent_arguments()
    {
        var (installer, server) = Build();
        var path = await installer.PrepareAsync(Offer(server), null, CancellationToken.None);

        installer.Launch(path);

        Assert.Equal("launch EcclesiaCast-1.0.1-setup.exe " + UpdateInstaller.InstallerArguments, _log.Last());
        Assert.Contains("/SILENT", UpdateInstaller.InstallerArguments);
        Assert.Contains("/SP-", UpdateInstaller.InstallerArguments);
        Assert.Contains("/NOCANCEL", UpdateInstaller.InstallerArguments);
        Assert.Contains("/RELANZAR", UpdateInstaller.InstallerArguments);
    }

    [Fact]
    public async Task A_wrong_size_deletes_the_file_and_stops_before_the_backup()
    {
        var (installer, server) = Build();

        var ex = await Assert.ThrowsAsync<UpdateException>(
            () => installer.PrepareAsync(Offer(server, size: Content.Length + 1), null, CancellationToken.None));

        Assert.Equal(UpdateFailure.Invalid, ex.Failure);
        Assert.Equal("La actualización descargada no es válida; no se instaló nada.", ex.Message);
        Assert.Empty(Directory.GetFiles(_folder));
        Assert.Empty(_log);
    }

    [Fact]
    public async Task A_wrong_sha256_deletes_the_file_and_stops_before_the_backup()
    {
        var (installer, server) = Build();

        var ex = await Assert.ThrowsAsync<UpdateException>(
            () => installer.PrepareAsync(Offer(server, sha: new string('a', 64)), null, CancellationToken.None));

        Assert.Equal(UpdateFailure.Invalid, ex.Failure);
        Assert.Empty(Directory.GetFiles(_folder));
        Assert.Empty(_log);
    }

    [Fact]
    public async Task An_unsigned_program_accepts_an_unsigned_installer()
    {
        var (installer, server) = Build(installed: SignatureInfo.Unsigned, candidate: SignatureInfo.Unsigned);

        await installer.PrepareAsync(Offer(server), null, CancellationToken.None);

        Assert.Equal(["backup 1.0.1"], _log);
    }

    [Fact]
    public async Task A_signed_program_accepts_an_installer_with_the_same_signer()
    {
        var signer = new SignatureInfo("CN=SignPath Foundation, O=SignPath Foundation", true);
        var (installer, server) = Build(installed: signer, candidate: signer with { });

        await installer.PrepareAsync(Offer(server), null, CancellationToken.None);

        Assert.Equal(["backup 1.0.1"], _log);
    }

    [Fact]
    public async Task A_signed_program_refuses_an_unsigned_installer()
    {
        var (installer, server) = Build(
            installed: new SignatureInfo("CN=SignPath Foundation", true), candidate: SignatureInfo.Unsigned);

        var ex = await Assert.ThrowsAsync<UpdateException>(
            () => installer.PrepareAsync(Offer(server), null, CancellationToken.None));

        Assert.Equal(UpdateFailure.Invalid, ex.Failure);
        Assert.Empty(Directory.GetFiles(_folder));
        Assert.Empty(_log);
    }

    [Fact]
    public async Task A_signed_program_refuses_an_installer_signed_by_somebody_else()
    {
        var (installer, server) = Build(
            installed: new SignatureInfo("CN=SignPath Foundation", true),
            candidate: new SignatureInfo("CN=Otro Editor", true));

        var ex = await Assert.ThrowsAsync<UpdateException>(
            () => installer.PrepareAsync(Offer(server), null, CancellationToken.None));

        Assert.Equal(UpdateFailure.Invalid, ex.Failure);
        Assert.Empty(Directory.GetFiles(_folder));
    }

    [Fact]
    public async Task A_signed_program_refuses_an_installer_whose_signature_is_broken()
    {
        var (installer, server) = Build(
            installed: new SignatureInfo("CN=SignPath Foundation", true),
            candidate: new SignatureInfo("CN=SignPath Foundation", false));

        await Assert.ThrowsAsync<UpdateException>(
            () => installer.PrepareAsync(Offer(server), null, CancellationToken.None));
    }

    [Fact]
    public async Task A_failed_backup_stops_the_update_and_deletes_the_installer()
    {
        var (installer, server) = Build(backup: _ => throw new IOException("disco lleno"));

        var ex = await Assert.ThrowsAsync<UpdateException>(
            () => installer.PrepareAsync(Offer(server), null, CancellationToken.None));

        Assert.Equal(UpdateFailure.Backup, ex.Failure);
        Assert.Equal("No se pudo hacer la copia de seguridad de la biblioteca; no se instaló nada.", ex.Message);
        Assert.Empty(Directory.GetFiles(_folder));
        Assert.DoesNotContain(_log, line => line.StartsWith("launch"));
    }

    [Fact]
    public async Task A_failed_download_is_reported_as_such()
    {
        var (installer, server) = Build();
        var offer = Offer(server);
        server.Dispose();

        var ex = await Assert.ThrowsAsync<UpdateException>(
            () => installer.PrepareAsync(offer, null, CancellationToken.None));

        Assert.Equal(UpdateFailure.Download, ex.Failure);
        Assert.Empty(_log);
    }

    [Fact]
    public void Clean_up_deletes_what_an_earlier_update_left()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "EcclesiaCast-1.0.0-setup.exe"), "x");

        UpdateInstaller.CleanUp(_folder);

        Assert.False(Directory.Exists(_folder));
    }

    [Fact]
    public void Clean_up_with_nothing_there_does_nothing()
    {
        UpdateInstaller.CleanUp(_folder);

        Assert.False(Directory.Exists(_folder));
    }

    [Fact]
    public void Clean_up_leaves_a_file_that_is_in_use_for_next_time()
    {
        Directory.CreateDirectory(_folder);
        using var held = new FileStream(Path.Combine(_folder, "setup.exe"), FileMode.Create, FileAccess.Write, FileShare.None);

        UpdateInstaller.CleanUp(_folder);

        Assert.True(File.Exists(Path.Combine(_folder, "setup.exe")));
    }

    [Fact]
    public void The_default_folder_is_the_programs_own_inside_the_temporary_one()
    {
        Assert.Equal("EcclesiaCast-actualizacion", Path.GetFileName(UpdateInstaller.DefaultWorkFolder));
        Assert.Equal(Path.GetTempPath().TrimEnd('\\', '/'), Path.GetDirectoryName(UpdateInstaller.DefaultWorkFolder));
    }

    private sealed class SyncProgress(Action<UpdateProgress> report) : IProgress<UpdateProgress>
    {
        public void Report(UpdateProgress value) => report(value);
    }
}
