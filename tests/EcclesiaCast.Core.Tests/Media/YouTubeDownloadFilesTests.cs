using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class YouTubeDownloadFilesTests
{
    private const string Folder = @"C:\datos\youtube";

    [Fact]
    public void A_video_downloaded_before_is_found_by_its_id()
    {
        var files = new[]
        {
            Path.Combine(Folder, "Otro video [abc123XYZ_-].mp4"),
            Path.Combine(Folder, "Rescate - El legado [DctG4SabK9k].mp4"),
        };

        Assert.Equal(files[1], YouTubeDownloadFiles.AlreadyDownloaded(files, "DctG4SabK9k"));
    }

    [Fact]
    public void Half_downloads_and_unjoined_pieces_are_not_the_video()
    {
        var files = new[]
        {
            Path.Combine(Folder, "Rescate [DctG4SabK9k].mp4.part"),
            Path.Combine(Folder, "Rescate [DctG4SabK9k].f137.mp4"),
            Path.Combine(Folder, "Rescate [DctG4SabK9k].f140.m4a"),
            Path.Combine(Folder, "Rescate [DctG4SabK9k].mp4.ytdl"),
        };

        Assert.Null(YouTubeDownloadFiles.AlreadyDownloaded(files, "DctG4SabK9k"));
    }

    [Fact]
    public void Another_video_with_a_similar_id_is_not_taken()
    {
        var files = new[] { Path.Combine(Folder, "Algo [XDctG4SabK9k].mp4") };

        Assert.Null(YouTubeDownloadFiles.AlreadyDownloaded(files, "DctG4SabK9k"));
        Assert.Null(YouTubeDownloadFiles.AlreadyDownloaded(files, ""));
    }
}
