using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace EcclesiaCast.Desktop.Views;

/// <summary>The loading screen, while the library and the video engine open.</summary>
public sealed class SplashWindow : Window
{
    public SplashWindow()
    {
        Title = "EcclesiaCast";
        Width = 420;
        Height = 160;
        CanResize = false;
        ShowInTaskbar = false;
        WindowDecorations = WindowDecorations.None;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.Parse("#10141E"));
        Content = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "EcclesiaCast", FontSize = 28, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center },
                new TextBlock { Text = "Abriendo la biblioteca…", Foreground = Brushes.LightGray, HorizontalAlignment = HorizontalAlignment.Center },
            },
        };
    }
}
