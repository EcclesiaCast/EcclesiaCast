using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App;

public partial class MainWindow : Window
{
    private const string LayoutLibraryKey = "layout.library.width";
    private const string LayoutPreviewKey = "layout.preview.width";
    private const string LayoutPlaylistKey = "layout.playlist.height";
    private const string LayoutWindowKey = "layout.window";

    private ISettingsStore? _settings;

    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is not MainViewModel vm)
                return;

            // Keep the relevant slide card visible: the live one while
            // navigating with the arrows, the previewed one after a search.
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.LiveSlideIndex))
                    ScrollSlideIntoView(vm.LiveSlideIndex);
                else if (args.PropertyName == nameof(MainViewModel.PreviewSlideIndex))
                    ScrollSlideIntoView(vm.PreviewSlideIndex);
            };

            // A fresh chapter or passage always starts at the top.
            vm.Slides.CollectionChanged += (_, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                    SlidesScroll.ScrollToTop();
            };
            vm.BibleChapters.CollectionChanged += (_, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                    ChaptersScroll.ScrollToTop();
            };
        };
    }

    /// <summary>Restores the panel sizes saved from the last session.</summary>
    public void AttachLayoutPersistence(ISettingsStore settings)
    {
        _settings = settings;

        if (ReadDouble(LayoutLibraryKey) is double library)
            LibraryColumn.Width = new GridLength(library);
        if (ReadDouble(LayoutPreviewKey) is double preview)
            PreviewColumn.Width = new GridLength(preview);
        if (ReadDouble(LayoutPlaylistKey) is double playlist)
            PlaylistRow.Height = new GridLength(playlist);

        if (settings.Get(LayoutWindowKey)?.Split(';') is [var w, var h, var state]
            && double.TryParse(w, out var width) && double.TryParse(h, out var height))
        {
            Width = Math.Max(MinWidth, width);
            Height = Math.Max(MinHeight, height);
            if (state == "max")
                WindowState = WindowState.Maximized;
        }

        Closing += (_, _) => SaveLayout();
    }

    private double? ReadDouble(string key) =>
        double.TryParse(_settings?.Get(key), out var value) && value > 0 ? value : null;

    private void SaveLayout()
    {
        if (_settings is null)
            return;

        try
        {
            _settings.Set(LayoutLibraryKey, LibraryColumn.Width.Value.ToString("0"));
            _settings.Set(LayoutPreviewKey, PreviewColumn.Width.Value.ToString("0"));
            _settings.Set(LayoutPlaylistKey, PlaylistRow.Height.Value.ToString("0"));

            var size = WindowState == WindowState.Maximized
                ? $"{RestoreBounds.Width:0};{RestoreBounds.Height:0};max"
                : $"{Width:0};{Height:0};normal";
            _settings.Set(LayoutWindowKey, size);
        }
        catch
        {
            // El layout es cosmético: nunca debe impedir cerrar la app.
        }
    }

    // Bible slides have no per-slide actions, so suppress their context menu.
    private void SlideCard_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SlideItemViewModel { SectionId: 0 } })
            e.Handled = true;
    }

    private void ScrollSlideIntoView(int index)
    {
        if (index < 0)
            return;

        // Defer until the cards have been laid out (they may have just been added).
        Dispatcher.InvokeAsync(() =>
        {
            if (SlideGrid.ItemContainerGenerator.ContainerFromIndex(index) is FrameworkElement container)
                container.BringIntoView();
        }, DispatcherPriority.Background);
    }

    // Arrow keys must be intercepted before WPF's directional focus
    // navigation consumes them (it moves focus between slide cards and
    // the event never reaches the window's input bindings).
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Handled || (e.Key != Key.Left && e.Key != Key.Right))
            return;

        // Leave the arrows alone while the operator is typing.
        if (e.OriginalSource is TextBox)
            return;

        if (DataContext is not MainViewModel vm)
            return;

        if (e.Key == Key.Right)
            vm.NextSlideCommand.Execute(null);
        else
            vm.PreviousSlideCommand.Execute(null);

        e.Handled = true;
    }
}
