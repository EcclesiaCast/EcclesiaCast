using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Search;
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

    public void UpdateVersion(int versionId, string name, string abbreviation)
    {
        using var db = new AppDbContext(dbPath);
        var version = db.BibleVersions.Find(versionId);
        if (version is null)
            return;

        if (version.Abbreviation != abbreviation)
        {
            var oldSuffix = $"({version.Abbreviation})";
            foreach (var item in db.PlaylistItems.Where(i => i.BibleVersionId == versionId))
            {
                if (item.Caption.EndsWith(oldSuffix, StringComparison.Ordinal))
                    item.Caption = item.Caption[..^oldSuffix.Length] + $"({abbreviation})";
            }
        }

        version.Name = name;
        version.Abbreviation = abbreviation;
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

    public ParsedBible ReadVersion(int versionId)
    {
        using var db = new AppDbContext(dbPath);

        var books = db.BibleBooks.AsNoTracking()
            .Where(b => b.VersionId == versionId)
            .OrderBy(b => b.Number)
            .Select(b => new { b.Id, b.Number, b.Name })
            .ToList();
        var verses = db.BibleVerses.AsNoTracking()
            .Where(v => v.Book.VersionId == versionId)
            .OrderBy(v => v.Chapter).ThenBy(v => v.Verse)
            .Select(v => new { v.BookId, v.Chapter, v.Verse, v.Text })
            .ToList()
            .ToLookup(v => v.BookId);

        return new ParsedBible(books
            .Select(b => new ParsedBook(
                b.Number,
                b.Name,
                verses[b.Id].Select(v => new ParsedVerse(v.Chapter, v.Verse, v.Text)).ToList()))
            .ToList());
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

    public int UpdateVerseText(int versionId, int bookNumber, int chapter, int firstVerse, int lastVerse, string text)
    {
        using var db = new AppDbContext(dbPath);

        var verses = db.BibleVerses
            .Where(v => v.Book.VersionId == versionId && v.Book.Number == bookNumber
                && v.Chapter == chapter && v.Verse >= firstVerse && v.Verse <= lastVerse)
            .ToList();
        foreach (var verse in verses)
            verse.Text = text;

        db.SaveChanges();
        return verses.Count;
    }

    public IReadOnlyList<BibleVerseResult> SearchText(int versionId, string query, int limit = 60)
    {
        using var db = new AppDbContext(dbPath);
        if (TextMatching.ContainsPattern(query) is not { } pattern)
            return [];

        // Some files keep the line breaks of the printed page; flatten them
        // the way the query was flattened so a phrase still matches across them.
        return db.BibleVerses.AsNoTracking()
            .Where(v => v.Book.VersionId == versionId && EF.Functions.Like(
                v.Text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ")
                    .Replace("  ", " ").Replace("  ", " ").Replace("  ", " "),
                pattern))
            .OrderBy(v => v.Book.Number).ThenBy(v => v.Chapter).ThenBy(v => v.Verse)
            .Select(v => new BibleVerseResult(v.Book.Number, v.Book.Name, v.Chapter, v.Verse, v.Text))
            .AsEnumerable()
            // LIKE also matches inside a word («echa» in «derecha»); the rows
            // are read one by one until enough start at a word.
            .Where(v => TextMatching.StartsAtWord(v.Text, query))
            .Take(limit)
            .ToList();
    }
}
