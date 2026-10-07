using EcclesiaCast.Core.Performance;

namespace EcclesiaCast.Core.Tests.Performance;

public class LowPerformanceTests
{
    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Only_a_one_turns_the_mode_on(string? saved, bool expected)
    {
        Assert.Equal(expected, LowPerformance.IsOn(saved));
    }

    [Fact]
    public void The_installer_answer_is_copied_when_nothing_was_decided_yet()
    {
        Assert.Equal("1", LowPerformance.InitialSetting(savedSetting: null, installerValue: "1"));
    }

    [Fact]
    public void The_installer_answer_never_overrides_a_choice_made_in_the_program()
    {
        Assert.Null(LowPerformance.InitialSetting(savedSetting: "0", installerValue: "1"));
        Assert.Null(LowPerformance.InitialSetting(savedSetting: "1", installerValue: "1"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("")]
    [InlineData("quizás")]
    public void Without_a_yes_from_the_installer_nothing_is_stored(string? installerValue)
    {
        Assert.Null(LowPerformance.InitialSetting(savedSetting: null, installerValue: installerValue));
    }
}
