using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Views;

public partial class MediaInspectorWindow : Window
{
    private readonly MediaItem _item;

    public MediaInspectorWindow(MediaItem item, IReadOnlyList<string> categories)
    {
        InitializeComponent();
        _item = item;

        NameBox.Text = item.Name;
        TypeText.Text = item.Type switch
        {
            MediaType.Video => "Video",
            MediaType.YouTube => $"YouTube · {item.YouTubeId}",
            _ => "Imagen",
        };
        VideoOptions.Visibility = item.Type is MediaType.Video or MediaType.YouTube
            ? Visibility.Visible
            : Visibility.Collapsed;

        // YouTube always fills the screen with its own player.
        ScalingBox.IsEnabled = item.Type != MediaType.YouTube;

        CategoryBox.ItemsSource = categories;
        CategoryBox.Text = item.Category;

        BehaviorBox.SelectedIndex = (int)item.Behavior;
        ScalingBox.SelectedIndex = (int)item.Scaling;
        EndBox.SelectedIndex = (int)item.EndBehavior;
        MuteCheck.IsChecked = item.Muted;
        VolSlider.Value = item.Volume;

        var poster = item.ThumbnailPath ?? (item.Type == MediaType.Image ? item.Path : null);
        if (!string.IsNullOrWhiteSpace(poster) && File.Exists(poster))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(poster);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.DecodePixelWidth = 240;
                bmp.EndInit();
                bmp.Freeze();
                Poster.Source = bmp;
            }
            catch { /* ignore */ }
        }
    }

    public bool Saved { get; private set; }

    private void Vol_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) =>
        VolValue.Text = $"{VolSlider.Value:0}";

    private void OpenLocation_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(_item.Path))
            return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{_item.Path}\"",
            UseShellExecute = true,
        });
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _item.Name = string.IsNullOrWhiteSpace(NameBox.Text) ? _item.Name : NameBox.Text.Trim();
        _item.Category = string.IsNullOrWhiteSpace(CategoryBox.Text) ? "Fondos" : CategoryBox.Text.Trim();
        _item.Behavior = (MediaBehavior)Math.Max(0, BehaviorBox.SelectedIndex);
        _item.Scaling = (MediaScaling)Math.Max(0, ScalingBox.SelectedIndex);
        _item.EndBehavior = (VideoEndBehavior)Math.Max(0, EndBox.SelectedIndex);
        _item.Muted = MuteCheck.IsChecked == true;
        _item.Volume = (int)VolSlider.Value;
        Saved = true;
        DialogResult = true;
    }
}
