using Avalonia.Headless.XUnit;
using EcclesiaCast.Desktop.Services;
using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// The file dialogs, as far as they go without opening one: what the system
/// is asked for and how the answer is waited for.
/// </summary>
public sealed class DialogsTests
{
    private static readonly FileFilter[] Filters =
    [
        new("Biblias (*.json, *.xml)", ["*.json", "*.xml"]),
        new("XML (Zefania u OpenSong)", ["*.xml"]),
    ];

    [Fact]
    public void The_type_list_keeps_names_patterns_and_order()
    {
        var types = AvaloniaDialogs.FileTypes(Filters);

        Assert.Equal(["Biblias (*.json, *.xml)", "XML (Zefania u OpenSong)"], types.Select(t => t.Name));
        Assert.Equal(["*.json", "*.xml"], types[0].Patterns!);
        Assert.Equal(["*.xml"], types[1].Patterns!);
    }

    [Fact]
    public void Opening_asks_for_one_file_or_for_several()
    {
        var one = AvaloniaDialogs.OpenOptions(new OpenFileRequest("Importar Biblia", Filters), several: false);
        var many = AvaloniaDialogs.OpenOptions(new OpenFileRequest("Agregar audios", Filters), several: true);

        Assert.Equal("Importar Biblia", one.Title);
        Assert.False(one.AllowMultiple);
        Assert.True(many.AllowMultiple);
        Assert.Equal(2, one.FileTypeFilter!.Count);
        // As in WPF, the system chooses where to start.
        Assert.Null(one.SuggestedStartLocation);
    }

    [Fact]
    public void Saving_suggests_the_name_and_adds_the_extension()
    {
        var options = AvaloniaDialogs.SaveOptions(new SaveFileRequest("Exportar", Filters)
        {
            FileName = "Reina Valera.json",
            DefaultExtension = ".json",
        });

        Assert.Equal("Reina Valera.json", options.SuggestedFileName);
        Assert.Equal("json", options.DefaultExtension);
        Assert.True(options.ShowOverwritePrompt);
    }

    [Fact]
    public void Without_an_extension_asked_for_the_first_filter_gives_it()
    {
        var request = new SaveFileRequest("Exportar", [new FileFilter("Paquete", ["*.ecbiblias"])]);

        Assert.Equal("ecbiblias", AvaloniaDialogs.Extension(request));
        Assert.Null(AvaloniaDialogs.Extension(request with { AddExtension = false }));
        Assert.Null(AvaloniaDialogs.Extension(new SaveFileRequest("Guardar", [new FileFilter("Todos", ["*.*"])])));
    }

    [Fact]
    public void A_folder_is_asked_for_one_at_a_time()
    {
        var options = AvaloniaDialogs.FolderOptions("Elegí una carpeta");

        Assert.Equal("Elegí una carpeta", options.Title);
        Assert.False(options.AllowMultiple);
    }

    [AvaloniaFact]
    public void The_answer_that_comes_later_is_waited_for()
    {
        var later = Task.Run(async () =>
        {
            await Task.Delay(50);
            return "elegido";
        });

        Assert.Equal("elegido", AvaloniaDialogs.Wait(later));
        Assert.Equal("ya", AvaloniaDialogs.Wait(Task.FromResult("ya")));
    }

    [AvaloniaFact]
    public void Without_a_window_every_dialog_is_a_cancel()
    {
        var dialogs = new AvaloniaDialogs();

        Assert.Null(dialogs.OpenFile(new OpenFileRequest("Elegir", Filters)));
        Assert.Null(dialogs.OpenFiles(new OpenFileRequest("Elegir", Filters)));
        Assert.Null(dialogs.SaveFile(new SaveFileRequest("Guardar", Filters)));
        Assert.Null(dialogs.PickFolder("Carpeta"));
    }
}
