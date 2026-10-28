using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EcclesiaCast.App.Remote;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Remote;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Serilog;

namespace EcclesiaCast.App.Views;

/// <summary>
/// «Transmisión → Estilos de la transmisión…»: the design of each of the three
/// layers of the stream, with a preview that is the very page OBS loads, drawn
/// over a made-up camera. Nothing is saved until «Guardar».
/// </summary>
public partial class BroadcastStyleWindow : Window
{
    private const string FileHost = "fondo.ecclesiacast.local";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>A stage-like backdrop (violet and dark blue with lights) so the plates can be judged against something.</summary>
    private const string FakeCamera =
        "document.documentElement.style.background = "
        + "'radial-gradient(40% 50% at 18% 30%, rgba(255,190,90,.55), transparent 70%),"
        + "radial-gradient(35% 45% at 82% 22%, rgba(120,170,255,.5), transparent 70%),"
        + "radial-gradient(60% 40% at 50% 105%, rgba(255,255,255,.14), transparent 70%),"
        + "linear-gradient(135deg, #3a1c6b 0%, #1a1747 45%, #0c1230 100%)';";

    private readonly ISettingsStore _settings;
    private BroadcastStyles _draft = BroadcastStyleSettings.Current;
    private OutputChannel _channel = OutputChannel.Bible;

    /// <summary>True while the controls are being filled from a style, so filling does not count as editing.</summary>
    private bool _loading = true;
    private bool _ready;

    private WebView2? _web;
    private bool _pageLoaded;
    private string? _mappedFolder;

    public BroadcastStyleWindow(ISettingsStore settings)
    {
        InitializeComponent();
        _settings = settings;

        FontCombo.ItemsSource = System.Windows.Media.Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .OrderBy(name => name)
            .ToList();

        foreach (var preset in BroadcastPresets.All)
        {
            var button = new RadioButton
            {
                Content = preset.Name,
                ToolTip = preset.Description,
                Tag = preset,
                GroupName = "Preset",
                Style = (Style)FindResource("Choice"),
            };
            // Click, not Checked: clicking the look already chosen puts it back after a touch-up.
            button.Click += Preset_Click;
            PresetPanel.Children.Add(button);
        }

        Loaded += async (_, _) => await StartPreviewAsync();
        Closed += (_, _) => _web?.Dispose();

        Fill();
        _ready = true;
    }

    private BroadcastStyle Current => _draft.For(_channel);

    // ── Controls ↔ style ─────────────────────────────────────────

    /// <summary>Shows the style of the open layer in the controls.</summary>
    private void Fill()
    {
        var s = Current;
        _loading = true;

        FollowCheck.IsChecked = s.FollowProjector;
        Pick(s.Position.ToString(), PosTop, PosCenter, PosBottom);
        Pick(s.Plate.ToString(), PlNone, PlTab, PlGlass, PlSolid, PlGradient, PlHalo, PlFillet, PlLine);
        Pick(s.Background.ToString(), BgNone, BgColor, BgFile);

        if (!FontCombo.Items.Contains(s.FontFamily))
            ((List<string>)FontCombo.ItemsSource).Insert(0, s.FontFamily);
        FontCombo.Items.Refresh();
        FontCombo.SelectedItem = s.FontFamily;

        SizeSlider.Value = s.FontSize;
        BoldCheck.IsChecked = s.Bold;
        UpperCheck.IsChecked = s.Uppercase;
        OpacitySlider.Value = s.PlateOpacity;
        WidthSlider.Value = s.WidthPercent;
        MarginSlider.Value = s.MarginPercent;
        LabelCheck.IsChecked = s.ShowLabel;
        LabelSizeSlider.Value = s.LabelSize;
        Pick(s.LabelAlign.ToString(), LabLeft, LabCenter, LabRight);
        Pick(s.LabelBelow ? "Below" : "Above", LabAbove, LabBelow);

        _loading = false;
        Refresh();
    }

    private static void Pick(string tag, params RadioButton[] group)
    {
        foreach (var button in group)
            button.IsChecked = (string)button.Tag == tag;
    }

    private static T Chosen<T>(T fallback, params RadioButton[] group) where T : struct, Enum
    {
        var button = group.FirstOrDefault(b => b.IsChecked == true);
        return button is not null && Enum.TryParse<T>((string)button.Tag, out var value) ? value : fallback;
    }

    /// <summary>The style the controls describe, built over the open one (colours and the file are kept by their own buttons).</summary>
    private BroadcastStyle Read() => Current with
    {
        FollowProjector = FollowCheck.IsChecked == true,
        Position = Chosen(Current.Position, PosTop, PosCenter, PosBottom),
        Plate = Chosen(Current.Plate, PlNone, PlTab, PlGlass, PlSolid, PlGradient, PlHalo, PlFillet, PlLine),
        Background = Chosen(Current.Background, BgNone, BgColor, BgFile),
        FontFamily = FontCombo.SelectedItem as string ?? Current.FontFamily,
        FontSize = SizeSlider.Value,
        Bold = BoldCheck.IsChecked == true,
        Uppercase = UpperCheck.IsChecked == true,
        PlateOpacity = (int)OpacitySlider.Value,
        WidthPercent = (int)WidthSlider.Value,
        MarginPercent = (int)MarginSlider.Value,
        ShowLabel = LabelCheck.IsChecked == true,
        LabelSize = (int)LabelSizeSlider.Value,
        LabelAlign = Chosen(Current.LabelAlign, LabLeft, LabCenter, LabRight),
        LabelBelow = LabBelow.IsChecked == true,
    };

    private void Control_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading)
            return;

        _draft = _draft.With(_channel, Read().Clamp());

        // Choosing «Imagen o video…» with no file yet asks for one right away;
        // if none is picked the background goes back to transparent.
        if (ReferenceEquals(sender, BgFile) && string.IsNullOrEmpty(Current.BackgroundFile) && !AskForFile())
        {
            _draft = _draft.With(_channel, Current with { Background = BroadcastBackground.None });
            _loading = true;
            Pick("None", BgNone, BgColor, BgFile);
            _loading = false;
        }

        Refresh();
    }

    private void Slider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) => Control_Changed(sender, e);

    private void Layer_Checked(object sender, RoutedEventArgs e)
    {
        // The first tab is checked while the window is still being built.
        if (!_ready || sender is not RadioButton { Tag: string name } || !Enum.TryParse<OutputChannel>(name, out var channel))
            return;

        _channel = channel;
        Fill();
    }

    /// <summary>Updates what depends on the style (readouts, which rows show) and redraws the preview.</summary>
    private void Refresh()
    {
        var s = Current;
        SizeText.Text = ((int)s.FontSize).ToString();
        OpacityText.Text = s.PlateOpacity + " %";
        WidthText.Text = s.WidthPercent + " %";
        MarginText.Text = s.MarginPercent + " %";
        LabelSizeText.Text = s.LabelSize + " %";

        Controls.IsEnabled = !s.FollowProjector;
        Controls.Opacity = s.FollowProjector ? 0.4 : 1;
        BgColorRow.Visibility = s.Background == BroadcastBackground.Color ? Visibility.Visible : Visibility.Collapsed;
        BgFileRow.Visibility = s.Background == BroadcastBackground.File ? Visibility.Visible : Visibility.Collapsed;
        BgFileName.Text = string.IsNullOrEmpty(s.BackgroundFile) ? "Ninguno" : Path.GetFileName(s.BackgroundFile);

        SetSwatch(TextColorButton, s.TextColor);
        SetSwatch(AccentButton, s.AccentColor);
        SetSwatch(PlateButton, s.PlateColor);
        SetSwatch(BgColorButton, s.BackgroundColor);

        // The look the layer wears is marked; none is once it has been touched up.
        foreach (var button in PresetPanel.Children.OfType<RadioButton>())
            button.IsChecked = button.Tag is BroadcastPreset preset && preset.Matches(s);

        PushPreview();
    }

    private static void SetSwatch(Button button, string hex)
    {
        Brush brush;
        try
        {
            brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        catch (FormatException)
        {
            brush = Brushes.Transparent;
        }

        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(new Border
        {
            Width = 18, Height = 18, CornerRadius = new CornerRadius(3), Background = brush,
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), BorderThickness = new Thickness(1),
        });
        row.Children.Add(new TextBlock { Text = hex, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
        button.Content = row;
    }

    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string which })
            return;

        var s = Current;
        var current = which switch
        {
            "Text" => s.TextColor,
            "Accent" => s.AccentColor,
            "Plate" => s.PlateColor,
            _ => s.BackgroundColor,
        };

        var picked = ColorPickerHelper.Pick(current);
        if (picked is null)
            return;

        _draft = _draft.With(_channel, (which switch
        {
            "Text" => s with { TextColor = picked },
            "Accent" => s with { AccentColor = picked },
            "Plate" => s with { PlateColor = picked },
            _ => s with { BackgroundColor = picked },
        }).Clamp());
        Refresh();
    }

    private void PickFile_Click(object sender, RoutedEventArgs e)
    {
        if (AskForFile())
            Refresh();
    }

    /// <summary>Lets the operator choose the picture or video; true when one was chosen.</summary>
    private bool AskForFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Fondo de la transmisión",
            Filter = "Imágenes y videos|*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp;*.mp4;*.m4v;*.webm;*.mov",
        };
        if (dialog.ShowDialog(this) != true)
            return false;

        _draft = _draft.With(_channel, Current with { BackgroundFile = dialog.FileName, Background = BroadcastBackground.File });
        return true;
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: BroadcastPreset preset })
            return;

        _draft = _draft.With(_channel, preset.ApplyTo(Current));
        Fill();
    }

    private void Defaults_Click(object sender, RoutedEventArgs e)
    {
        _draft = _draft.With(_channel, BroadcastStyle.Default(_channel));
        Fill();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        foreach (var channel in new[] { OutputChannel.Bible, OutputChannel.Songs, OutputChannel.Announcements })
        {
            if (_draft.For(channel) != BroadcastStyleSettings.Current.For(channel))
                BroadcastStyleSettings.Set(channel, _draft.For(channel), _settings);
        }

        DialogResult = true;
    }

    // ── Preview ──────────────────────────────────────────────────

    private async Task StartPreviewAsync()
    {
        try
        {
            _web = new WebView2 { DefaultBackgroundColor = System.Drawing.Color.Black };
            PreviewSlot.Content = _web;
            FitPreview();

            await _web.EnsureCoreWebView2Async(await WebViewProfile.GetEnvironmentAsync());
            var core = _web.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.NavigationCompleted += async (_, _) =>
            {
                // The page does not ask any server in here: it is drawn by calling paint().
                await core.ExecuteScriptAsync(FakeCamera);
                _pageLoaded = true;
                PreviewMessage.Visibility = Visibility.Collapsed;
                PushPreview();
            };
            core.NavigateToString(OutputPage.Html);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo mostrar la vista previa de los estilos de la transmisión");
            _web = null;
            PreviewSlot.Content = null;
            PreviewMessage.Text = "No se pudo mostrar la vista previa";
            PreviewMessage.Visibility = Visibility.Visible;
        }
    }

    private void PreviewHost_SizeChanged(object sender, SizeChangedEventArgs e) => FitPreview();

    /// <summary>Keeps the preview 16:9 inside the space it has, like the screen of the stream.</summary>
    private void FitPreview()
    {
        if (_web is null)
            return;

        var width = Math.Max(160, PreviewHost.ActualWidth - 4);
        var height = Math.Max(90, PreviewHost.ActualHeight - 4);
        if (width / height > 16d / 9d)
            width = height * 16d / 9d;
        else
            height = width * 9d / 16d;

        _web.Width = width;
        _web.Height = height;
    }

    private (string Main, string Label) Sample() => _channel switch
    {
        OutputChannel.Songs => ("Santo, santo, santo, Señor omnipotente", "CORO"),
        OutputChannel.Announcements => ("Reunión de jóvenes · Sábado 19 h", string.Empty),
        _ => ("Porque de tal manera amó Dios al mundo, que ha dado á su Hijo unigénito, para que todo aquel que en él cree, no se pierda, mas tenga vida eterna.",
            "JUAN 3:16 · RV1909"),
    };

    /// <summary>Draws the open style on the preview with the sample text of its layer.</summary>
    private void PushPreview()
    {
        if (_web?.CoreWebView2 is not { } core || !_pageLoaded)
            return;

        try
        {
            var style = Current.Clamp();
            var (main, label) = Sample();

            var output = new JsonObject
            {
                ["showText"] = true,
                ["mainText"] = main,
                ["secondaryText"] = string.Empty,
                ["caption"] = string.Empty,
                ["label"] = label,
                ["blank"] = false,
                ["mainMarks"] = new JsonArray(),
                ["secondaryMarks"] = new JsonArray(),
                // What the projector would give when the layer follows it.
                ["fontFamily"] = "Segoe UI",
                ["fontSize"] = 54,
                ["bold"] = true,
                ["italic"] = false,
                ["color"] = "#FFFFFF",
                ["outlineColor"] = "#000000",
                ["outlineWidth"] = 2,
                ["alignH"] = "center",
                ["alignV"] = "bottom",
            };

            if (!style.FollowProjector)
            {
                var node = JsonSerializer.SerializeToNode(style, Json)!.AsObject();
                node["backgroundFile"] = Path.GetFileName(style.BackgroundFile);
                node["backgroundUrl"] = PreviewFileUrl(core, style);
                output["style"] = node;
            }

            _ = core.ExecuteScriptAsync("paint(" + output.ToJsonString(Json) + ");");
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo actualizar la vista previa de los estilos");
        }
    }

    /// <summary>
    /// The preview has no server to fetch the background from, so the folder of
    /// the file is mapped to a made-up address the page can load it from.
    /// </summary>
    private string? PreviewFileUrl(CoreWebView2 core, BroadcastStyle style)
    {
        if (style.Background != BroadcastBackground.File || !File.Exists(style.BackgroundFile))
            return null;

        var folder = Path.GetDirectoryName(style.BackgroundFile)!;
        if (!string.Equals(folder, _mappedFolder, StringComparison.OrdinalIgnoreCase))
        {
            core.SetVirtualHostNameToFolderMapping(FileHost, folder, CoreWebView2HostResourceAccessKind.Allow);
            _mappedFolder = folder;
        }

        return $"https://{FileHost}/{Uri.EscapeDataString(Path.GetFileName(style.BackgroundFile))}";
    }
}
