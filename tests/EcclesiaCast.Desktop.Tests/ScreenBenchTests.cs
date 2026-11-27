using System.Text;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EcclesiaCast.Desktop.Controls;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// The screens bench of Avalonia (batch 14): draws in memory the same screens
/// as tests/EcclesiaCast.ScreenBench.Wpf and leaves them in
/// artifacts/pantallas/avalonia/&lt;windows|mac&gt;/&lt;name&gt;.png. When the WPF
/// picture of a screen is there (run the WPF bench first), it also leaves
/// both side by side in artifacts/pantallas/comparacion/&lt;name&gt;.png, to look
/// at them. The names are the same on both sides. Every batch of phase 3 adds
/// its screens here and there (docs/guia-wpf-a-avalonia.md).
/// </summary>
public sealed partial class ScreenBenchTests
{
    /// <summary>The same long tooltip as the WPF bench.</summary>
    private const string TipSample =
        "Para computadoras viejas o sin placa de video: las transiciones cortan, el panel LIVE muestra la imagen fija del video y las vistas previas se dibujan sin sombra ni contorno. Lo que ve la congregación conserva su diseño.";

    [AvaloniaFact]
    public void The_menu_bar_is_drawn()
    {
        var picture = Draw(new MainMenu(), "menu-barra", width: 900);
        Assert.Equal(900, picture.Width);
        Assert.InRange(picture.Height, 20, 40);
    }

    [AvaloniaFact]
    public void Every_menu_is_drawn_open()
    {
        var menu = new MainMenu();
        var window = Host(menu, width: 900);
        window.Show();
        Settle();

        var tops = menu.MenuBar.Items.OfType<MenuItem>().ToList();
        Assert.Equal(9, tops.Count);
        for (var i = 0; i < tops.Count; i++)
        {
            var top = tops[i];
            top.Open();
            Settle();

            var popup = top.GetVisualDescendants().OfType<Popup>().First();
            Assert.True(popup.IsOpen, $"No se abrió el menú {top.Header}.");
            var box = (Control)popup.Child!;
            var name = $"menu-{i + 1:00}-{Slug((string)top.Header!)}";
            var size = Save(CaptureOf(box), name);
            Assert.True(size.Height > 40, $"El menú {top.Header} quedó vacío.");

            top.Close();
            Settle();
        }

        window.Close();
    }

    [AvaloniaFact]
    public void The_sampler_of_looks_is_drawn()
    {
        var picture = Draw(new Muestrario(), "muestrario");
        Assert.Equal(640, picture.Width);
    }

    [AvaloniaFact]
    public void A_long_tooltip_wraps()
    {
        var picture = Draw(new ToolTip { Content = TipSample }, "ayuda-al-pasar");
        Assert.InRange(picture.Width, 300, 400);
    }

    // ── The menu against WPF's ──────────────────────────────────

    /// <summary>Every row of WPF's menu, with its tooltip and whether it shows a shortcut.</summary>
    [AvaloniaFact]
    public void The_menu_has_every_row_of_the_WPF_one()
    {
        var wpf = WpfMenuRows();
        Assert.True(wpf.Count > 60, "No se pudo leer el menú de MainWindow.xaml.");

        var menu = new MainMenu();
        var window = Host(menu, width: 900);
        window.Show();
        Settle();
        var ours = menu.GetLogicalDescendants().OfType<MenuItem>()
            .Where(m => m.Header is string)
            .GroupBy(m => (string)m.Header!)
            .ToDictionary(g => g.Key, g => g.First());
        window.Close();

        var missing = new List<string>();
        foreach (var (header, tip, hasShortcut) in wpf)
        {
            if (!ours.TryGetValue(header, out var row))
            {
                missing.Add($"falta «{header}»");
                continue;
            }
            if (tip is not null && !Equals(ToolTip.GetTip(row), tip))
                missing.Add($"«{header}»: la ayuda no es la misma");
            if (hasShortcut != (row.InputGesture is not null))
                missing.Add($"«{header}»: el atajo no coincide");
        }
        Assert.True(missing.Count == 0, string.Join("\n", missing));
    }

    [AvaloniaFact]
    public void A_row_the_window_carries_out_is_passed_on()
    {
        var menu = new MainMenu();
        var heard = new List<MenuRequest>();
        menu.Requested += (_, r) => heard.Add(r);

        menu.Row(MenuRequest.Settings)!.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        menu.Row(MenuRequest.About)!.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        Assert.Equal([MenuRequest.Settings, MenuRequest.About], heard);
    }

    [AvaloniaFact]
    public void The_window_ticks_the_layout_rows()
    {
        var menu = new MainMenu();
        menu.SetChecked(MenuRequest.LeftRail, true);

        Assert.True(menu.Row(MenuRequest.LeftRail)!.IsChecked);
        Assert.False(menu.Row(MenuRequest.PlaylistTop)!.IsChecked);
    }

    /// <summary>The rows of the Menu of MainWindow.xaml: header, tooltip, shortcut or not.</summary>
    private static List<(string Header, string? Tip, bool HasShortcut)> WpfMenuRows()
    {
        XNamespace wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var path = Path.Combine(RepoRoot, "src", "EcclesiaCast.App", "MainWindow.xaml");
        var menu = XDocument.Load(path).Descendants(wpf + "Menu").First(e => (string?)e.Attribute(x + "Name") == "MainMenu");

        var rows = new List<(string, string?, bool)>();
        foreach (var item in menu.Descendants(wpf + "MenuItem"))
        {
            var header = (string?)item.Attribute("Header")
                ?? item.Descendants(wpf + "Setter").Where(s => (string?)s.Attribute("Property") == "Header")
                    .Select(s => (string?)s.Attribute("Value")).FirstOrDefault();
            if (header is null)
                continue;
            rows.Add((header, (string?)item.Attribute("ToolTip"), item.Attribute("InputGestureText") is not null));
        }
        return rows;
    }

    // ── Drawing ──────────────────────────────────────────────────

    private static Window Host(Control content, double? width = null)
    {
        var window = new Window
        {
            Content = content,
            SizeToContent = width is null ? SizeToContent.WidthAndHeight : SizeToContent.Height,
        };
        if (width is { } w)
            window.Width = w;
        return window;
    }

    private static PixelSize Draw(Control content, string name, double? width = null)
    {
        var window = Host(content, width);
        window.Show();
        Settle();
        var size = Save(window.CaptureRenderedFrame()!, name);
        window.Close();
        return size;
    }

    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// A control that floats over the window (an opened menu): drawn on its
    /// own, at its own size, as the WPF bench draws it.
    /// </summary>
    private static WriteableBitmap CaptureOf(Control control)
    {
        var size = control.Bounds.Size;
        var target = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height)));
        target.Render(control);
        using var stream = new MemoryStream();
        target.Save(stream, PngBitmapEncoderOptions.Default);
        stream.Position = 0;
        return WriteableBitmap.Decode(stream);
    }

    private static PixelSize Save(Bitmap picture, string name)
    {
        var folder = Folder(Path.Combine("avalonia", RenderBenchTests.System));
        var path = Path.Combine(folder, name + ".png");
        picture.Save(path, PngBitmapEncoderOptions.Default);
        var size = picture.PixelSize;
        picture.Dispose();

        var wpf = Path.Combine(Folder("wpf"), name + ".png");
        if (File.Exists(wpf))
            SideBySide(wpf, path, Path.Combine(Folder("comparacion"), name + ".png"));
        return size;
    }

    /// <summary>WPF on the left, Avalonia on the right, at their real size.</summary>
    private static void SideBySide(string wpfPath, string avaloniaPath, string outPath)
    {
        using var left = new Bitmap(wpfPath);
        using var right = new Bitmap(avaloniaPath);

        Control Column(string title, Bitmap picture) => new StackPanel
        {
            Margin = new Thickness(12),
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = title, Foreground = Brushes.White, FontWeight = FontWeight.SemiBold },
                new Image { Source = picture, Width = picture.Size.Width, Height = picture.Size.Height, Stretch = Stretch.None },
            },
        };

        // The width is said: left to the content, a wide pair is cut at the
        // width of the made-up screen.
        var window = new Window
        {
            Width = left.Size.Width + right.Size.Width + 48,
            SizeToContent = SizeToContent.Height,
            Background = new SolidColorBrush(Color.Parse("#3A3F4C")),
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { Column("WPF", left), Column("Avalonia", right) },
            },
        };
        window.Show();
        Settle();
        using var frame = window.CaptureRenderedFrame()!;
        frame.Save(outPath, PngBitmapEncoderOptions.Default);
        window.Close();
    }

    private static string RepoRoot =>
        Directory.GetParent(Directory.GetParent(RenderCases.Folder)!.FullName)!.FullName;

    private static string Folder(string name)
    {
        var folder = Path.Combine(RepoRoot, "artifacts", "pantallas", name);
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static string Slug(string text)
    {
        var plain = text.Replace("_", "").Normalize(NormalizationForm.FormD)
            .Where(c => char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
        return new string(plain.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-').ToArray());
    }
}
