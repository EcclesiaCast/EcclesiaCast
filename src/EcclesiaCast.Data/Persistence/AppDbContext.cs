using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Data.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly string _dbPath;

    public AppDbContext(string dbPath) => _dbPath = dbPath;

    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={_dbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Setting>().HasKey(s => s.Key);
}
