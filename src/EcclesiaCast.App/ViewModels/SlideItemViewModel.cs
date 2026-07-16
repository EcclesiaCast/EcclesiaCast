using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.ViewModels;

/// <summary>One card in the slide grid.</summary>
public sealed partial class SlideItemViewModel(int index, string label, SlideContent slide)
    : ObservableObject
{
    public int Index { get; } = index;
    public string Label { get; } = label;
    public SlideContent Slide { get; } = slide;
    public string PreviewText => Slide.MainText;

    [ObservableProperty]
    private bool _isLive;
}
