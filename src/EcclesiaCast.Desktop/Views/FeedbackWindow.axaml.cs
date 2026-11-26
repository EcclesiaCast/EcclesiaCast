using Avalonia.Controls;
using Avalonia.Interactivity;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// «¿Querés enviarnos algún comentario?» Everything it says and decides is in
/// <see cref="FeedbackViewModel"/>.
/// </summary>
public sealed partial class FeedbackWindow : Window
{
    private readonly FeedbackViewModel _form;
    private bool _closed;

    public FeedbackWindow(FeedbackViewModel form)
    {
        InitializeComponent();
        _form = form;
        DataContext = form;
    }

    private void Preview_Click(object? sender, RoutedEventArgs e) => _form.TogglePreview();

    private void Skip_Click(object? sender, RoutedEventArgs e) => Finish();

    private async void Send_Click(object? sender, RoutedEventArgs e)
    {
        switch (await _form.SendAsync())
        {
            case FeedbackSendResult.Empty:
                CommentBox.Focus();
                break;
            case FeedbackSendResult.Sent:
                Finish();
                break;
        }
    }

    private void Finish()
    {
        // Closed with the X while it was sending: there is nothing left to close.
        if (!_closed)
            Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _closed = true;
        _form.Closed();
        base.OnClosed(e);
    }
}
