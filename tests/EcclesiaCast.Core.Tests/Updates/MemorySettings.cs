using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Core.Tests.Updates;

/// <summary>Settings kept in a dictionary, for tests that need no database.</summary>
internal sealed class MemorySettings : ISettingsStore
{
    public Dictionary<string, string> Values { get; } = [];

    public string? Get(string key) => Values.GetValueOrDefault(key);

    public void Set(string key, string value) => Values[key] = value;
}
