using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

public sealed class BibleImportViewModelTests
{
    [Theory]
    [InlineData(" ", "X", BibleImportField.Name, "El nombre es obligatorio.")]
    [InlineData("", "", BibleImportField.Name, "El nombre es obligatorio.")]
    [InlineData("Biblia", "  ", BibleImportField.Abbreviation, "La abreviatura es obligatoria.")]
    public void A_name_and_an_abbreviation_are_needed(string name, string abbreviation, BibleImportField field, string error)
    {
        var viewModel = new BibleImportViewModel("Mi Biblia");

        Assert.Equal(field, viewModel.TryAccept(name, abbreviation, "es"));
        Assert.Equal(error, viewModel.Error);
        Assert.Null(viewModel.Result);
    }

    [Theory]
    [InlineData("  ", "es")]
    [InlineData(null, "es")]
    [InlineData(" en ", "en")]
    public void An_empty_language_is_spanish_and_everything_is_trimmed(string? language, string expected)
    {
        var viewModel = new BibleImportViewModel("Mi Biblia");

        Assert.Null(viewModel.TryAccept(" Biblia ", " B ", language));
        Assert.Equal(new BibleImportMetadata("Biblia", "B", expected), viewModel.Result);
    }
}
