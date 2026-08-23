using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Logos;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Data.Persistence;

public sealed class LogoRepository(string dbPath) : ILogoRepository
{
    public IReadOnlyList<Logo> GetAll()
    {
        using var db = new AppDbContext(dbPath);
        return db.Logos.AsNoTracking().OrderBy(l => l.Order).ThenBy(l => l.Id).ToList();
    }

    public Logo? Get(int id)
    {
        using var db = new AppDbContext(dbPath);
        return db.Logos.AsNoTracking().FirstOrDefault(l => l.Id == id);
    }

    public Logo Save(Logo logo)
    {
        using var db = new AppDbContext(dbPath);
        if (logo.Id == 0)
            db.Logos.Add(logo);
        else
            db.Logos.Update(logo);
        db.SaveChanges();
        return logo;
    }

    public void Delete(int id)
    {
        using var db = new AppDbContext(dbPath);
        var logo = db.Logos.Find(id);
        if (logo is null)
            return;
        db.Logos.Remove(logo);
        db.SaveChanges();
    }
}
