using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Turns Windows' Spanish spell checker on for the boxes where lyrics and
/// announcements are written, so missing accents and typos get caught before
/// they reach the screen. Attach it in XAML with
/// <c>services:SpellChecking.Enabled="True"</c>.
///
/// Right-clicking a marked word offers the suggestions, plus adding the word
/// to the church's own dictionary — Bible names and worship vocabulary that no
/// general dictionary knows.
/// </summary>
public static class SpellChecking
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(SpellChecking),
            new PropertyMetadata(false, OnEnabledChanged));

    public static void SetEnabled(DependencyObject element, bool value) =>
        element.SetValue(EnabledProperty, value);

    public static bool GetEnabled(DependencyObject element) =>
        (bool)element.GetValue(EnabledProperty);

    /// <summary>The operator's own word list, next to the database.</summary>
    public static string DictionaryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EcclesiaCast", "diccionario.lex");

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox box)
            return;

        if (e.NewValue is not true)
        {
            box.SpellCheck.IsEnabled = false;
            box.ContextMenuOpening -= OnContextMenuOpening;
            return;
        }

        box.Language = SpanishLanguage();
        box.SpellCheck.IsEnabled = true;
        AttachDictionary(box);

        // WPF's built-in menu only offers corrections; ours also adds words.
        box.ContextMenuOpening += OnContextMenuOpening;
    }

    /// <summary>
    /// The checker follows the control's language. Use the operator's own
    /// Spanish variant when they have one, so "vos"-style spellings and local
    /// vocabulary are judged by the right dictionary.
    /// </summary>
    private static XmlLanguage SpanishLanguage()
    {
        var culture = CultureInfo.CurrentCulture;
        var tag = culture.TwoLetterISOLanguageName == "es" ? culture.IetfLanguageTag : "es-ES";
        try
        {
            return XmlLanguage.GetLanguage(tag);
        }
        catch (InvalidOperationException)
        {
            return XmlLanguage.GetLanguage("es-ES");
        }
    }

    private static void AttachDictionary(TextBox box)
    {
        try
        {
            EnsureDictionary();
            var uri = new Uri(DictionaryPath);
            if (!box.SpellCheck.CustomDictionaries.Contains(uri))
                box.SpellCheck.CustomDictionaries.Add(uri);
        }
        catch (Exception ex)
        {
            // A missing or malformed .lex must not break the editor; the
            // checker simply runs without the extra words.
            Log.Warning(ex, "No se pudo cargar el diccionario propio");
        }
    }

    /// <summary>Creates the word list on first use, seeded with church vocabulary.</summary>
    private static void EnsureDictionary()
    {
        if (File.Exists(DictionaryPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(DictionaryPath)!);
        // A .lex is one word per line; the BOM keeps WPF from guessing wrong.
        File.WriteAllLines(DictionaryPath, SeedWords, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    /// <summary>Adds a word to the church dictionary and re-checks the open boxes.</summary>
    public static void Add(string word, TextBox box)
    {
        word = word.Trim();
        if (word.Length == 0)
            return;

        try
        {
            EnsureDictionary();
            File.AppendAllText(DictionaryPath, word + Environment.NewLine, new UTF8Encoding(false));

            // The dictionary is read when it is attached, so re-attach it for
            // the word to stop being marked right away.
            var uri = new Uri(DictionaryPath);
            box.SpellCheck.CustomDictionaries.Remove(uri);
            box.SpellCheck.CustomDictionaries.Add(uri);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo agregar «{Word}» al diccionario", word);
        }
    }

    // ── Menú del clic derecho ────────────────────────────────────

    private static void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not TextBox box)
            return;

        var index = box.CaretIndex;

        // A right-click puts the caret where it was pressed only after this
        // event, so find the word under the pointer ourselves.
        if (e.CursorLeft >= 0 && e.CursorTop >= 0)
        {
            var position = box.GetCharacterIndexFromPoint(new Point(e.CursorLeft, e.CursorTop), true);
            if (position >= 0)
                index = position;
        }

        var error = box.GetSpellingError(index);
        var menu = new ContextMenu { PlacementTarget = box };

        if (error is not null)
        {
            var suggestions = error.Suggestions.Take(6).ToList();
            foreach (var suggestion in suggestions)
            {
                var item = new MenuItem { Header = suggestion, FontWeight = FontWeights.SemiBold };
                item.Click += (_, _) => error.Correct(suggestion);
                menu.Items.Add(item);
            }

            if (suggestions.Count == 0)
                menu.Items.Add(new MenuItem { Header = "(sin sugerencias)", IsEnabled = false });

            menu.Items.Add(new Separator());

            var word = WordAt(box, index);
            var add = new MenuItem { Header = $"Agregar «{word}» al diccionario" };
            add.Click += (_, _) => Add(word, box);
            menu.Items.Add(add);

            var ignore = new MenuItem { Header = "Ignorar en este texto" };
            ignore.Click += (_, _) => error.IgnoreAll();
            menu.Items.Add(ignore);

            menu.Items.Add(new Separator());
        }

        menu.Items.Add(new MenuItem { Header = "Cortar", Command = ApplicationCommands.Cut });
        menu.Items.Add(new MenuItem { Header = "Copiar", Command = ApplicationCommands.Copy });
        menu.Items.Add(new MenuItem { Header = "Pegar", Command = ApplicationCommands.Paste });

        box.ContextMenu = menu;
    }

    /// <summary>The misspelled word around a position, for the "add" entry.</summary>
    private static string WordAt(TextBox box, int index)
    {
        var start = box.GetSpellingErrorStart(index);
        var length = box.GetSpellingErrorLength(index);
        return start >= 0 && length > 0 && start + length <= box.Text.Length
            ? box.Text.Substring(start, length)
            : string.Empty;
    }

    /// <summary>
    /// Words a general Spanish dictionary flags but a church writes every
    /// week. The operator's own additions land in the same file.
    /// </summary>
    private static readonly string[] SeedWords =
    [
        "Jehová", "Yahvé", "Jesucristo", "Emanuel", "Mesías", "Abba",
        "Getsemaní", "Gólgota", "Betel", "Belén", "Nazaret", "Galilea", "Judea",
        "Sion", "Jerusalén", "Efrata", "Edén", "Sinaí", "Horeb", "Canaán",
        "Génesis", "Éxodo", "Levítico", "Deuteronomio", "Josué", "Jueces",
        "Samuel", "Reyes", "Crónicas", "Esdras", "Nehemías", "Ester",
        "Salmos", "Proverbios", "Eclesiastés", "Cantares", "Isaías", "Jeremías",
        "Lamentaciones", "Ezequiel", "Daniel", "Oseas", "Joel", "Amós",
        "Abdías", "Jonás", "Miqueas", "Nahúm", "Habacuc", "Sofonías",
        "Hageo", "Zacarías", "Malaquías", "Mateo", "Marcos", "Lucas", "Juan",
        "Hechos", "Romanos", "Corintios", "Gálatas", "Efesios", "Filipenses",
        "Colosenses", "Tesalonicenses", "Timoteo", "Tito", "Filemón",
        "Hebreos", "Santiago", "Pedro", "Judas", "Apocalipsis",
        "Abraham", "Isaac", "Jacob", "Moisés", "Aarón", "David", "Salomón",
        "Elías", "Eliseo", "Ezequías", "Josías", "María", "José", "Pablo",
        "Bernabé", "Timoteo", "Esteban", "Felipe", "Matías", "Tomás",
        "aleluya", "amén", "hosanna", "aleluyas",
        "Espíritu", "Trinidad", "Pentecostés", "Cuaresma", "Adviento",
        "Eucaristía", "diácono", "presbítero", "pastoral", "misionero",
        "discipulado", "alabanza", "adoración", "santidad", "redención",
        "expiación", "resurrección", "encarnación", "consagración",
        "arrepentimiento", "intercesión", "unción", "avivamiento",
        "primicias", "diezmo", "ofrenda", "congregación", "hermandad",
    ];
}
