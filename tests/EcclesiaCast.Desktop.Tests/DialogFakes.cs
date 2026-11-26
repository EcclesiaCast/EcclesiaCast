using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.Desktop.Tests;

internal sealed class MemorySettings : ISettingsStore
{
    private readonly Dictionary<string, string> _values = [];

    public string? Get(string key) => _values.GetValueOrDefault(key);

    public void Set(string key, string value) => _values[key] = value;
}

/// <summary>A shell that only writes down what it was asked to open.</summary>
internal sealed class RecordingShell : IShell
{
    public List<string> Opened { get; } = [];

    public void OpenUrl(string url) => Opened.Add(url);

    public void OpenFolder(string folder) => Opened.Add(folder);

    public void ShowInFolder(string file) => Opened.Add(file);
}
