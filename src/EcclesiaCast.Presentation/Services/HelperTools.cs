namespace EcclesiaCast.Presentation.Services;

/// <summary>
/// The file names and download addresses of the helper programs (yt-dlp,
/// the JavaScript runtime it needs for YouTube, ffmpeg), which change from
/// one system to another. <see cref="ToolInstaller"/> and <see cref="YtDlp"/>
/// take them only from here.
/// </summary>
public sealed record HelperTools(
    string YtDlpFile,
    string YtDlpUrl,
    string DenoFile,
    string DenoUrl,
    string FfmpegFile,
    string FfprobeFile)
{
    /// <summary>The ones of this system. For now, only Windows.</summary>
    public static HelperTools Current { get; } = Windows;

    public static HelperTools Windows => new(
        YtDlpFile: "yt-dlp.exe",
        // The official yt-dlp release; "latest/download" always serves the newest.
        YtDlpUrl: "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe",
        // A single executable inside a zip, under this same name.
        DenoFile: "deno.exe",
        DenoUrl: "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip",
        FfmpegFile: "ffmpeg.exe",
        FfprobeFile: "ffprobe.exe");
}
