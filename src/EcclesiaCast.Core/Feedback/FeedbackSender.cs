namespace EcclesiaCast.Core.Feedback;

/// <summary>Posts a comment to the form the same way the form's own page does.</summary>
public sealed class FeedbackSender(HttpClient client, string submitUrl = FeedbackForm.SubmitUrl)
{
    // Long answers take a lot, but not without limit; cut instead of failing.
    private const int MaxComment = 8_000;
    private const int MaxUsage = 20_000;

    public async Task<bool> SendAsync(FeedbackMessage message, CancellationToken cancellation = default)
    {
        var fields = new List<KeyValuePair<string, string>>
        {
            new(FeedbackForm.KindField, FeedbackForm.Kinds.Contains(message.Kind) ? message.Kind : FeedbackForm.Kinds[^1]),
            new(FeedbackForm.CommentField, Cut(message.Comment, MaxComment)),
            new(FeedbackForm.ChurchField, Cut(message.Church, 200)),
            new(FeedbackForm.EmailField, Cut(message.Email, 200)),
            new(FeedbackForm.UsageField, Cut(message.UsageSummary, MaxUsage)),
        };

        try
        {
            using var content = new FormUrlEncodedContent(fields);
            using var response = await client.PostAsync(submitUrl, content, cancellation).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            // Timeout: for the person it is the same as having no connection.
            return false;
        }
    }

    private static string Cut(string? text, int max)
    {
        text = (text ?? "").Trim();
        return text.Length <= max ? text : text[..max];
    }
}
