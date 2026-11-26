namespace EcclesiaCast.Presentation.Services;

/// <summary>Opens the "bring my songs over from another program" window (ProPresenter, EasyWorship, Holyrics).</summary>
public interface ISongImportDialog
{
    /// <summary>Shows it modally; returns the status line, or null if nothing was imported.</summary>
    string? Show();
}
