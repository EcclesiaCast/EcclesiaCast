namespace EcclesiaCast.Presentation.Services;

/// <summary>«Cómo preparar una Biblia para importar…»: the two formats and the instructions for an AI.</summary>
public interface IBibleFormatHelpDialog
{
    /// <summary>Shows it and returns when it closes: true when the operator asked to go on to the import.</summary>
    bool Show();
}
