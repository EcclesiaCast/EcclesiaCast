using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace EcclesiaCast.Desktop.Controls;

/// <summary>
/// What a row of the menu asks of the window, where WPF's MainWindow handles
/// a Click in its own code. The window that holds the menu does it.
/// </summary>
public enum MenuRequest
{
    Settings,
    Exit,
    Pixabay,
    ViewMenuOpening,
    LibraryRight,
    PlaylistTop,
    MediaTop,
    SplitOutput,
    LiveBottom,
    LibraryCollapsed,
    PlaylistCollapsed,
    MediaCollapsed,
    ToolsCollapsed,
    AudioListCollapsed,
    LeftRail,
    RestartSound,
    LearningMode,
    AdvancedMode,
    ResetLayout,
    Transitions,
    Listening,
    HelpMenuOpening,
    WelcomeTour,
    Guide,
    Donate,
    Feedback,
    About,
}

/// <summary>
/// The operator's menu (MainMenu.axaml). It decides nothing: the commands
/// are MainViewModel's and the rest goes to the window through
/// <see cref="Requested"/>.
/// </summary>
public sealed partial class MainMenu : UserControl
{
    private readonly Dictionary<MenuRequest, MenuItem> _rows = new();

    public MainMenu()
    {
        InitializeComponent();

        foreach (var row in this.GetLogicalDescendants().OfType<MenuItem>())
        {
            if (row.Tag is MenuRequest request)
            {
                _rows[request] = row;
                row.Click += OnRowClick;
            }
        }

        ViewMenu.SubmenuOpened += (_, e) =>
        {
            if (ReferenceEquals(e.Source, ViewMenu))
                Requested?.Invoke(this, MenuRequest.ViewMenuOpening);
        };
        HelpMenu.SubmenuOpened += (_, e) =>
        {
            if (ReferenceEquals(e.Source, HelpMenu))
                Requested?.Invoke(this, MenuRequest.HelpMenuOpening);
        };
    }

    /// <summary>A row was chosen that the window has to carry out.</summary>
    public event EventHandler<MenuRequest>? Requested;

    /// <summary>The bar itself, for the window's shortcuts and the tour.</summary>
    public Menu MenuBar => Bar;

    /// <summary>Ayuda → Tutoriales: the window fills it with the tutorials (batch 21).</summary>
    public MenuItem Tutorials => TutorialsMenu;

    /// <summary>Ayuda → Colaborar: hidden until there is a page to send to.</summary>
    public MenuItem Donate => DonateMenuItem;

    /// <summary>Ticks or unticks a row of Ver (the window knows the layout).</summary>
    public void SetChecked(MenuRequest row, bool isChecked)
    {
        if (_rows.TryGetValue(row, out var item))
            item.IsChecked = isChecked;
    }

    /// <summary>The row of a request, for the tests and the window.</summary>
    public MenuItem? Row(MenuRequest row) => _rows.GetValueOrDefault(row);

    private void OnRowClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: MenuRequest request })
            Requested?.Invoke(this, request);
    }
}
