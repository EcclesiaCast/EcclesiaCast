using EcclesiaCast.Core.Capture;

namespace EcclesiaCast.Core.Tests.Capture;

public class WindowTargetTests
{
    private static RawWindow Win(long handle, string process, string title) =>
        new(handle, ProcessId: 100 + (int)handle, ProcessName: process, Title: title);

    [Fact]
    public void Writes_the_program_and_the_title_with_a_bar_between()
    {
        Assert.Equal("explorer.exe|Descargas", WindowTarget.Format("explorer.exe", "Descargas"));
    }

    [Fact]
    public void Reads_back_what_it_wrote()
    {
        Assert.True(WindowTarget.TryParse(WindowTarget.Format("winword.exe", "Informe - Word"), out var target));

        Assert.Equal("winword.exe", target.Process);
        Assert.Equal("Informe - Word", target.Title);
    }

    [Fact]
    public void A_title_that_has_a_bar_inside_keeps_it()
    {
        Assert.True(WindowTarget.TryParse("chrome.exe|Antes | Después", out var target));

        Assert.Equal("chrome.exe", target.Process);
        Assert.Equal("Antes | Después", target.Title);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sin barra")]
    [InlineData("|solo título")]
    public void Text_without_a_program_is_not_a_target(string? text)
    {
        Assert.False(WindowTarget.TryParse(text, out _));
    }

    [Fact]
    public void A_title_may_be_empty_after_the_bar()
    {
        Assert.True(WindowTarget.TryParse("notepad.exe|", out var target));

        Assert.Equal(string.Empty, target.Title);
    }

    [Fact]
    public void Prefers_the_same_program_and_the_same_title()
    {
        var windows = new[]
        {
            Win(1, "winword.exe", "Otro - Word"),
            Win(2, "winword.exe", "Informe - Word"),
            Win(3, "excel.exe", "Informe - Word"),
        };

        var best = WindowTarget.Best(windows, new WindowTarget("winword.exe", "Informe - Word"));

        Assert.Equal(2, best?.Handle);
    }

    [Fact]
    public void The_program_name_is_compared_without_mind_to_capitals()
    {
        var best = WindowTarget.Best([Win(1, "Explorer.EXE", "Descargas")], new WindowTarget("explorer.exe", "Descargas"));

        Assert.Equal(1, best?.Handle);
    }

    [Fact]
    public void Falls_back_to_a_title_that_contains_the_saved_one()
    {
        var windows = new[]
        {
            Win(1, "winword.exe", "Boletín - Word"),
            Win(2, "winword.exe", "Informe - Word [modo de compatibilidad]"),
        };

        var best = WindowTarget.Best(windows, new WindowTarget("winword.exe", "Informe - Word"));

        Assert.Equal(2, best?.Handle);
    }

    [Fact]
    public void Falls_back_to_the_only_window_of_the_program()
    {
        var windows = new[]
        {
            Win(1, "powerpnt.exe", "Presentación1 - PowerPoint"),
            Win(2, "excel.exe", "Libro1"),
        };

        var best = WindowTarget.Best(windows, new WindowTarget("powerpnt.exe", "Otro nombre"));

        Assert.Equal(1, best?.Handle);
    }

    [Fact]
    public void Gives_up_when_the_program_has_several_windows_and_none_matches()
    {
        var windows = new[]
        {
            Win(1, "winword.exe", "Uno - Word"),
            Win(2, "winword.exe", "Dos - Word"),
        };

        Assert.Null(WindowTarget.Best(windows, new WindowTarget("winword.exe", "Tres - Word")));
    }

    [Fact]
    public void Finds_nothing_when_the_program_is_not_open()
    {
        Assert.Null(WindowTarget.Best([Win(1, "excel.exe", "Libro1")], new WindowTarget("winword.exe", "Informe")));
        Assert.Null(WindowTarget.Best([], new WindowTarget("winword.exe", "Informe")));
    }
}
