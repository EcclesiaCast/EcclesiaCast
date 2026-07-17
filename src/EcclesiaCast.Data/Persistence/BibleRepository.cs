using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Data.Persistence;

public sealed class BibleRepository(string dbPath) : IBibleRepository
{
    public IReadOnlyList<BibleVersionInfo> GetVersions()
    {
        using var db = new AppDbContext(dbPath);

        var versions = db.BibleVersions.AsNoTracking().ToList();
        var counts = db.BibleVerses.AsNoTracking()
            .GroupBy(v => v.Book.VersionId)
            .Select(g => new { VersionId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.VersionId, x => x.Count);

        return versions
            .Select(v => new BibleVersionInfo(v.Id, v.Name, v.Abbreviation, v.Language, counts.GetValueOrDefault(v.Id)))
            .OrderBy(v => v.Name)
            .ToList();
    }

    public BibleVersionInfo Import(string name, string abbreviation, string language, ParsedBible parsed)
    {
        using var db = new AppDbContext(dbPath);

        var version = new BibleVersion
        {
            Name = name,
            Abbreviation = abbreviation,
            Language = language,
            Books = parsed.Books
                .Where(b => b.Verses.Count > 0)
                .Select(b => new BibleBook
                {
                    Number = b.Number,
                    Name = b.Name,
                    Verses = b.Verses
                        .Select(v => new BibleVerse { Chapter = v.Chapter, Verse = v.Verse, Text = v.Text })
                        .ToList(),
                })
                .ToList(),
        };

        db.BibleVersions.Add(version);
        db.SaveChanges();

        return new BibleVersionInfo(version.Id, version.Name, version.Abbreviation, version.Language, parsed.VerseCount);
    }

    public void RenameVersion(int versionId, string newName)
    {
        using var db = new AppDbContext(dbPath);
        var version = db.BibleVersions.Find(versionId);
        if (version is null)
            return;
        version.Name = newName;
        db.SaveChanges();
    }

    public void DeleteVersion(int versionId)
    {
        using var db = new AppDbContext(dbPath);
        var version = db.BibleVersions.Find(versionId);
        if (version is null)
            return;
        db.BibleVersions.Remove(version);
        db.SaveChanges();
    }

    public IReadOnlyList<int> GetAvailableBookNumbers(int versionId)
    {
        using var db = new AppDbContext(dbPath);
        return db.BibleBooks.AsNoTracking()
            .Where(b => b.VersionId == versionId)
            .OrderBy(b => b.Number)
            .Select(b => b.Number)
            .ToList();
    }

    public IReadOnlyList<int> GetChapterNumbers(int versionId, int bookNumber)
    {
        using var db = new AppDbContext(dbPath);
        var book = db.BibleBooks.AsNoTracking()
            .FirstOrDefault(b => b.VersionId == versionId && b.Number == bookNumber);
        if (book is null)
            return [];

        return db.BibleVerses.AsNoTracking()
            .Where(v => v.BookId == book.Id)
            .Select(v => v.Chapter)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    public IReadOnlyList<BibleVerseResult> GetPassage(int versionId, BibleReference reference)
    {
        using var db = new AppDbContext(dbPath);

        var book = db.BibleBooks.AsNoTracking()
            .FirstOrDefault(b => b.VersionId == versionId && b.Number == reference.BookNumber);
        if (book is null)
            return [];

        IQueryable<BibleVerse> verses = db.BibleVerses.AsNoTracking()
            .Where(v => v.BookId == book.Id && v.Chapter == reference.Chapter);

        if (reference.VerseStart is int start)
        {
            var end = reference.VerseEnd ?? start;
            verses = verses.Where(v => v.Verse >= start && v.Verse <= end);
        }

        return verses
            .OrderBy(v => v.Verse)
            .Select(v => new BibleVerseResult(reference.BookNumber, book.Name, v.Chapter, v.Verse, v.Text))
            .ToList();
    }

    public IReadOnlyList<BibleVerseResult> SearchText(int versionId, string query, int limit = 60)
    {
        using var db = new AppDbContext(dbPath);
        var pattern = $"%{query.Trim()}%";

        return db.BibleVerses.AsNoTracking()
            .Where(v => v.Book.VersionId == versionId && EF.Functions.Like(v.Text, pattern))
            .OrderBy(v => v.Book.Number).ThenBy(v => v.Chapter).ThenBy(v => v.Verse)
            .Take(limit)
            .Select(v => new BibleVerseResult(v.Book.Number, v.Book.Name, v.Chapter, v.Verse, v.Text))
            .ToList();
    }
}
