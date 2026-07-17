using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.ViewModels;

/// <summary>One card in the slide grid.</summary>
public sealed partial class SlideItemViewModel(
    int index, string label, SlideContent slide, BibleReference? jumpTarget = null)
    : ObservableObject
{
    public int Index { get; } = index;
    public string Label { get; } = label;
    public SlideContent Slide { get; } = slide;

    /// <summary>
    /// When set, activating this card doesn't project it: it loads this
    /// passage instead (the "next chapter" card at the end of a chapter).
    /// </summary>
    public BibleReference? JumpTarget { get; } = jumpTarget;

    public string PreviewText => Slide.MainText;

    [ObservableProperty]
    private bool _isLive;

    /// <summary>Selected as "next up" (e.g. the verse a reference points to), not live yet.</summary>
    [ObservableProperty]
    private bool _isPreviewed;
}
