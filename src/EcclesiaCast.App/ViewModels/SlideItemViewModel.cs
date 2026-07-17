using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.ViewModels;

/// <summary>One card in the slide grid.</summary>
public sealed partial class SlideItemViewModel(
    int index, string label, SlideContent slide,
    BibleReference? jumpTarget = null, bool jumpToEnd = false)
    : ObservableObject
{
    public int Index { get; } = index;
    public string Label { get; } = label;
    public SlideContent Slide { get; } = slide;

    /// <summary>
    /// When set, activating this card doesn't project it: it loads this
    /// passage instead (the previous/next chapter cards).
    /// </summary>
    public BibleReference? JumpTarget { get; } = jumpTarget;

    /// <summary>
    /// True on the "previous chapter" card: after the jump, go live on the
    /// last verse instead of the first, so backward reading is continuous.
    /// </summary>
    public bool JumpToEnd { get; } = jumpToEnd;

    public string PreviewText => Slide.MainText;

    [ObservableProperty]
    private bool _isLive;

    /// <summary>Selected as "next up" (e.g. the verse a reference points to), not live yet.</summary>
    [ObservableProperty]
    private bool _isPreviewed;
}
