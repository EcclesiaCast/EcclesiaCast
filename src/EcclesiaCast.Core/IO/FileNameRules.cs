namespace EcclesiaCast.Core.IO;

/// <summary>
/// What a file name may not contain. It is the list Windows uses, on every
/// system: <see cref="Path.GetInvalidFileNameChars"/> on a Mac only forbids
/// «/», so a name made there would carry characters (« : * ? " &lt; &gt; | »)
/// that break as soon as the file reaches a Windows computer.
/// </summary>
public static class FileNameRules
{
    /// <summary>The characters Windows does not allow in a file name.</summary>
    public static readonly IReadOnlyList<char> InvalidChars =
        [.. Enumerable.Range(0, 128).Select(i => (char)i).Where(IsInvalid)];

    /// <summary>True when the character cannot be part of a file name.</summary>
    public static bool IsInvalid(char c) => c < ' ' || c is '"' or '*' or '/' or ':' or '<' or '>' or '?' or '\\' or '|';

    /// <summary>True when the name has any character that is not allowed.</summary>
    public static bool HasInvalid(string name) => name.Any(IsInvalid);
}
