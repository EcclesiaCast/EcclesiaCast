using System.Windows.Media;

namespace EcclesiaCast.App.Platform;

public sealed class WpfFontCatalog : IFontCatalog
{
    public IReadOnlyList<string> InstalledFamilies() =>
        Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(n => n).ToList();
}
