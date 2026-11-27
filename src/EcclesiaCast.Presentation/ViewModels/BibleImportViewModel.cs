using CommunityToolkit.Mvvm.ComponentModel;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>The boxes of «Importar Biblia» that cannot be left empty.</summary>
public enum BibleImportField { Name, Abbreviation }

/// <summary>
/// What «Importar Biblia» asks of a new version: a name and an abbreviation
/// are needed, and a language left empty is Spanish.
/// </summary>
public sealed partial class BibleImportViewModel(string suggestedName) : ObservableObject
{
    public const string DefaultLanguage = "es";

    public string SuggestedName { get; } = suggestedName;

    /// <summary>Why the last <see cref="TryAccept"/> did not accept; empty before.</summary>
    [ObservableProperty]
    private string _error = string.Empty;

    /// <summary>What was accepted; null until <see cref="TryAccept"/> does.</summary>
    public BibleImportMetadata? Result { get; private set; }

    /// <summary>
    /// Returns null when the boxes will do (see <see cref="Result"/>);
    /// otherwise the box to put the cursor in, and <see cref="Error"/> says why.
    /// </summary>
    public BibleImportField? TryAccept(string? nameText, string? abbreviationText, string? languageText)
    {
        var name = (nameText ?? string.Empty).Trim();
        var abbreviation = (abbreviationText ?? string.Empty).Trim();

        if (name.Length == 0)
        {
            Error = "El nombre es obligatorio.";
            return BibleImportField.Name;
        }

        if (abbreviation.Length == 0)
        {
            Error = "La abreviatura es obligatoria.";
            return BibleImportField.Abbreviation;
        }

        Result = new BibleImportMetadata(name, abbreviation,
            string.IsNullOrWhiteSpace(languageText) ? DefaultLanguage : languageText.Trim());
        return null;
    }
}
