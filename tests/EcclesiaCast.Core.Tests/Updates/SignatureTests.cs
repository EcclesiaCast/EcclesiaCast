using EcclesiaCast.Core.Updates;

namespace EcclesiaCast.Core.Tests.Updates;

public class SignatureTests
{
    private static readonly WindowsSignatureReader Reader = new();

    private sealed class FakeReader(Dictionary<string, SignatureInfo> files) : ISignatureReader
    {
        public SignatureInfo Read(string path) => files.GetValueOrDefault(path, SignatureInfo.Unsigned);
    }

    /// <summary>A program signed by Microsoft that is on any machine where these tests run: the dotnet host.</summary>
    private static string? SignedSystemProgram()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? string.Empty, "dotnet.exe"),
            @"C:\Program Files\dotnet\dotnet.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "notepad.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
        };
        return candidates.FirstOrDefault(candidate => OperatingSystem.IsWindows() && File.Exists(candidate)
            && Reader.Read(candidate).IsSigned);
    }

    [Fact]
    public void A_program_signed_by_microsoft_gives_its_signer_and_is_trusted()
    {
        if (!OperatingSystem.IsWindows() || SignedSystemProgram() is not { } program)
            return;

        var info = Reader.Read(program);

        Assert.True(info.IsSigned);
        Assert.Contains("Microsoft", info.Signer);
        Assert.True(info.IsTrusted);
    }

    [Fact]
    public void An_unsigned_program_has_no_signer()
    {
        if (!OperatingSystem.IsWindows())
            return;

        // This very test library is built here and carries no signature.
        var info = Reader.Read(typeof(SignatureTests).Assembly.Location);

        Assert.False(info.IsSigned);
        Assert.False(info.IsTrusted);
    }

    [Fact]
    public void The_unsigned_application_of_this_repository_has_no_signer()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src", "EcclesiaCast.App")))
            directory = directory.Parent;

        var app = directory is null
            ? null
            : Directory.EnumerateFiles(Path.Combine(directory.FullName, "src", "EcclesiaCast.App", "bin"), "EcclesiaCast.App.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (app is null)
            return; // not built here

        Assert.False(Reader.Read(app).IsSigned);
    }

    [Fact]
    public void A_file_that_is_not_a_program_has_no_signer()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ecclesiacast-sig-{Guid.NewGuid():N}.exe");
        File.WriteAllText(path, "esto no es un ejecutable");
        try
        {
            Assert.False(Reader.Read(path).IsSigned);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_file_that_does_not_exist_has_no_signer()
    {
        Assert.False(Reader.Read(@"C:\no\existe\nada.exe").IsSigned);
    }

    [Fact]
    public void A_signature_copied_onto_another_file_is_not_trusted()
    {
        if (!OperatingSystem.IsWindows() || SignedSystemProgram() is not { } program)
            return;

        // Same bytes, one of them changed after the signing: the signer is
        // still readable but Windows must refuse to trust the file.
        var copy = Path.Combine(Path.GetTempPath(), $"ecclesiacast-sig-{Guid.NewGuid():N}.exe");
        var bytes = File.ReadAllBytes(program);
        bytes[bytes.Length / 2] ^= 0xFF;
        File.WriteAllBytes(copy, bytes);
        try
        {
            var info = Reader.Read(copy);

            Assert.True(info.IsSigned);
            Assert.False(info.IsTrusted);
        }
        finally
        {
            File.Delete(copy);
        }
    }

    [Fact]
    public void An_unsigned_installed_program_accepts_anything()
    {
        Assert.True(SignaturePolicy.Accepts(SignatureInfo.Unsigned, SignatureInfo.Unsigned));
        Assert.True(SignaturePolicy.Accepts(SignatureInfo.Unsigned, new SignatureInfo("CN=Alguien", true)));
    }

    [Fact]
    public void A_signed_installed_program_needs_the_same_signer_intact()
    {
        var installed = new SignatureInfo("CN=SignPath Foundation", true);

        Assert.True(SignaturePolicy.Accepts(installed, new SignatureInfo("CN=SignPath Foundation", true)));
        Assert.True(SignaturePolicy.Accepts(installed, new SignatureInfo("cn=signpath foundation", true)));
        Assert.False(SignaturePolicy.Accepts(installed, new SignatureInfo("CN=SignPath Foundation", false)));
        Assert.False(SignaturePolicy.Accepts(installed, new SignatureInfo("CN=Otro", true)));
        Assert.False(SignaturePolicy.Accepts(installed, SignatureInfo.Unsigned));
    }

    [Fact]
    public void The_installed_signature_is_the_first_file_that_has_one()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"ecclesiacast-sig-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            var app = Path.Combine(folder, "EcclesiaCast.App.exe");
            var uninstaller = Path.Combine(folder, "unins000.exe");
            File.WriteAllText(app, "a");
            File.WriteAllText(uninstaller, "b");
            var reader = new FakeReader(new Dictionary<string, SignatureInfo>
            {
                [uninstaller] = new SignatureInfo("CN=Editor", true),
            });

            var fromUninstaller = SignaturePolicy.OfInstalled(reader, app, uninstaller);

            Assert.Equal("CN=Editor", fromUninstaller.Signer);
            Assert.False(SignaturePolicy.OfInstalled(reader, null, "", Path.Combine(folder, "otro.exe"), app).IsSigned);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
