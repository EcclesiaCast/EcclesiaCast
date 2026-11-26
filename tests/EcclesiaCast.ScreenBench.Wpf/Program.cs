using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace EcclesiaCast.ScreenBench.Wpf;

/// <summary>
/// The screens bench of WPF (batch 14). It draws, without showing a window,
/// the operator's screens with the real looks of App.xaml and leaves them in
/// artifacts/pantallas/wpf/&lt;name&gt;.png. The Avalonia tests
/// (ScreenBenchTests) draw the same screens and put each pair side by side.
/// The menu is read from MainWindow.xaml itself, so the picture is always the
/// real one. A menu opened in WPF is a window of its own (a popup), which
/// cannot be drawn in memory: each one is drawn as a right click menu with
/// the same rows, which has the same look (App.xaml gives both one box).
/// There is no console: the outcome goes to resultado.txt, and the exit code
/// is 0 when everything was drawn.
///
/// Usage: ScreenBench.Wpf [folder of the results]
/// </summary>
public static class Program
{
    private static readonly XNamespace Wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>The attributes that name a handler in MainWindow.xaml.cs: left out.</summary>
    private static readonly string[] Handlers = ["Click", "SubmenuOpened", "Checked", "Unchecked"];

    [STAThread]
    public static int Main(string[] args)
    {
        var report = new StringBuilder();
        var failures = new List<string>();
        var drawn = 0;
        string? outFolder = null;

        try
        {
            var root = FindRoot(AppContext.BaseDirectory)
                ?? throw new DirectoryNotFoundException("EcclesiaCast.slnx was not found above " + AppContext.BaseDirectory);
            outFolder = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
                ? Path.GetFullPath(args[0])
                : Path.Combine(root, "artifacts", "pantallas", "wpf");
            Directory.CreateDirectory(outFolder);
            foreach (var old in Directory.GetFiles(outFolder, "*.png"))
                File.Delete(old);

            // The program's own App: its constructor and App.xaml, never Run().
            var app = new EcclesiaCast.App.App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();

            foreach (var (name, make, width) in Screens(root))
            {
                try
                {
                    Save(Draw(make(), width), Path.Combine(outFolder, name + ".png"));
                    drawn++;
                }
                catch (Exception ex)
                {
                    failures.Add($"{name}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            report.AppendLine($"Pantallas dibujadas: {drawn}");
        }
        catch (Exception ex)
        {
            failures.Add(ex.ToString());
        }

        foreach (var failure in failures)
            report.AppendLine("ERROR " + failure);
        report.Insert(0, failures.Count == 0 ? "OK\n" : "FALLÓ\n");

        try
        {
            outFolder ??= Path.Combine(AppContext.BaseDirectory, "pantallas");
            Directory.CreateDirectory(outFolder);
            File.WriteAllText(Path.Combine(outFolder, "resultado.txt"), report.ToString(), new UTF8Encoding(false));
        }
        catch
        {
            // Nowhere to say it: the exit code still tells.
        }

        return failures.Count == 0 ? 0 : 1;
    }

    /// <summary>
    /// Every screen of the bench: its name (the same in the Avalonia tests),
    /// how to make it and its width (NaN: as wide as it wants). A batch that
    /// translates a window adds it here and in ScreenBenchTests.
    /// </summary>
    private static IEnumerable<(string Name, Func<FrameworkElement> Make, double Width)> Screens(string root)
    {
        var menu = ReadMenu(Path.Combine(root, "src", "EcclesiaCast.App", "MainWindow.xaml"));

        yield return ("menu-barra", () => Parse<Menu>(menu), 900);

        var index = 0;
        foreach (var top in menu.Elements(Wpf + "MenuItem"))
        {
            index++;
            var header = ((string?)top.Attribute("Header") ?? $"menu{index}").Replace("_", "");
            var rows = new XElement(Wpf + "ContextMenu", top.Elements().Select(e => new XElement(e)));
            yield return ($"menu-{index:00}-{Slug(header)}", () => Parse<ContextMenu>(rows), double.NaN);
        }

        yield return ("muestrario", () => new Muestrario(), double.NaN);
        yield return ("ayuda-al-pasar", () => new ToolTip { Content = TipSample }, double.NaN);

        foreach (var dialog in DialogScreens.All())
            yield return dialog;
    }

    /// <summary>A long tooltip of the menu: it has to wrap at 400.</summary>
    public const string TipSample =
        "Para computadoras viejas o sin placa de video: las transiciones cortan, el panel LIVE muestra la imagen fija del video y las vistas previas se dibujan sin sombra ni contorno. Lo que ve la congregación conserva su diseño.";

    /// <summary>The Menu of MainWindow.xaml, without the handlers of the window's code.</summary>
    private static XElement ReadMenu(string path)
    {
        var menu = XDocument.Load(path).Descendants(Wpf + "Menu")
            .First(e => (string?)e.Attribute(X + "Name") == "MainMenu");
        var copy = new XElement(menu);
        foreach (var attribute in copy.DescendantsAndSelf().Attributes().ToList())
        {
            if (attribute.Name.Namespace == XNamespace.None && Handlers.Contains(attribute.Name.LocalName))
                attribute.Remove();
            if (attribute.Name == X + "Name" || attribute.Name.LocalName == "Grid.Row")
                attribute.Remove();
        }
        return copy;
    }

    private static T Parse<T>(XElement element) where T : FrameworkElement
    {
        var copy = new XElement(element);
        copy.SetAttributeValue(XNamespace.Xmlns + "x", X.NamespaceName);
        return (T)XamlReader.Parse(copy.ToString());
    }

    private static string Slug(string text)
    {
        var plain = text.Normalize(NormalizationForm.FormD)
            .Where(c => char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
        return new string(plain.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-').ToArray());
    }

    // ── Drawing ──────────────────────────────────────────────────

    /// <summary>
    /// The element on the window's background (MainWindow: BgBrush, Segoe UI),
    /// laid out at its own size, drawn at 96 dpi. A menu box or a tooltip
    /// lives in a popup and takes no parent: it is drawn on its own, as it
    /// floats over the window.
    /// </summary>
    private static BitmapSource Draw(FrameworkElement element, double width)
    {
        if (element is ContextMenu or ToolTip)
            return Render(element);

        var host = new Border
        {
            Background = (Brush)Application.Current.Resources["BgBrush"],
            Child = element,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        TextElement.SetFontFamily(host, new FontFamily("Segoe UI"));
        TextElement.SetFontSize(host, 12);
        if (!double.IsNaN(width))
            host.Width = width;
        return Render(host);
    }

    private static BitmapSource Render(FrameworkElement host)
    {
        host.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        host.Arrange(new Rect(host.DesiredSize));
        host.UpdateLayout();

        var picture = new RenderTargetBitmap(
            (int)Math.Ceiling(host.ActualWidth), (int)Math.Ceiling(host.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        picture.Render(host);
        picture.Freeze();
        return picture;
    }

    private static void Save(BitmapSource picture, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(picture));
        using var file = File.Create(path);
        encoder.Save(file);
    }

    private static string? FindRoot(string start)
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "EcclesiaCast.slnx")))
                return dir.FullName;
        }
        return null;
    }
}
