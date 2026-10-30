namespace EcclesiaCast.Core.Feedback;

/// <summary>
/// Where the comments go: a Google Form that takes answers without a sign-in,
/// so nothing has to be run or paid for. The numbers are the form's own field
/// ids; changing a question's text does not change them, but deleting and
/// re-creating one does.
/// </summary>
public static class FeedbackForm
{
    public const string SubmitUrl =
        "https://docs.google.com/forms/d/e/1FAIpQLScgT4yZwrNimPJhISxnzI81M_gz9nDS_0Xudt-KSCCgqeTHwg/formResponse";

    public const string KindField = "entry.1937494968";
    public const string CommentField = "entry.1155759116";
    public const string ChurchField = "entry.343889983";
    public const string EmailField = "entry.1307411214";
    public const string UsageField = "entry.1557267329";

    /// <summary>The options of the form's «Tipo de comentario» list, word for word: the form drops any other text.</summary>
    public static readonly IReadOnlyList<string> Kinds =
        ["Algo no funcionó", "Tengo una idea", "Me gustó algo", "Otra cosa"];

    public const string KindProblem = "Algo no funcionó";

    /// <summary>
    /// The notice on closing is temporary: after this day it stops appearing
    /// by itself (the Help menu entry stays).
    /// </summary>
    public static readonly DateTime AutoPromptUntil = new(2027, 6, 30);
}

/// <summary>What one person sends. Every part is optional.</summary>
public sealed record FeedbackMessage(string Kind, string Comment, string Church, string Email, string UsageSummary)
{
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Comment) && string.IsNullOrWhiteSpace(UsageSummary);
}
