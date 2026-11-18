using EcclesiaCast.Core.IO;

namespace EcclesiaCast.Core.Tests.IO;

public class FileNameRulesTests
{
    [Fact]
    public void The_list_is_the_one_windows_uses_whatever_the_system()
    {
        char[] expected = [.. Enumerable.Range(0, 32).Select(i => (char)i), '"', '*', '/', ':', '<', '>', '?', '\\', '|'];

        Assert.Equal(expected.Order(), FileNameRules.InvalidChars.Order());
    }

    [Fact]
    public void On_windows_it_matches_what_the_system_says()
    {
        if (!OperatingSystem.IsWindows())
            return;

        Assert.Equal(Path.GetInvalidFileNameChars().Order(), FileNameRules.InvalidChars.Order());
    }

    [Theory]
    [InlineData("Cruz de madera.jpg", false)]
    [InlineData("Canción · Pixabay 3.mp4", false)]
    [InlineData("a|b.exe", true)]
    [InlineData("a:b", true)]
    [InlineData("a?b", true)]
    [InlineData("a*b", true)]
    [InlineData("a\\b", true)]
    [InlineData("a/b", true)]
    [InlineData("a\"b", true)]
    [InlineData("a<b>", true)]
    [InlineData("a\tb", true)]
    public void A_name_with_a_forbidden_character_is_told_apart(string name, bool hasInvalid)
    {
        Assert.Equal(hasInvalid, FileNameRules.HasInvalid(name));
    }
}
