using System.Globalization;

namespace EcclesiaCast.Core.Updates;

/// <summary>
/// A version such as 1.0.0-beta.7, ordered the way SemVer 2.0 says. The part
/// after a hyphen is a pre-release: 1.0.0-beta.10 comes after 1.0.0-beta.9
/// because its identifiers are compared as numbers, not as text, and any
/// pre-release comes before the plain 1.0.0 it leads up to.
///
/// The release tags are written "v1.0.0", so a leading v is accepted, and the
/// build metadata after a plus (1.0.0+abc123) is dropped: SemVer says it takes
/// no part in the order.
/// </summary>
public sealed class AppVersion : IComparable<AppVersion>, IEquatable<AppVersion>
{
    private readonly ulong _major;
    private readonly ulong _minor;
    private readonly ulong _patch;
    private readonly string[] _preRelease;

    private AppVersion(ulong major, ulong minor, ulong patch, string[] preRelease)
    {
        _major = major;
        _minor = minor;
        _patch = patch;
        _preRelease = preRelease;
    }

    /// <summary>True for 1.0.0-beta.7 and 1.0.0-rc.1; false for 1.0.0.</summary>
    public bool IsPreRelease => _preRelease.Length > 0;

    /// <summary>
    /// Reads a version, or returns false for anything that is not one (an
    /// empty text, "latest", a tag with letters in the numbers).
    /// </summary>
    public static bool TryParse(string? text, out AppVersion version)
    {
        version = null!;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var span = text.Trim();
        if (span.Length > 0 && (span[0] == 'v' || span[0] == 'V'))
            span = span[1..];

        var plus = span.IndexOf('+');
        if (plus >= 0)
            span = span[..plus];

        var hyphen = span.IndexOf('-');
        var core = hyphen >= 0 ? span[..hyphen] : span;
        var pre = hyphen >= 0 ? span[(hyphen + 1)..] : string.Empty;

        // "1.0" is a version people write; "1" and "1.0.0.0" are not.
        var parts = core.Split('.');
        if (parts.Length is < 2 or > 3)
            return false;

        var numbers = new ulong[3];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!IsDigits(parts[i]) || !ulong.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i]))
                return false;
        }

        string[] identifiers = [];
        if (hyphen >= 0)
        {
            identifiers = pre.Split('.');
            for (var i = 0; i < identifiers.Length; i++)
            {
                var identifier = identifiers[i];
                if (identifier.Length == 0 || !identifier.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                    return false;

                // beta.01 and beta.1 are the same version, so they must also
                // look the same to Equals and GetHashCode.
                if (IsDigits(identifier) && ulong.TryParse(identifier, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                    identifiers[i] = number.ToString(CultureInfo.InvariantCulture);
            }
        }

        version = new AppVersion(numbers[0], numbers[1], numbers[2], identifiers);
        return true;
    }

    /// <summary>The version, or null when the text is not one.</summary>
    public static AppVersion? TryParse(string? text) => TryParse(text, out var version) ? version : null;

    public int CompareTo(AppVersion? other)
    {
        if (other is null)
            return 1;

        var byNumbers = _major.CompareTo(other._major);
        if (byNumbers == 0) byNumbers = _minor.CompareTo(other._minor);
        if (byNumbers == 0) byNumbers = _patch.CompareTo(other._patch);
        if (byNumbers != 0)
            return byNumbers;

        // 1.0.0 is later than any 1.0.0-something.
        if (!IsPreRelease || !other.IsPreRelease)
            return IsPreRelease == other.IsPreRelease ? 0 : IsPreRelease ? -1 : 1;

        var shared = Math.Min(_preRelease.Length, other._preRelease.Length);
        for (var i = 0; i < shared; i++)
        {
            var byIdentifier = CompareIdentifiers(_preRelease[i], other._preRelease[i]);
            if (byIdentifier != 0)
                return byIdentifier;
        }

        // beta is before beta.1: the longer list wins when the rest is equal.
        return _preRelease.Length.CompareTo(other._preRelease.Length);
    }

    public bool Equals(AppVersion? other) => other is not null && CompareTo(other) == 0;

    public override bool Equals(object? obj) => Equals(obj as AppVersion);

    public override int GetHashCode() =>
        HashCode.Combine(_major, _minor, _patch, string.Join('.', _preRelease));

    public static bool operator ==(AppVersion? left, AppVersion? right) => left is null ? right is null : left.Equals(right);

    public static bool operator !=(AppVersion? left, AppVersion? right) => !(left == right);

    public static bool operator >(AppVersion left, AppVersion right) => left.CompareTo(right) > 0;

    public static bool operator <(AppVersion left, AppVersion right) => left.CompareTo(right) < 0;

    public static bool operator >=(AppVersion left, AppVersion right) => left.CompareTo(right) >= 0;

    public static bool operator <=(AppVersion left, AppVersion right) => left.CompareTo(right) <= 0;

    /// <summary>The version as it is shown to people: 1.0.0-beta.7, no v and no build.</summary>
    public override string ToString() =>
        $"{_major}.{_minor}.{_patch}" + (IsPreRelease ? "-" + string.Join('.', _preRelease) : string.Empty);

    private static int CompareIdentifiers(string left, string right)
    {
        var leftIsNumber = IsDigits(left) && ulong.TryParse(left, NumberStyles.None, CultureInfo.InvariantCulture, out _);
        var rightIsNumber = IsDigits(right) && ulong.TryParse(right, NumberStyles.None, CultureInfo.InvariantCulture, out _);

        if (leftIsNumber && rightIsNumber)
            return ulong.Parse(left, CultureInfo.InvariantCulture).CompareTo(ulong.Parse(right, CultureInfo.InvariantCulture));

        // A number is lower than a word (1 < beta); two words go by letters.
        if (leftIsNumber != rightIsNumber)
            return leftIsNumber ? -1 : 1;

        return string.CompareOrdinal(left, right);
    }

    private static bool IsDigits(string text) => text.Length > 0 && text.All(char.IsAsciiDigit);
}
