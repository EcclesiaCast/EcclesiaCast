namespace EcclesiaCast.Presentation.Services;

/// <summary>How the window that makes a light copy of a video ended.</summary>
public enum LightCopyResult
{
    /// <summary>The copy is ready.</summary>
    Done,

    /// <summary>The operator cancelled.</summary>
    Cancelled,

    /// <summary>The copy could not be made (the original is projected).</summary>
    Failed,
}

/// <summary>The window that makes the light copy of a heavy video while the operator waits.</summary>
public interface ILightCopyDialog
{
    /// <summary>Makes the copy of <paramref name="path"/> and returns when the window closes.</summary>
    LightCopyResult Run(string path, string name);
}
