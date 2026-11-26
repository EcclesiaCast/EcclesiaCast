namespace EcclesiaCast.Core.Ui;

/// <summary>
/// What a tutorial step can open before its card shows («abrir:» in the text
/// file), so the step shows the screen it talks about instead of only naming
/// it. The window does the opening; this class only says which names exist, so
/// a file that asks for something unknown is turned down when it is read.
/// </summary>
public static class TourActions
{
    /// <summary>«menu:MenuBroadcast»: a menu of the bar at the top, open.</summary>
    public const string MenuPrefix = "menu:";

    /// <summary>«ventana:editor»: a real window of the program, open beside the card.</summary>
    public const string WindowPrefix = "ventana:";

    /// <summary>«herramienta:TabPlates»: a tab of the text tools (bottom right).</summary>
    public const string ToolTabPrefix = "herramienta:";

    /// <summary>The ➕ Agregar menu of the Canciones tab.</summary>
    public const string AddMenu = "agregar";

    /// <summary>The 🕘 Historial of the Biblia tab.</summary>
    public const string BibleHistory = "historial";

    /// <summary>The Biblia tab's books as a list, or as a grid of colored tiles.</summary>
    public const string BibleList = "vista:lista";
    public const string BibleGrid = "vista:grilla";

    /// <summary>«pestana:canciones» / «pestana:biblia»: the tab of the library the step needs shown first.</summary>
    public const string SongsTab = "pestana:canciones";
    public const string BibleTab = "pestana:biblia";

    public const string SongEditor = "editor";
    public const string SongSearch = "buscar";
    public const string SongImport = "importar";
    public const string SongImportHolyrics = "importar-holyrics";
    public const string Broadcast = "obs";
    public const string Remote = "celular";

    public static IReadOnlyList<string> Windows { get; } = [SongEditor, SongSearch, SongImport, SongImportHolyrics, Broadcast, Remote];

    /// <summary>The drawings a card can carry («dibujo:»): how the phone looks.</summary>
    public static IReadOnlyList<string> Pictures { get; } = ["celular-pin", "celular-control"];

    public static IReadOnlyList<string> Fixed { get; } = [AddMenu, BibleHistory, BibleList, BibleGrid, SongsTab, BibleTab];

    /// <summary>The names that point at an element of the main window (menu, tool tab).</summary>
    public static bool TryGetElementName(string action, out string name)
    {
        foreach (var prefix in new[] { MenuPrefix, ToolTabPrefix })
        {
            if (action.StartsWith(prefix, StringComparison.Ordinal) && action.Length > prefix.Length)
            {
                name = action[prefix.Length..];
                return true;
            }
        }

        name = string.Empty;
        return false;
    }

    public static bool TryGetWindow(string action, out string window)
    {
        window = action.StartsWith(WindowPrefix, StringComparison.Ordinal) ? action[WindowPrefix.Length..] : string.Empty;
        return Windows.Contains(window);
    }

    public static bool IsKnown(string action) =>
        Fixed.Contains(action) || TryGetWindow(action, out _) || TryGetElementName(action, out _);
}
