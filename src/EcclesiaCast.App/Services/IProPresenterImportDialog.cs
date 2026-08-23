namespace EcclesiaCast.App.Services;

/// <summary>Opens the "bring my songs over from ProPresenter" window.</summary>
public interface IProPresenterImportDialog
{
    /// <summary>Shows it modally; returns the status line, or null if nothing was imported.</summary>
    string? Show();
}
