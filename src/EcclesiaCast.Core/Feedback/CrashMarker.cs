namespace EcclesiaCast.Core.Feedback;

/// <summary>
/// A file that exists while the program is open. If it is still there at the
/// next start, the last run did not end on its own (a crash, a freeze, a
/// forced close) and the person can be asked whether to tell us.
/// </summary>
public sealed class CrashMarker(string folder)
{
    private string FilePath => Path.Combine(folder, "en-uso.marca");

    /// <summary>Marks this run as open. True when the previous run left its mark behind.</summary>
    public bool Begin()
    {
        try
        {
            Directory.CreateDirectory(folder);
            var crashed = File.Exists(FilePath);
            File.WriteAllText(FilePath, DateTime.Now.ToString("O"));
            return crashed;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public void End()
    {
        try { File.Delete(FilePath); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
