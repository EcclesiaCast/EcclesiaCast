using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// One version in the Bible versions row. Selected means active for
/// projection (up to two at a time, like BibleShow); the first one drives the
/// text and the second shows below it.
/// </summary>
public sealed partial class BibleVersionOption(BibleVersionInfo info) : ObservableObject
{
    public BibleVersionInfo Info { get; } = info;

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>The first active version (the main text).</summary>
    [ObservableProperty]
    private bool _isPrimary;

    /// <summary>The second active version, shown below the main one.</summary>
    [ObservableProperty]
    private bool _isSecondary;
}
