namespace EcclesiaCast.Core.Remote;

/// <summary>The four-digit PIN of the phone remote: making one, and telling a usable one from a damaged one.</summary>
public static class RemotePin
{
    /// <summary>True when <paramref name="pin"/> is exactly four digits.</summary>
    public static bool IsValid([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? pin) =>
        pin is { Length: 4 } && pin.All(char.IsAsciiDigit);

    /// <summary>
    /// A new PIN (1000–9999), never equal to <paramref name="current"/>: asking
    /// for a new one and getting the same would look like the button did nothing.
    /// </summary>
    public static string Generate(string? current = null, Random? random = null)
    {
        random ??= Random.Shared;
        string pin;
        do
        {
            pin = random.Next(1000, 10000).ToString();
        }
        while (pin == current);
        return pin;
    }
}
