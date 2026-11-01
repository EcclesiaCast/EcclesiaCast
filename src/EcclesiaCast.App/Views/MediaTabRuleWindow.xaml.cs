using System.Windows;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Views;

/// <summary>
/// The rule of one media tab: where everything in it goes (behind the text, on
/// the whole screen, or as each media says) and whether a foreground tab ends
/// on the logo.
/// </summary>
public partial class MediaTabRuleWindow : Window
{
    public MediaTabRuleWindow(string tab, MediaTabRule current, bool presentation = false)
    {
        InitializeComponent();
        PresentationCheck.IsChecked = presentation;
        Title = $"Configurar «{tab}»";
        Heading.Text = $"¿Cómo se usan los medios de la pestaña «{tab}»?";

        switch (current.Mode)
        {
            case MediaTabMode.Background: BackgroundRadio.IsChecked = true; break;
            case MediaTabMode.Foreground: ForegroundRadio.IsChecked = true; break;
            default: PerItemRadio.IsChecked = true; break;
        }

        LogoCheck.IsChecked = current.LogoAtEnd;
        MuteCheck.IsChecked = current.Muted;
        SecondsBox.Text = current.SlideSeconds.ToString();
        switch (current.Slideshow)
        {
            case MediaSlideshow.Sequential: ShowSequentialRadio.IsChecked = true; break;
            case MediaSlideshow.Random: ShowRandomRadio.IsChecked = true; break;
            default: ShowOffRadio.IsChecked = true; break;
        }

        Mode_Changed(this, new RoutedEventArgs());
        Show_Changed(this, new RoutedEventArgs());
    }

    /// <summary>The seconds only matter while the tab passes its media by itself.</summary>
    private void Show_Changed(object sender, RoutedEventArgs e)
    {
        if (SecondsPanel is null)
            return;

        SecondsPanel.IsEnabled = ShowOffRadio.IsChecked != true;
        PresentationCheck.IsEnabled = ShowOffRadio.IsChecked == true;
        if (!PresentationCheck.IsEnabled)
            PresentationCheck.IsChecked = false;
    }

    /// <summary>The rule the operator chose; valid once the window closed with Guardar.</summary>
    public MediaTabRule Rule { get; private set; } = MediaTabRule.Default;

    /// <summary>Whether the tab opens as a presentation in the central grid.</summary>
    public bool Presentation { get; private set; }

    /// <summary>The logo at the end only makes sense for a foreground.</summary>
    private void Mode_Changed(object sender, RoutedEventArgs e)
    {
        if (LogoCheck is null)
            return;

        LogoCheck.IsEnabled = ForegroundRadio.IsChecked == true;
        if (!LogoCheck.IsEnabled)
            LogoCheck.IsChecked = false;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var mode = ForegroundRadio.IsChecked == true ? MediaTabMode.Foreground
            : BackgroundRadio.IsChecked == true ? MediaTabMode.Background
            : MediaTabMode.PerItem;

        var slideshow = ShowRandomRadio.IsChecked == true ? MediaSlideshow.Random
            : ShowSequentialRadio.IsChecked == true ? MediaSlideshow.Sequential
            : MediaSlideshow.Off;
        var seconds = int.TryParse(SecondsBox.Text.Trim(), out var typed)
            ? MediaTabRule.ClampSeconds(typed)
            : MediaTabRule.DefaultSlideSeconds;

        Rule = new MediaTabRule(
            mode,
            mode == MediaTabMode.Foreground && LogoCheck.IsChecked == true,
            MuteCheck.IsChecked == true,
            slideshow,
            seconds);
        Presentation = PresentationCheck.IsChecked == true;
        DialogResult = true;
    }
}
