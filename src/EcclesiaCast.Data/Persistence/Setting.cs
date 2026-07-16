namespace EcclesiaCast.Data.Persistence;

/// <summary>A single persisted key-value setting.</summary>
public sealed class Setting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
