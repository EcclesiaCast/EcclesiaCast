namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>The sizes the «fixed size» box of a picture or a logo offers ready-made.</summary>
public static class FramePresets
{
    /// <summary>The width and height the preset at this place of the list stands for (0 and unknown ones: none).</summary>
    public static (double Width, double Height)? SizeOf(int presetIndex) => presetIndex switch
    {
        1 => (1920, 1080),
        2 => (1280, 720),
        3 => (1440, 1080),
        4 => (1080, 1080),
        5 => (1080, 1920),
        _ => null,
    };
}
