using System.Windows;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// «¿Querés enviarnos algún comentario?» Everything it says and decides is in
/// <see cref="FeedbackViewModel"/>.
/// </summary>
public partial class FeedbackWindow : Window
{
    private readonly FeedbackViewModel _form;
    private bool _closed;

    public FeedbackWindow(FeedbackViewModel form)
    {
        InitializeComponent();
        _form = form;
        DataContext = form;
    }

    private void Preview_Click(object sender, RoutedEventArgs e) => _form.TogglePreview();

    private void Skip_Click(object sender, RoutedEventArgs e) => Finish();

    private async void Send_Click(object sender, RoutedEventArgs e)
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
        // Closed with the X while it was sending: nobody is waiting for an answer.
        if (!_closed)
            DialogResult = _form.Sent;
    }

    protected override void OnClosed(EventArgs e)
    {
        _closed = true;
        _form.Closed();
        base.OnClosed(e);
    }
}
