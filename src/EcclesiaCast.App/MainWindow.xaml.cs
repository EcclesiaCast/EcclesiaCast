using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using EcclesiaCast.App.ViewModels;

namespace EcclesiaCast.App;

public partial class MainWindow : Window
{
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
