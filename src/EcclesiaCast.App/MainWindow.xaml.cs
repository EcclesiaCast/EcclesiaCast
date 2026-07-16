using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EcclesiaCast.App.ViewModels;

namespace EcclesiaCast.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Keep the live slide card visible when navigating with the arrows.
        DataContextChanged += (_, _) =>
        {
            if (DataContext is not MainViewModel vm)
                return;

            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.LiveSlideIndex))
                    ScrollLiveSlideIntoView(vm.LiveSlideIndex);
            };
        };
    }

    private void ScrollLiveSlideIntoView(int index)
    {
        if (index < 0)
            return;

        if (SlideGrid.ItemContainerGenerator.ContainerFromIndex(index) is FrameworkElement container)
            container.BringIntoView();
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
