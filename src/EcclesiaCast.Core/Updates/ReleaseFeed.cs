using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EcclesiaCast.Core.Updates;

/// <summary>Why a search for a new version came back with nothing to say.</summary>
public enum UpdateCheckFailure
{
    /// <summary>No internet, or GitHub did not answer in time.</summary>
    NoConnection,

    /// <summary>GitHub answered, but not with the list of releases (limit of requests, a change in the API).</summary>
    Unexpected,
}

/// <summary>
/// A search that did not work. The program does not bother anyone about it
/// when it searched by itself: it is written to the log and nothing else.
/// </summary>
public sealed class UpdateCheckException(UpdateCheckFailure failure, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public UpdateCheckFailure Failure { get; } = failure;
}

/// <summary>
/// Asks GitHub which releases there are. The address and the client are
/// parameters so a test can put a local server in its place.
/// </summary>
public sealed class ReleaseFeed(HttpClient client, string url = GitHubReleases.DefaultUrl)
{
    /// <summary>GitHub gives up on requests without a User-Agent; this is the one that says who is asking.</summary>
    public static HttpClient CreateClient(string appVersion)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"EcclesiaCast/{appVersion}");
        return client;
    }

    /// <exception cref="UpdateCheckException">There is no list to read.</exception>
    public async Task<IReadOnlyList<ReleaseInfo>> FetchAsync(CancellationToken cancellation = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

            using var response = await client.SendAsync(request, cancellation);
            if (!response.IsSuccessStatusCode)
            {
                // 403 and 429 are how GitHub says "too many requests from this address".
                var limited = response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests;
                throw new UpdateCheckException(
                    UpdateCheckFailure.Unexpected,
                    limited
                        ? $"GitHub puso un límite de pedidos ({(int)response.StatusCode})"
                        : $"GitHub contestó {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellation);
            return GitHubReleases.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new UpdateCheckException(UpdateCheckFailure.Unexpected, "GitHub mandó algo que no es la lista de versiones", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new UpdateCheckException(UpdateCheckFailure.NoConnection, "No hay conexión con GitHub", ex);
        }
        catch (OperationCanceledException ex) when (!cancellation.IsCancellationRequested)
        {
            // HttpClient reports its own time limit as a cancellation.
            throw new UpdateCheckException(UpdateCheckFailure.NoConnection, "GitHub no contestó a tiempo", ex);
        }
    }
}
