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
    }
}
