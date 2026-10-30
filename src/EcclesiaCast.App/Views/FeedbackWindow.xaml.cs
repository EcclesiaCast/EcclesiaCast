using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Feedback;

namespace EcclesiaCast.App.Views;

/// <summary>Why the comments window is open: it decides the wording and whether it offers to stop asking.</summary>
public enum FeedbackReason
{
    /// <summary>The person chose Ayuda → Enviar comentarios.</summary>
    Menu,

    /// <summary>The program is closing after a real session.</summary>
    Closing,

    /// <summary>The last run did not end on its own.</summary>
    AfterCrash,
}

/// <summary>
/// «¿Querés enviarnos algún comentario?» The person writes, picks whether the
/// usage summary goes along (after reading it) and sends; nothing leaves the
/// computer before «Enviar», and closing the window sends nothing.
/// </summary>
public partial class FeedbackWindow : Window
{
    private const string ChurchKey = "feedback.church";
    private const string EmailKey = "feedback.email";

    private static readonly HttpClient Client = CreateClient();

    private readonly ISettingsStore _settings;
    private readonly FeedbackReason _reason;
    private readonly string _summary;
    private bool _sent;

    public FeedbackWindow(
        ISettingsStore settings, UsageStats stats, string dataDir, FeedbackReason reason, int sessionMinutes)
    {
        InitializeComponent();
        _settings = settings;
        _reason = reason;

        var errors = RecentErrors.Read(
            Path.Combine(dataDir, "logs"),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        _summary = stats.BuildSummary(AppInfo.Version, RuntimeInformation.OSDescription, sessionMinutes, errors);
        SummaryBox.Text = _summary;

        foreach (var kind in FeedbackForm.Kinds)
            KindBox.Items.Add(kind);
        KindBox.SelectedIndex = 0;

        ChurchBox.Text = settings.Get(ChurchKey) ?? "";
        EmailBox.Text = settings.Get(EmailKey) ?? "";

        switch (reason)
        {
            case FeedbackReason.Closing:
                HeadingText.Text = "¿Querés enviarnos algún comentario?";
                IntroText.Text = "Antes de cerrar: contanos qué anduvo bien, qué falló o qué te faltó. "
                    + "Es opcional y no lleva más de un minuto.";
                KindBox.SelectedItem = "Otra cosa";
                break;
            case FeedbackReason.AfterCrash:
                HeadingText.Text = "El programa se cerró de golpe la última vez";
                IntroText.Text = "Perdón por eso. Si nos contás qué estabas haciendo, lo podemos arreglar. "
                    + "Tildá «Adjuntar el resumen de uso» para que veamos los últimos errores.";
                KindBox.SelectedItem = FeedbackForm.KindProblem;
                AttachCheck.IsChecked = true;
                break;
            default:
                HeadingText.Text = "Enviar comentarios";
                IntroText.Text = "Contanos qué anduvo bien, qué falló o qué te faltó. Lo lee una persona, no un programa.";
                break;
        }

        if (reason != FeedbackReason.Closing)
            NeverCheck.Visibility = Visibility.Collapsed;
    }

    private void Preview_Click(object sender, RoutedEventArgs e) =>
        SummaryBox.Visibility = SummaryBox.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

    private void Skip_Click(object sender, RoutedEventArgs e) => Finish();

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        var comment = CommentBox.Text.Trim();
        var attach = AttachCheck.IsChecked == true;
        if (comment.Length == 0 && !attach)
        {
            ShowStatus("Escribí un comentario o tildá el resumen de uso. Si no querés mandar nada, tocá «Ahora no».");
            return;
        }

        SendButton.IsEnabled = false;
        SkipButton.IsEnabled = false;
        ShowStatus("Enviando…");

        var message = new FeedbackMessage(
            KindBox.SelectedItem as string ?? "Otra cosa",
            comment,
            ChurchBox.Text.Trim(),
            EmailBox.Text.Trim(),
            attach ? _summary : "");
        var ok = await new FeedbackSender(Client).SendAsync(message);

        if (!ok)
        {
            ShowStatus("No se pudo enviar. ¿Hay internet? Probá de nuevo, o tocá «Ahora no» para seguir sin enviar.");
            SendButton.IsEnabled = true;
            SkipButton.IsEnabled = true;
            return;
        }

        _sent = true;
        _settings.Set(ChurchKey, message.Church);
        _settings.Set(EmailKey, message.Email);
        ShowStatus("¡Gracias! Recibimos tu comentario.");
        await Task.Delay(1100);
        Finish();
    }

    private void Finish() => DialogResult = _sent;

    /// <summary>Also when the window is closed with the X: the tick is the person's answer either way.</summary>
    protected override void OnClosed(EventArgs e)
    {
        if (_reason == FeedbackReason.Closing && NeverCheck.IsChecked == true)
            FeedbackPolicy.NeverAgain(_settings);
        base.OnClosed(e);
    }

    private void ShowStatus(string text)
    {
        StatusText.Text = text;
        StatusText.Visibility = Visibility.Visible;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"EcclesiaCast/{AppInfo.Version}");
        return client;
    }
}
