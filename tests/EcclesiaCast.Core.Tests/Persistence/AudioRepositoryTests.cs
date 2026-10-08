using EcclesiaCast.Core.Audio;
using EcclesiaCast.Data.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EcclesiaCast.Core.Tests.Persistence;

public class AudioRepositoryTests
{
    private static void WithDatabase(Action<string> test)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ecclesiacast-test-{Guid.NewGuid():N}.db");
        try
        {
            test(path);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (var file in new[] { path, path + "-wal", path + "-shm" })
            {
                try { File.Delete(file); } catch (IOException) { }
            }
        }
    }

    private static AudioRepository Fresh(string path)
    {
        using (var db = new AppDbContext(path))
            db.Database.Migrate();
        return new AudioRepository(path);
    }

    [Fact]
    public void Tracks_come_back_in_the_order_they_were_added()
    {
        WithDatabase(path =>
        {
            var repository = Fresh(path);
            repository.Add(new AudioTrack { Name = "Uno", Path = @"C:\a\uno.mp3" });
            repository.Add(new AudioTrack { Name = "Dos", Path = @"C:\a\dos.mp3" });
            repository.Add(new AudioTrack { Name = "Tres", Path = @"C:\a\tres.mp3" });

            Assert.Equal(["Uno", "Dos", "Tres"], repository.GetAll().Select(t => t.Name));
        });
    }

    [Fact]
    public void The_order_the_operator_chose_is_kept()
    {
        WithDatabase(path =>
        {
            var repository = Fresh(path);
            var one = repository.Add(new AudioTrack { Name = "Uno", Path = @"C:\a\uno.mp3" });
            var two = repository.Add(new AudioTrack { Name = "Dos", Path = @"C:\a\dos.mp3" });
            var three = repository.Add(new AudioTrack { Name = "Tres", Path = @"C:\a\tres.mp3" });

            repository.SaveOrder([three.Id, one.Id, two.Id]);

            Assert.Equal(["Tres", "Uno", "Dos"], repository.GetAll().Select(t => t.Name));
        });
    }

    [Fact]
    public void A_track_added_after_a_reorder_goes_to_the_end()
    {
        WithDatabase(path =>
        {
            var repository = Fresh(path);
            var one = repository.Add(new AudioTrack { Name = "Uno", Path = @"C:\a\uno.mp3" });
            var two = repository.Add(new AudioTrack { Name = "Dos", Path = @"C:\a\dos.mp3" });
            repository.SaveOrder([two.Id, one.Id]);

            repository.Add(new AudioTrack { Name = "Tres", Path = @"C:\a\tres.mp3" });

            Assert.Equal(["Dos", "Uno", "Tres"], repository.GetAll().Select(t => t.Name));
        });
    }

    [Fact]
    public void A_deleted_track_is_gone_and_the_rest_stay()
    {
        WithDatabase(path =>
        {
            var repository = Fresh(path);
            repository.Add(new AudioTrack { Name = "Uno", Path = @"C:\a\uno.mp3" });
            var two = repository.Add(new AudioTrack { Name = "Dos", Path = @"C:\a\dos.mp3" });

            repository.Delete(two.Id);

            Assert.Equal(["Uno"], repository.GetAll().Select(t => t.Name));
        });
    }

    [Fact]
    public void Deleting_a_track_that_does_not_exist_is_harmless()
    {
        WithDatabase(path =>
        {
            var repository = Fresh(path);
            repository.Delete(999);

            Assert.Empty(repository.GetAll());
        });
    }

    [Fact]
    public void A_library_from_before_the_audio_list_gets_the_table_and_loses_nothing()
    {
        WithDatabase(path =>
        {
            using (var old = new AppDbContext(path))
            {
                old.GetService<IMigrator>().Migrate("20261008065508_AddAfterMedia");
                old.Database.ExecuteSqlRaw(
                    "INSERT INTO MediaItems (Name, Path, Type, Category) VALUES ('Ofrendas', 'C:\\a.png', 0, 'Anuncios')");
            }

            using (var upgraded = new AppDbContext(path))
                upgraded.Database.Migrate();

            var media = Assert.Single(new MediaRepository(path).GetAll());
            Assert.Equal("Ofrendas", media.Name);

            var audio = new AudioRepository(path);
            Assert.Empty(audio.GetAll());
            audio.Add(new AudioTrack { Name = "Uno", Path = @"C:\a\uno.mp3" });
            Assert.Single(audio.GetAll());
        });
    }
}
