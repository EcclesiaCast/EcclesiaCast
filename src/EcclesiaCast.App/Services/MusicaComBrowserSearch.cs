using System.Text.Json;
using EcclesiaCast.Core.Songs.Web;
using Microsoft.Web.WebView2.Core;

namespace EcclesiaCast.App.Services;

/// <summary>
/// The full search of musica.com, the one that finds a song by a line of its
/// lyrics. The site hands it to Google's custom search, which only runs
/// inside a real browser, so this drives an invisible WebView2 to the search
/// page and reads the links once they appear.
/// </summary>
public sealed class MusicaComBrowserSearch(IntPtr ownerWindow) : IDisposable
{
    /// <summary>
    /// Lists the result links once Google has drawn them, and the query of
    /// the page it read: until the new page loads, the previous search is
    /// still there.
    /// </summary>
    private const string ReadResults = """
        (() => {
            const query = new URLSearchParams(location.search).get('q');
            const links = [...document.querySelectorAll('a.gs-title')]
                .map(a => [a.href, a.textContent.trim()])
                .filter(l => /letras\.asp\?letra=\d+/.test(l[0]));
            const empty = !!document.querySelector('.gs-no-results-result, .gs-snippet-noResult');
            return { query, links, empty };
        })()
        """;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan PollEvery = TimeSpan.FromMilliseconds(300);

    private CoreWebView2Controller? _controller;

    public async Task<IReadOnlyList<WebSongHit>> SearchAsync(string query, CancellationToken cancel)
    {
        var web = await EnsureBrowserAsync();
        web.Navigate(MusicaComParser.FullSearchUrl(query));

        var started = DateTime.UtcNow;
        var lastCount = -1;

        while (DateTime.UtcNow - started < Timeout)
        {
            await Task.Delay(PollEvery, cancel);

            var json = await web.ExecuteScriptAsync(ReadResults);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || doc.RootElement.GetProperty("query").GetString() != query)
            {
                continue;
            }

            var links = doc.RootElement.GetProperty("links");
            if (doc.RootElement.GetProperty("empty").GetBoolean() && links.GetArrayLength() == 0)
                return [];

            // Google draws the results in one go, but wait for one more look
            // with the same count before trusting the list is complete.
            var count = links.GetArrayLength();
            if (count > 0 && count == lastCount)
                return Parse(links);
            lastCount = count;
        }

        return [];
    }

    private static List<WebSongHit> Parse(JsonElement links)
    {
        var hits = new List<WebSongHit>();
        foreach (var link in links.EnumerateArray())
        {
            var hit = MusicaComParser.ParseFullSearchResult(
                link[0].GetString() ?? "", link[1].GetString() ?? "");
            if (hit is not null && hits.All(h => h.Url != hit.Url))
                hits.Add(hit);
        }
        return hits;
    }

    private async Task<CoreWebView2> EnsureBrowserAsync()
    {
        if (_controller is not null)
            return _controller.CoreWebView2;

        var environment = await WebViewProfile.GetEnvironmentAsync();
        _controller = await environment.CreateCoreWebView2ControllerAsync(ownerWindow);
        _controller.IsVisible = false;

        var web = _controller.CoreWebView2;
        web.IsMuted = true;
        web.Settings.AreDevToolsEnabled = false;
        web.Settings.AreDefaultScriptDialogsEnabled = false;
        web.NewWindowRequested += (_, e) => e.Handled = true;

        // Only the result links matter: skip the pictures, videos and fonts
        // of the page and its ads, which are most of what it would download.
        web.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.Image);
        web.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.Media);
        web.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.Font);
        web.WebResourceRequested += (_, e) =>
            e.Response = environment.CreateWebResourceResponse(null, 204, "No Content", "");

        return web;
    }

    public void Dispose()
    {
        _controller?.Close();
        _controller = null;
    }
}
