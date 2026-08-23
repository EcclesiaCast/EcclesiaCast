namespace EcclesiaCast.App.Services;

/// <summary>Opens the window where the church's logos are set up.</summary>
public interface ILogoManagerDialog
{
    /// <summary>Shows it modally; true when something was added, edited or removed.</summary>
    bool Show();
}
