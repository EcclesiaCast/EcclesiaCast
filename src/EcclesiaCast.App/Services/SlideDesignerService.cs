using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.App.Services;

public sealed class SlideDesignerService : ISlideDesigner
{
    public (bool Saved, SlideOverride? Result) Edit(string text, SlideTheme theme, SlideOverride? current)
    {
        var window = new SlideDesignerWindow(text, theme, current)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        window.ShowDialog();
        return (window.Saved, window.Result);
    }
}
