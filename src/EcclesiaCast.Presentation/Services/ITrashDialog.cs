namespace EcclesiaCast.Presentation.Services;

/// <summary>«Edición → Papelera…»: what was deleted in the last 30 days, to bring it back.</summary>
public interface ITrashDialog
{
    /// <summary>Shows it and returns when the operator closes it.</summary>
    void Show();
}
