using System.Globalization;
using System.Text;

namespace EcclesiaCast.Core.Audio;

/// <summary>Moving things up and down a list and sorting names the way a person reads them.</summary>
public static class ListOrder
{
    /// <summary>
    /// Moves the item at <paramref name="index"/> by <paramref name="delta"/>
    /// places (negative goes up). Returns where it ended up, or -1 when it
    /// would fall off either end and nothing moved.
    /// </summary>
    public static int Move<T>(IList<T> list, int index, int delta)
    {
        var target = index + delta;
        if (index < 0 || index >= list.Count || target < 0 || target >= list.Count || delta == 0)
            return -1;

        (list[index], list[target]) = (list[target], list[index]);
        return target;
    }

    /// <summary>
    /// Compares names ignoring case and accents, with the numbers inside them
    /// by value: «Canción 2» goes before «Canción 10», which the plain
    /// alphabetical order gets the wrong way round.
    /// </summary>
    public static int CompareNatural(string? a, string? b)
    {
        a = Fold(a);
        b = Fold(b);

        int i = 0, j = 0;
        while (i < a.Length && j < b.Length)
        {
            if (char.IsDigit(a[i]) && char.IsDigit(b[j]))
            {
                var startA = i;
                var startB = j;
                while (i < a.Length && char.IsDigit(a[i]))
                    i++;
                while (j < b.Length && char.IsDigit(b[j]))
                    j++;

                // By value: compare without the leading zeros, then by length.
                var numberA = a[startA..i].TrimStart('0');
                var numberB = b[startB..j].TrimStart('0');
                if (numberA.Length != numberB.Length)
                    return numberA.Length < numberB.Length ? -1 : 1;

                var byDigits = string.CompareOrdinal(numberA, numberB);
                if (byDigits != 0)
                    return byDigits < 0 ? -1 : 1;
            }
            else
            {
                if (a[i] != b[j])
                    return a[i] < b[j] ? -1 : 1;
                i++;
                j++;
            }
        }

        return (a.Length - i).CompareTo(b.Length - j);
    }

    /// <summary>Lower case and without accents, so «Árbol» sits with the A's.</summary>
    private static string Fold(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }
}
