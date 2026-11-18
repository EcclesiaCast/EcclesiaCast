using CommunityToolkit.Mvvm.ComponentModel;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>One numbered button in the chapter grid.</summary>
public sealed partial class ChapterOption(int number) : ObservableObject
{
    public int Number { get; } = number;

    [ObservableProperty]
    private bool _isSelected;
}
