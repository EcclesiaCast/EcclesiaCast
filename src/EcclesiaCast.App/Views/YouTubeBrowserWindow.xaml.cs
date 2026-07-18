using System.Windows;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;
using Serilog;

namespace EcclesiaCast.App.Views;

/// <summary>
/// An embedded YouTube browser: the operator signs in with the church's own
/// account (Premium plays without ads) and can search and pick videos to add
/// to the library. The session lives in the app's browser profile.
/// </summary>
public partial class YouTubeBrowserWindow : Window
{
    public YouTubeBrowserWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await StartAsync();
    }

    /// <summary>Video ids the operator chose to add.</summary>
    public List<string> AddedVideoIds { get; } = [];

    private async Task StartAsync()
    {
        try
        {
            var environment = await WebViewProfile.GetEnvironmentAsync();
            await Browser.EnsureCoreWebView2Async(environment);
            Browser.CoreWebView2.Navigate("https://www.youtube.com/");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo abrir el navegador de YouTube");
            MessageBox.Show(
                "No se pudo abrir el navegador embebido.\n\n" +
                "Instalá el runtime de WebView2 (Microsoft Edge WebView2) y volvé a intentar.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (Browser.CanGoBack)
            Browser.GoBack();
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (Browser.CanGoForward)
            Browser.GoForward();
    }

    private void Home_Click(object sender, RoutedEventArgs e) =>
        Browser.CoreWebView2?.Navigate("https://www.youtube.com/");

    private void AddCurrent_Click(object sender, RoutedEventArgs e)
    {
        var id = YouTubeUrl.TryParseVideoId(Browser.Source?.ToString());
        if (id is null)
        {
            MessageBox.Show(
                "Abrí primero el video que querés agregar (la página tiene que ser la del video).",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!AddedVideoIds.Contains(id))
            AddedVideoIds.Add(id);

        DialogResult = true;
    }
}
