using System.Windows;

namespace EcclesiaCast.App.Platform;

public sealed class WpfClipboardText : IClipboardText
{
    public void SetText(string text) => Clipboard.SetText(text);
}
