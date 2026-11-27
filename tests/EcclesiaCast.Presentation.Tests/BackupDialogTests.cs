using EcclesiaCast.Data.Persistence;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Presentation.Tests;

/// <summary>Always with a library made in a temporary folder.</summary>
[Collection("ViewModels")]
public sealed class BackupDialogTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ecclesiacast-copias-" + Guid.NewGuid().ToString("N"));
    private readonly string _db;
    private readonly ScriptedNotices _notices = new();
    private readonly List<string> _done = [];

    public BackupDialogTests()
    {
        Directory.CreateDirectory(_root);
        _db = Path.Combine(_root, "ecclesiacast.db");
        using var db = new AppDbContext(_db);
        db.Database.Migrate();
    }

    public void Dispose()
    {
        _notices.Dispose();
        SqliteConnection.ClearAllPools();
        Directory.Delete(_root, recursive: true);
    }

    private sealed class Chooses(string? save = null, string? open = null) : IDialogs
    {
        public SaveFileRequest? Saved { get; private set; }

        public OpenFileRequest? Opened { get; private set; }

        public string? SaveFile(SaveFileRequest request)
        {
            Saved = request;
            return save;
        }

        public string? OpenFile(OpenFileRequest request)
        {
            Opened = request;
            return open;
        }

        public IReadOnlyList<string>? OpenFiles(OpenFileRequest request) => null;

        public string? PickFolder(string title) => null;
    }

    private BackupDialog Create(IDialogs dialogs, Action? relaunch = null) => new(
        _db, dialogs,
        relaunch ?? (() => _done.Add("relaunch")),
        () => _done.Add("quiet"),
        () => _done.Add("shutdown"));

    private string MadeCopy()
    {
        var copy = Path.Combine(_root, "copia.ecbackup");
        DatabaseBackup.Create(_db, copy);
        return copy;
    }

    [Fact]
    public void Saving_writes_the_copy_and_says_where()
    {
        var path = Path.Combine(_root, "guardada.ecbackup");
        var dialogs = new Chooses(save: path);

        Create(dialogs).SaveBackup();

        Assert.True(File.Exists(path));
        Assert.Equal("Guardar una copia de la biblioteca", dialogs.Saved!.Title);
        Assert.Equal(".ecbackup", dialogs.Saved.DefaultExtension);
        Assert.EndsWith(".ecbackup", dialogs.Saved.FileName);
        Assert.Equal(["Copia de EcclesiaCast (*.ecbackup)", "Todos los archivos"], dialogs.Saved.Filters.Select(f => f.Name));
        var notice = Assert.Single(_notices.Asked);
        Assert.Equal("Copia guardada", notice.Title);
        Assert.Contains(path, notice.Message);
    }

    [Fact]
    public void Cancelling_either_dialog_does_nothing()
    {
        var backup = Create(new Chooses());

        backup.SaveBackup();
        backup.RestoreBackup();

        Assert.Empty(_notices.Asked);
        Assert.Empty(_done);
    }

    [Fact]
    public void A_file_that_is_not_a_copy_is_refused()
    {
        var other = Path.Combine(_root, "otra.ecbackup");
        File.WriteAllText(other, "no es una copia");

        Create(new Chooses(open: other)).RestoreBackup();

        Assert.Equal("Ese archivo no es una copia de EcclesiaCast", Assert.Single(_notices.Asked).Title);
        Assert.Empty(_done);
    }

    [Fact]
    public void Saying_not_now_leaves_everything_as_it_was()
    {
        _notices.No();

        Create(new Chooses(open: MadeCopy())).RestoreBackup();

        var question = Assert.Single(_notices.Asked);
        Assert.Equal("Restaurar la copia", question.Title);
        Assert.Equal(["Ahora no", "Restaurar"], question.Buttons);
        Assert.Empty(_done);
        Assert.False(DatabaseBackup.ApplyPendingRestore(_db).WasStaged);
    }

    [Fact]
    public void Restoring_leaves_it_waiting_and_goes_around_again()
    {
        _notices.Yes();

        Create(new Chooses(open: MadeCopy())).RestoreBackup();

        Assert.Equal(["relaunch", "quiet", "shutdown"], _done);
        // The program swaps it in as it starts, when nothing holds the library open yet.
        SqliteConnection.ClearAllPools();
        Assert.True(DatabaseBackup.ApplyPendingRestore(_db).Restored);
    }

    [Fact]
    public void When_it_cannot_be_opened_again_it_says_so_and_closes_anyway()
    {
        _notices.Yes();

        Create(new Chooses(open: MadeCopy()), () => throw new InvalidOperationException("no")).RestoreBackup();

        Assert.Equal("La copia quedó lista, pero no pude volver a abrir EcclesiaCast solo", _notices.Asked[^1].Title);
        Assert.Equal(["quiet", "shutdown"], _done);
    }
}
