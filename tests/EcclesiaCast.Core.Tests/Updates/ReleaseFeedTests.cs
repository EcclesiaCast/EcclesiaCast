using EcclesiaCast.Core.Updates;

namespace EcclesiaCast.Core.Tests.Updates;

public class ReleaseFeedTests : IDisposable
{
    private readonly List<IDisposable> _disposables = [];

    public void Dispose()
    {
        foreach (var item in _disposables)
            item.Dispose();
    }

    private LocalHttpServer Serve(Func<string, ServerResponse> handler)
    {
        var server = new LocalHttpServer(handler);
        _disposables.Add(server);
        return server;
    }

    private ReleaseFeed Feed(LocalHttpServer server, TimeSpan? timeout = null)
    {
        var client = ReleaseFeed.CreateClient("1.0.0-beta.7");
        if (timeout is not null)
            client.Timeout = timeout.Value;
        _disposables.Add(client);
        return new ReleaseFeed(client, server.BaseUrl + "/releases");
    }

    private static string Sample() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Updates", "releases-sample.json"));

    [Fact]
    public async Task It_reads_the_list_the_server_sends()
    {
        var server = Serve(_ => ServerResponse.Ok(Sample()));

        var releases = await Feed(server).FetchAsync();

        Assert.Equal(4, releases.Count);
        Assert.Equal("v1.0.0", releases[1].Tag);
    }

    [Fact]
    public async Task It_says_who_is_asking_as_github_requires()
    {
        var server = Serve(_ => ServerResponse.Ok("[]"));

        await Feed(server).FetchAsync();

        var request = Assert.Single(server.Requests);
        Assert.Contains("User-Agent: EcclesiaCast/1.0.0-beta.7", request);
        Assert.Contains("application/vnd.github+json", request);
        Assert.StartsWith("GET /releases ", request);
    }

    [Fact]
    public void The_client_gives_up_after_fifteen_seconds()
    {
        using var client = ReleaseFeed.CreateClient("1.0.0");

        Assert.Equal(TimeSpan.FromSeconds(15), client.Timeout);
    }

    [Fact]
    public async Task The_limit_of_requests_is_a_failure_that_is_not_a_missing_connection()
    {
        var server = Serve(_ => ServerResponse.Status403());

        var ex = await Assert.ThrowsAsync<UpdateCheckException>(() => Feed(server).FetchAsync());

        Assert.Equal(UpdateCheckFailure.Unexpected, ex.Failure);
        Assert.Contains("límite", ex.Message);
    }

    [Fact]
    public async Task A_server_error_is_a_failure()
    {
        var server = Serve(_ => new ServerResponse(500, []));

        var ex = await Assert.ThrowsAsync<UpdateCheckException>(() => Feed(server).FetchAsync());

        Assert.Equal(UpdateCheckFailure.Unexpected, ex.Failure);
    }

    [Fact]
    public async Task An_answer_that_is_not_json_is_a_failure()
    {
        var server = Serve(_ => ServerResponse.Ok("<html>Bad gateway</html>"));

        var ex = await Assert.ThrowsAsync<UpdateCheckException>(() => Feed(server).FetchAsync());

        Assert.Equal(UpdateCheckFailure.Unexpected, ex.Failure);
    }

    [Fact]
    public async Task No_server_at_all_means_no_connection()
    {
        var server = Serve(_ => ServerResponse.Ok("[]"));
        var feed = Feed(server);
        server.Dispose();

        var ex = await Assert.ThrowsAsync<UpdateCheckException>(() => feed.FetchAsync());

        Assert.Equal(UpdateCheckFailure.NoConnection, ex.Failure);
    }

    [Fact]
    public async Task A_server_that_does_not_answer_in_time_means_no_connection()
    {
        var server = Serve(_ => new ServerResponse(200, [], DeclaredLength: 100, HangAfterBody: true));

        var ex = await Assert.ThrowsAsync<UpdateCheckException>(
            () => Feed(server, timeout: TimeSpan.FromMilliseconds(500)).FetchAsync());

        Assert.Equal(UpdateCheckFailure.NoConnection, ex.Failure);
    }

    [Fact]
    public async Task Cancelling_is_not_reported_as_a_failure()
    {
        var server = Serve(_ => new ServerResponse(200, [], DeclaredLength: 100, HangAfterBody: true));
        using var cancel = new CancellationTokenSource(300);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Feed(server).FetchAsync(cancel.Token));
    }
}
