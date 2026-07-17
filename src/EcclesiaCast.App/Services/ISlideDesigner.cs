using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.App.Services;

/// <summary>Opens the per-slide design window.</summary>
public interface ISlideDesigner
{
    /// <summary>Returns (false, _) if cancelled; (true, null) means "reset to theme".</summary>
    (bool Saved, SlideOverride? Result) Edit(string text, SlideTheme theme, SlideOverride? current);
}
