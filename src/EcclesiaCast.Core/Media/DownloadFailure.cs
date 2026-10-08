namespace EcclesiaCast.Core.Media;

/// <summary>What went wrong in a yt-dlp download, as far as the operator can act on it.</summary>
public enum DownloadFailureKind
{
    Other,
    /// <summary>HTTP 403: YouTube changed and the downloader is out of date.</summary>
    Forbidden,
    /// <summary>The video wants a signed-in account (age, members, bot check).</summary>
    SignInRequired,
    /// <summary>Private, removed or blocked in this country.</summary>
    Unavailable,
    /// <summary>YouTube's JavaScript challenge couldn't be solved (no runtime, or too old).</summary>
    Challenge,
    /// <summary>No connection, or the connection dropped.</summary>
    Network,
}

/// <summary>Sorts yt-dlp's English complaints into <see cref="DownloadFailureKind"/>.</summary>
public static class DownloadFailure
{
    public static DownloadFailureKind Classify(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return DownloadFailureKind.Other;

        if (error.Contains("getaddrinfo", StringComparison.OrdinalIgnoreCase)
            || error.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || error.Contains("Network is unreachable", StringComparison.OrdinalIgnoreCase)
            || error.Contains("Temporary failure in name resolution", StringComparison.OrdinalIgnoreCase)
            || error.Contains("Connection reset", StringComparison.OrdinalIgnoreCase)
            || error.Contains("Unable to connect", StringComparison.OrdinalIgnoreCase))
            return DownloadFailureKind.Network;

        if (error.Contains("403", StringComparison.Ordinal)
            || error.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
            return DownloadFailureKind.Forbidden;

        if (error.Contains("Private video", StringComparison.OrdinalIgnoreCase)
            || error.Contains("Video unavailable", StringComparison.OrdinalIgnoreCase)
            || error.Contains("has been removed", StringComparison.OrdinalIgnoreCase)
            || error.Contains("not available in your country", StringComparison.OrdinalIgnoreCase)
            || error.Contains("blocked it", StringComparison.OrdinalIgnoreCase))
            return DownloadFailureKind.Unavailable;

        if (error.Contains("Sign in", StringComparison.OrdinalIgnoreCase)
            || error.Contains("confirm your age", StringComparison.OrdinalIgnoreCase)
            || error.Contains("not a bot", StringComparison.OrdinalIgnoreCase)
            || error.Contains("age-restricted", StringComparison.OrdinalIgnoreCase))
            return DownloadFailureKind.SignInRequired;

        if (error.Contains("JavaScript runtime", StringComparison.OrdinalIgnoreCase)
            || error.Contains("n challenge", StringComparison.OrdinalIgnoreCase)
            || error.Contains("challenge solving", StringComparison.OrdinalIgnoreCase)
            || error.Contains("Requested format is not available", StringComparison.OrdinalIgnoreCase))
            return DownloadFailureKind.Challenge;

        return DownloadFailureKind.Other;
    }
}

/// <summary>Decides when the bundled downloader is checked for a newer version.</summary>
public static class ToolUpdatePolicy
{
    public static readonly TimeSpan Interval = TimeSpan.FromDays(3);

    /// <summary>True when the last check is older than <see cref="Interval"/> or never happened.</summary>
    public static bool IsDue(DateTimeOffset? lastCheck, DateTimeOffset now) =>
        lastCheck is not { } last || now - last >= Interval || last > now.AddDays(1);
}
