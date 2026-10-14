using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>What became of a song that was handed over to be saved.</summary>
public enum SongSaveResult
{
    /// <summary>A new song in the library.</summary>
    Saved,

    /// <summary>The title was taken: it went in under «Title (1)».</summary>
    SavedAsCopy,

    /// <summary>The title was taken: the song that was there now has the new lyrics.</summary>
    Replaced,

    /// <summary>The title was taken: the new song was thrown away.</summary>
    Discarded,
}

public sealed record SongSaveOutcome(SongSaveResult Result, Song? Song);

/// <summary>
/// Saves new songs (written from scratch, imported from a file, from
/// ProPresenter or from the internet) without piling up two with the same
/// title: when the title is taken it asks whether to replace the one that is
/// there, keep both or throw the new one away. Editing a song that is
/// already in the library does not come through here.
/// </summary>
public sealed class SongSaver(ISongRepository songs, ITextPrompt prompt)
{
    /// <summary>
    /// Starts a run of saves that share one memory of the titles in the
    /// library. <paramref name="several"/> adds the «same for the rest» box.
    /// </summary>
    public Session Begin(bool several = false) => new(songs, prompt, several);

    public sealed class Session
    {
        private readonly ISongRepository _songs;
        private readonly ITextPrompt _prompt;
        private readonly bool _several;
        private readonly List<SongTitle> _library;
        private int? _remembered;

        internal Session(ISongRepository songs, ITextPrompt prompt, bool several)
        {
            _songs = songs;
            _prompt = prompt;
            _several = several;
            // Asked once: asking the database for every song of a 300-song import is 300 round trips.
            _library = [.. songs.GetTitles()];
        }

        public SongSaveOutcome Save(Song song)
        {
            var taken = SongTitles.FindSame(_library, song.Title, song.Artist);
            if (taken is null)
                return new SongSaveOutcome(SongSaveResult.Saved, Add(song));

            var copyTitle = SongTitles.FreeTitle(_library.Select(s => s.Title), song.Title);
            var choice = _remembered ?? Ask(song.Title, taken.Artist, copyTitle);

            switch (choice)
            {
                case 0:
                    song.Title = copyTitle;
                    return new SongSaveOutcome(SongSaveResult.SavedAsCopy, Add(song));

                case 1:
                    return new SongSaveOutcome(SongSaveResult.Replaced, Replace(taken, song));

                default:
                    Log.Information("Canción repetida descartada: {Title}", song.Title);
                    return new SongSaveOutcome(SongSaveResult.Discarded, null);
            }
        }

        /// <summary>0 = keep both, 1 = replace, 2 = discard; closing the window discards.</summary>
        private int Ask(string title, string artist, string copyTitle)
        {
            string[] options =
            [
                $"Guardar como «{copyTitle}» (las dos quedan)",
                "Reemplazar la que ya está (se pierde su letra)",
                "Descartar la nueva",
            ];
            var by = string.IsNullOrWhiteSpace(artist) ? "" : $" de {artist}";
            var message = $"Ya hay una canción llamada «{title}»{by} en la biblioteca. ¿Qué hago con la nueva?";

            var (index, repeat) = _several
                ? _prompt.ChooseAndRemember("Canción repetida", message, options, "Hacer lo mismo con las demás")
                : (_prompt.Choose("Canción repetida", message, options), false);

            var chosen = index < 0 ? 2 : index;
            if (repeat && index >= 0)
                _remembered = chosen;
            return chosen;
        }

        private Song Add(Song song)
        {
            var saved = _songs.Save(song);
            _library.Add(new SongTitle(saved.Id, saved.Title, saved.Artist));
            return saved;
        }

        private Song Replace(SongTitle taken, Song song)
        {
            // The lyrics are replaced; the design the church gave that song
            // (its theme) stays unless the new one brings its own.
            var existing = _songs.Get(taken.Id);
            song.Id = taken.Id;
            song.Title = taken.Title;
            song.ThemeId ??= existing?.ThemeId;

            var saved = _songs.Save(song);
            _library.RemoveAll(s => s.Id == taken.Id);
            _library.Add(new SongTitle(saved.Id, saved.Title, saved.Artist));
            return saved;
        }
    }
}
