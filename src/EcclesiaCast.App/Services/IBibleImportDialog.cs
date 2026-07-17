namespace EcclesiaCast.App.Services;

public sealed record BibleImportMetadata(string Name, string Abbreviation, string Language);

/// <summary>Prompts the operator for a new Bible version's name, abbreviation, and language.</summary>
public interface IBibleImportDialog
{
    BibleImportMetadata? Prompt(string suggestedName);
}
