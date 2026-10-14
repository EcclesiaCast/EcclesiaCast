using EcclesiaCast.Core.Ui;

namespace EcclesiaCast.Core.Tests.Ui;

public class OperatorLayoutTests
{
    [Fact]
    public void Only_the_word_complex_turns_the_advanced_layout_on()
    {
        Assert.True(OperatorLayout.IsAdvanced("complex"));
    }

    [Theory]
    [InlineData("learning")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Complex")]
    [InlineData("quizás")]
    public void Anything_else_is_the_learning_layout_the_program_always_had(string? saved)
    {
        Assert.False(OperatorLayout.IsAdvanced(saved));
    }

    [Fact]
    public void Each_choice_is_stored_as_the_word_that_reads_back_to_it()
    {
        Assert.Equal("complex", OperatorLayout.ToSetting(advanced: true));
        Assert.Equal("learning", OperatorLayout.ToSetting(advanced: false));
        Assert.True(OperatorLayout.IsAdvanced(OperatorLayout.ToSetting(advanced: true)));
        Assert.False(OperatorLayout.IsAdvanced(OperatorLayout.ToSetting(advanced: false)));
    }
}
