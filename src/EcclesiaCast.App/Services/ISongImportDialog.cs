namespace EcclesiaCast.App.Services;

/// <summary>Opens the "bring my songs over from another program" window (ProPresenter, EasyWorship).</summary>
public interface ISongImportDialog
{
    /// <summary>Shows it modally; returns the status line, or null if nothing was imported.</summary>
    string? Show();
}
