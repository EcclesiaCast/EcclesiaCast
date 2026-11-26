using System.Text.RegularExpressions;
using System.Xml.Linq;
using EcclesiaCast.Core.Ui;

namespace EcclesiaCast.Core.Tests.Ui;

public class TourRunTests
{
    /// <summary>
    /// Element name → the tool tab that has to be open for it to be on show
    /// (null: it does not live in one), read from the real MainWindow.xaml.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string?> TabOfElement = ReadToolTabs();

    private static readonly string[] ToolTabs =
        ["TabQuick", "TabOverlay", "TabPlates", "TabHighlight", "TabNotes", "TabMusic", "TabTimes"];

    public static TheoryData<string, string> TutorialsFromEveryTab()
    {
        var data = new TheoryData<string, string>();
        foreach (var tutorial in TutorialFile.LoadBuiltIn())
            foreach (var tab in ToolTabs)
                data.Add(tutorial.Id, tab);
        return data;
    }

    /// <summary>
    /// The bug of the prueba.12: with «Música» open, «Durante la reunión» went
    /// from «1 de 3» to «3 de 5», because the steps behind a closed tab were
    /// dropped before their «abrir:» had a chance to open it.
    /// </summary>
    [Theory]
    [MemberData(nameof(TutorialsFromEveryTab))]
    public void A_tutorial_shows_every_step_with_a_fixed_total_and_its_part_on_show(string id, string openTab)
    {
        var steps = TutorialFile.LoadBuiltIn().Single(t => t.Id == id).Steps;
        var run = TourRun.ForTutorial(steps);

        var visited = new List<int>();
        for (var i = run.First; i >= 0; i = run.Next(i, 1))
        {
            visited.Add(i);
            Assert.Equal((visited.Count, steps.Count), run.Count(steps, i));

            // What the window does when the step's turn comes.
            if (steps[i].Open is { } open && open.StartsWith(TourActions.ToolTabPrefix, StringComparison.Ordinal))
                openTab = open[TourActions.ToolTabPrefix.Length..];

            if (steps[i].Target is { } target && TabOfElement.GetValueOrDefault(target) is { } needed)
                Assert.True(needed == openTab, $"{id}, paso {i + 1}: «{target}» está en {needed} y quedó abierta {openTab}.");
        }

        Assert.Equal(Enumerable.Range(0, steps.Count), visited);

        // And back again, one by one.
        for (var i = steps.Count - 1; i > 0; i--)
            Assert.Equal(i - 1, run.Next(i, -1));
        Assert.Equal(-1, run.Next(0, -1));
    }

    /// <summary>
    /// A step must not count on the step before it: going back lands on it with
    /// another tab open.
    /// </summary>
    [Fact]
    public void Every_step_about_one_tool_tab_opens_it_itself()
    {
        var problems = TutorialFile.LoadBuiltIn()
            .SelectMany(t => t.Steps.Select((s, i) => (t.Id, Number: i + 1, Step: s)))
            .Where(x => x.Step.Target is { } target && TabOfElement.GetValueOrDefault(target) is { } tab
                        && x.Step.Open != TourActions.ToolTabPrefix + tab)
            .Select(x => $"{x.Id}, paso {x.Number}: {x.Step.Target}")
            .ToList();

        Assert.Empty(problems);
    }

    [Fact]
    public void The_map_of_tool_tabs_really_reads_the_window()
    {
        Assert.Equal("TabQuick", TabOfElement["QuickTextBox"]);
        Assert.Equal("TabOverlay", TabOfElement["OverlayTextBox"]);
        Assert.Equal("TabTimes", TabOfElement["SectionsList"]);
        Assert.Equal("TabMusic", TabOfElement["YouTubeMusicSearch"]);
        Assert.Null(TabOfElement["ToolsTabs"]);
    }

    [Fact]
    public void The_messages_tutorial_explains_quick_text_and_footer_notice_and_how_they_differ()
    {
        var steps = TutorialFile.LoadBuiltIn().Single(t => t.Id == "10-mensajes-en-pantalla").Steps;
        var opened = steps.Select(s => s.Open).ToList();

        Assert.Contains("herramienta:TabQuick", opened);
        Assert.Contains("herramienta:TabOverlay", opened);
        Assert.Contains("parecido", steps[0].Text);
        Assert.Contains(steps, s => s.Title == "En qué se diferencian");
    }

    private static IReadOnlyDictionary<string, string?> ReadToolTabs()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "EcclesiaCast.App")))
            dir = dir.Parent;
        var path = Path.Combine(dir!.FullName, "src", "EcclesiaCast.App", "MainWindow.xaml");

        var map = new Dictionary<string, string?>();
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        void Walk(XElement element, string? tab)
        {
            var visibility = element.Attribute("Visibility")?.Value ?? string.Empty;
            var match = Regex.Match(visibility, @"IsChecked, ElementName=(Tab[A-Za-z]+)");
            if (match.Success)
                tab = match.Groups[1].Value;

            if (element.Attribute(x + "Name")?.Value is { } name)
                map.TryAdd(name, tab);

            foreach (var child in element.Elements())
                Walk(child, tab);
        }

        Walk(XDocument.Load(path).Root!, null);
        return map;
    }
}
