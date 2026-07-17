using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// One row in the Bible versions list. Its checkbox marks the version as
/// active for projection (up to two at a time, like BibleShow).
/// </summary>
public sealed partial class BibleVersionOption(BibleVersionInfo info) : ObservableObject
{
    public BibleVersionInfo Info { get; } = info;

    [ObservableProperty]
    private bool _isSelected;
}
