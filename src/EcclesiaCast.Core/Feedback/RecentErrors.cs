namespace EcclesiaCast.Core.Feedback;

/// <summary>The latest error lines of the log, cleaned of the person's folder names, for the usage summary.</summary>
public static class RecentErrors
{
    public static IReadOnlyList<string> Read(string logsFolder, string userProfile, int max = 5)
    {
        var found = new List<string>();
        try
        {
            if (!Directory.Exists(logsFolder))
                return found;

            // Newest day first; the file name carries the date.
            foreach (var path in Directory.GetFiles(logsFolder, "ecclesiacast-*.log")
                         .OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase).Take(3))
            {
                var lines = new List<string>();
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    while (reader.ReadLine() is { } line)
                        lines.Add(line);
                }

                for (var i = lines.Count - 1; i >= 0 && found.Count < max; i--)
                {
                    if (lines[i].Contains("[ERR]", StringComparison.Ordinal))
                        found.Add(Clean(lines[i], userProfile));
                }

                if (found.Count >= max)
                    break;
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        return found;
    }

    private static string Clean(string line, string userProfile)
    {
        if (!string.IsNullOrEmpty(userProfile))
            line = line.Replace(userProfile, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
        return line.Length <= 300 ? line : line[..300] + "…";
    }
}
