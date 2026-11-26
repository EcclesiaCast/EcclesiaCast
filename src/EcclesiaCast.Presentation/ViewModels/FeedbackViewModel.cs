using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Feedback;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>How pressing «Enviar» ended.</summary>
public enum FeedbackSendResult
{
    /// <summary>Nothing written: nothing was sent.</summary>
    Empty,

    /// <summary>It could not be sent; the window stays to try again.</summary>
    Failed,

    /// <summary>It arrived and the thanks were shown: the window closes.</summary>
    Sent,
}

/// <summary>
/// The comments form. The person writes, picks whether the usage summary goes
/// along (after reading it) and sends; nothing leaves the computer before
/// «Enviar», and closing the window sends nothing. Nothing written means
/// nothing to send: <see cref="CanSend"/> waits for the comment and
/// <see cref="ShowCommentHint"/> says why.
/// </summary>
public sealed partial class FeedbackViewModel : ObservableObject
{
    private const string ChurchKey = "feedback.church";
    private const string EmailKey = "feedback.email";
    private const string OtherKind = "Otra cosa";

    private readonly ISettingsStore _settings;
    private readonly FeedbackReason _reason;
    private readonly Func<FeedbackMessage, Task<bool>> _send;

    public FeedbackViewModel(
        ISettingsStore settings, FeedbackReason reason, string usageSummary,
        Func<FeedbackMessage, Task<bool>> send)
    {
        _settings = settings;
        _reason = reason;
        _send = send;
        Summary = usageSummary;
        _kind = Kinds[0];
        _church = settings.Get(ChurchKey) ?? "";
        _email = settings.Get(EmailKey) ?? "";

        switch (reason)
        {
            case FeedbackReason.Closing:
                Heading = "¿Querés enviarnos algún comentario?";
                Intro = "Antes de cerrar: contanos qué anduvo bien, qué falló o qué te faltó. "
                    + "Es opcional y no lleva más de un minuto.";
                _kind = OtherKind;
                break;
            case FeedbackReason.AfterCrash:
                Heading = "El programa se cerró de golpe la última vez";
                Intro = "Perdón por eso. Si nos contás qué estabas haciendo, lo podemos arreglar. "
                    + "Tildá «Adjuntar el resumen de uso» para que veamos los últimos errores.";
                _kind = FeedbackForm.KindProblem;
                _attach = true;
                break;
            default:
                Heading = "Enviar comentarios";
                Intro = "Contanos qué anduvo bien, qué falló o qué te faltó. Lo lee una persona, no un programa.";
                break;
        }
    }

    /// <summary>How long the thanks stay on screen before the window closes.</summary>
    public TimeSpan ThanksPause { get; init; } = TimeSpan.FromMilliseconds(1100);

    public string Heading { get; }

    public string Intro { get; }

    public IReadOnlyList<string> Kinds => FeedbackForm.Kinds;

    /// <summary>The usage summary, exactly as it would be sent.</summary>
    public string Summary { get; }

    /// <summary>«No volver a mostrar» only makes sense on the notice that comes by itself on closing.</summary>
    public bool OffersNeverAgain => _reason == FeedbackReason.Closing;

    [ObservableProperty]
    private string _kind;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend), nameof(ShowCommentHint))]
    private string _comment = "";

    [ObservableProperty]
    private string _church;

    [ObservableProperty]
    private string _email;

    [ObservableProperty]
    private bool _attach;

    [ObservableProperty]
    private bool _never;

    [ObservableProperty]
    private bool _isSummaryVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string? _status;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend), nameof(CanSkip))]
    private bool _isSending;

    public bool HasStatus => Status is not null;

    private bool HasComment => !string.IsNullOrWhiteSpace(Comment);

    public bool ShowCommentHint => !HasComment;

    public bool CanSend => HasComment && !IsSending;

    public bool CanSkip => !IsSending;

    /// <summary>True once a comment arrived (the window's answer).</summary>
    public bool Sent { get; private set; }

    public void TogglePreview() => IsSummaryVisible = !IsSummaryVisible;

    public async Task<FeedbackSendResult> SendAsync()
    {
        var message = new FeedbackMessage(
            Kind ?? OtherKind,
            (Comment ?? "").Trim(),
            (Church ?? "").Trim(),
            (Email ?? "").Trim(),
            Attach ? Summary : "");
        if (message.IsEmpty)
        {
            // The button is off while the box is empty; this is only a second guard.
            Status = "Escribí qué pasó o qué querés contarnos. Si no querés mandar nada, tocá «Ahora no».";
            return FeedbackSendResult.Empty;
        }

        IsSending = true;
        Status = "Enviando…";

        if (!await _send(message))
        {
            IsSending = false;
            Status = "No se pudo enviar. ¿Hay internet? Probá de nuevo, o tocá «Ahora no» para seguir sin enviar.";
            return FeedbackSendResult.Failed;
        }

        Sent = true;
        _settings.Set(ChurchKey, message.Church);
        _settings.Set(EmailKey, message.Email);
        Status = "¡Gracias! Recibimos tu comentario.";
        await Task.Delay(ThanksPause);
        return FeedbackSendResult.Sent;
    }

    /// <summary>
    /// The window closed, however it did (the X too): the tick is the
    /// person's answer either way.
    /// </summary>
    public void Closed()
    {
        if (OffersNeverAgain && Never)
            FeedbackPolicy.NeverAgain(_settings);
    }
}
