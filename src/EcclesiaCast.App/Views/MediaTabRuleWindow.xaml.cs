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
    public MediaTabRuleWindow(string tab, MediaTabRule current)
    {
        InitializeComponent();
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
        Mode_Changed(this, new RoutedEventArgs());
    }

    /// <summary>The rule the operator chose; valid once the window closed with Guardar.</summary>
    public MediaTabRule Rule { get; private set; } = MediaTabRule.Default;

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

        Rule = new MediaTabRule(
            mode,
            mode == MediaTabMode.Foreground && LogoCheck.IsChecked == true,
            MuteCheck.IsChecked == true);
        DialogResult = true;
    }
}
