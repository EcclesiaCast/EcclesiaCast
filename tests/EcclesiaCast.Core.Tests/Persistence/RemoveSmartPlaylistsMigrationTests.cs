using EcclesiaCast.Core.Playlists;
using EcclesiaCast.Data.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EcclesiaCast.Core.Tests.Persistence;

public class RemoveSmartPlaylistsMigrationTests
{
    [Fact]
    public void An_old_database_loses_its_smart_lists_and_keeps_the_hand_made_ones()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ecclesiacast-test-{Guid.NewGuid():N}.db");
        try
        {
            using (var old = new AppDbContext(path))
            {
                // The database as the beta.6 left it: playlists still have a rule.
                old.GetService<IMigrator>().Migrate("20260911002612_AddSmartPlaylistsAndSongDates");
                old.Database.ExecuteSqlRaw("INSERT INTO Playlists (Name, Rule, RuleValue) VALUES ('Domingo', 0, NULL)");
                old.Database.ExecuteSqlRaw("INSERT INTO Playlists (Name, Rule, RuleValue) VALUES ('Hace rato que no cantamos', 2, '90')");
                old.Database.ExecuteSqlRaw(
                    "INSERT INTO PlaylistItems (PlaylistId, \"Order\", Type, Caption) " +
                    "SELECT Id, 0, 0, 'Mi Gozo' FROM Playlists WHERE Name = 'Domingo'");
            }

            using (var upgraded = new AppDbContext(path))
                upgraded.Database.Migrate();

            var playlists = new PlaylistRepository(path).GetAll();
            Assert.Equal(["Domingo"], playlists.Select(p => p.Name));
            Assert.Equal(["Mi Gozo"], playlists[0].Items.Select(i => i.Caption));
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
}
