namespace EcclesiaCast.Core.Abstractions;

/// <summary>Key-value persistence for application settings.</summary>
public interface ISettingsStore
{
    string? Get(string key);
    void Set(string key, string value);
}
