using EcclesiaCast.Core.Media;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Core.Tests.Persistence;

public class MediaExpiryPersistenceTests
{
    [Fact]
    public void The_event_date_and_its_days_come_back_as_they_were_saved()
    {
        using var database = new TempDatabase();
        var repository = new MediaRepository(database.Path);
        repository.Add(new MediaItem
        {
            Name = "Overflow",
            Path = "C:/x/overflow.jpg",
            Category = "Anuncios",
            EventDate = new DateTime(2026, 10, 17),
            ArchiveAfterDays = 7,
        });

        var back = Assert.Single(repository.GetAll());

        Assert.Equal(new DateTime(2026, 10, 17), back.EventDate);
        Assert.Equal(7, back.ArchiveAfterDays);
    }

    [Fact]
    public void A_poster_saved_without_a_date_has_none_and_the_usual_ten_days()
    {
        using var database = new TempDatabase();
        var repository = new MediaRepository(database.Path);
        repository.Add(new MediaItem { Name = "Fondo", Path = "C:/x/fondo.jpg", Category = "Fondos" });

        var back = Assert.Single(repository.GetAll());

        Assert.Null(back.EventDate);
        Assert.Equal(MediaExpiry.DefaultDays, back.ArchiveAfterDays);
    }

    [Fact]
    public void Clearing_the_date_sticks()
    {
        using var database = new TempDatabase();
        var repository = new MediaRepository(database.Path);
        var item = repository.Add(new MediaItem
        {
            Name = "Overflow", Path = "C:/x/o.jpg", Category = "Anuncios", EventDate = new DateTime(2026, 10, 17),
        });

        item.EventDate = null;
        item.Category = MediaExpiry.ArchiveTab;
        repository.Update(item);

        var back = Assert.Single(repository.GetAll());
        Assert.Null(back.EventDate);
        Assert.Equal("Eventos pasados", back.Category);
    }
}
