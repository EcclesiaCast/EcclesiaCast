using Microsoft.EntityFrameworkCore.Design;

namespace EcclesiaCast.Data.Persistence;

/// <summary>Lets `dotnet ef` create the context when generating migrations.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) => new("design-time.db");
}
