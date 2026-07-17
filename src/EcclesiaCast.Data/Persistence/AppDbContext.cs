using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Songs;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Data.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly string _dbPath;

    public AppDbContext(string dbPath) => _dbPath = dbPath;

    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<Song> Songs => Set<Song>();
    public DbSet<SongSection> SongSections => Set<SongSection>();
    public DbSet<BibleVersion> BibleVersions => Set<BibleVersion>();
    public DbSet<BibleBook> BibleBooks => Set<BibleBook>();
    public DbSet<BibleVerse> BibleVerses => Set<BibleVerse>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={_dbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Setting>().HasKey(s => s.Key);

        modelBuilder.Entity<Song>(song =>
        {
            song.HasKey(s => s.Id);
            song.Property(s => s.Title).IsRequired();
            song.HasIndex(s => s.Title);
            song.HasMany(s => s.Sections)
                .WithOne()
                .HasForeignKey(x => x.SongId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SongSection>(section =>
        {
            section.HasKey(x => x.Id);
            section.Property(x => x.Text).IsRequired();
        });

        modelBuilder.Entity<BibleVersion>(version =>
        {
            version.HasKey(v => v.Id);
            version.Property(v => v.Name).IsRequired();
            version.HasMany(v => v.Books)
                .WithOne()
                .HasForeignKey(b => b.VersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BibleBook>(book =>
        {
            book.HasKey(b => b.Id);
            book.HasIndex(b => new { b.VersionId, b.Number }).IsUnique();
            book.HasMany(b => b.Verses)
                .WithOne(v => v.Book)
                .HasForeignKey(v => v.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BibleVerse>(verse =>
        {
            verse.HasKey(v => v.Id);
            verse.Property(v => v.Text).IsRequired();
            verse.HasIndex(v => new { v.BookId, v.Chapter, v.Verse });
        });
    }
}
