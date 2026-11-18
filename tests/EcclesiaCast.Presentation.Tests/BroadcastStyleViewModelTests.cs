using EcclesiaCast.Core.Remote;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

[Collection("ViewModels")]
public sealed class BroadcastStyleViewModelTests : IDisposable
{
    private readonly ViewModelWorld _world = new();
    private readonly BroadcastStyles _before = BroadcastStyleSettings.Current;

    public BroadcastStyleViewModelTests() => BroadcastStyleSettings.Load(_world.Settings);

    public void Dispose()
    {
        BroadcastStyleSettings.Preview(_before);
        _world.Dispose();
    }

    private BroadcastStyleViewModel New(string? pickedColor = "#ABCDEF") =>
        new(_world.Settings, new FixedColorPicker(pickedColor));

    [Fact]
    public void Starts_on_the_bible_layer_with_what_is_saved()
    {
        var vm = New();

        Assert.Equal(OutputChannel.Bible, vm.Channel);
        Assert.Equal(BroadcastStyle.Default(OutputChannel.Bible), vm.Current);
    }

    [Fact]
    public void Each_layer_keeps_its_own_draft()
    {
        var vm = New();
        vm.Edit(vm.Current with { FontSize = 70 });
        vm.Channel = OutputChannel.Songs;
        Assert.Equal(BroadcastStyle.Default(OutputChannel.Songs).FontSize, vm.Current.FontSize);

        vm.Channel = OutputChannel.Bible;
        Assert.Equal(70, vm.Current.FontSize);
    }

    [Fact]
    public void Edit_clamps_the_numbers()
    {
        var vm = New();
        vm.Edit(vm.Current with { PlateOpacity = 500, WidthPercent = 1 });

        Assert.Equal(100, vm.Current.PlateOpacity);
        Assert.Equal(BroadcastStyle.MinWidthPercent, vm.Current.WidthPercent);
    }

    [Fact]
    public void Nothing_is_written_until_Save()
    {
        var vm = New();
        vm.Edit(vm.Current with { FontSize = 70 });

        Assert.Null(_world.Settings.Get(BroadcastStyles.SettingKey(OutputChannel.Bible)));
        Assert.Equal(BroadcastStyle.Default(OutputChannel.Bible), BroadcastStyleSettings.Current.Bible);
    }

    [Fact]
    public void Save_writes_only_the_layers_that_changed_and_applies_them()
    {
        var vm = New();
        vm.Channel = OutputChannel.Songs;
        vm.Edit(vm.Current with { FontSize = 66 });
        var changed = 0;
        BroadcastStyleSettings.Changed += Count;
        try
        {
            vm.Save();
        }
        finally
        {
            BroadcastStyleSettings.Changed -= Count;
        }

        Assert.Equal(1, changed);
        Assert.Null(_world.Settings.Get(BroadcastStyles.SettingKey(OutputChannel.Bible)));
        Assert.Null(_world.Settings.Get(BroadcastStyles.SettingKey(OutputChannel.Announcements)));
        Assert.Equal(66, BroadcastStyles.Load(_world.Settings).Songs.FontSize);
        Assert.Equal(66, BroadcastStyleSettings.Current.Songs.FontSize);

        void Count(object? s, EventArgs e) => changed++;
    }

    [Fact]
    public void Closing_without_saving_puts_the_stream_back_on_what_was_saved()
    {
        var vm = New();
        BroadcastStyleSettings.Preview(BroadcastStyleSettings.Current.With(
            OutputChannel.Bible, BroadcastStyle.Default(OutputChannel.Bible) with { FontSize = 99 }));

        vm.Closed();

        Assert.Equal(BroadcastStyle.Default(OutputChannel.Bible), BroadcastStyleSettings.Current.Bible);
    }

    [Fact]
    public void Closing_after_saving_leaves_the_stream_alone()
    {
        var vm = New();
        vm.Edit(vm.Current with { FontSize = 70 });
        vm.Save();

        vm.Closed();

        Assert.Equal(70, BroadcastStyleSettings.Current.Bible.FontSize);
    }

    [Fact]
    public void A_preset_is_applied_over_the_open_layer()
    {
        var vm = New();
        var preset = BroadcastPresets.All[1];

        vm.ApplyPreset(preset);

        Assert.True(preset.Matches(vm.Current));
    }

    [Fact]
    public void Defaults_bring_back_the_factory_design_of_the_open_layer_only()
    {
        var vm = New();
        vm.Edit(vm.Current with { FontSize = 70 });
        vm.Channel = OutputChannel.Songs;
        vm.Edit(vm.Current with { FontSize = 71 });

        vm.ResetToDefaults();

        Assert.Equal(BroadcastStyle.Default(OutputChannel.Songs), vm.Current);
        vm.Channel = OutputChannel.Bible;
        Assert.Equal(70, vm.Current.FontSize);
    }

    [Fact]
    public void A_picked_colour_goes_into_the_colour_that_was_asked_for()
    {
        var vm = New("#ABCDEF");

        Assert.True(vm.PickColor("Accent"));
        Assert.Equal("#ABCDEF", vm.Current.AccentColor);
        Assert.True(vm.PickColor("BoxBorder"));
        Assert.Equal("#ABCDEF", vm.Current.BoxBorderColor);
        Assert.True(vm.PickColor("Anything else"));
        Assert.Equal("#ABCDEF", vm.Current.BackgroundColor);
    }

    [Fact]
    public void Cancelling_the_colour_picker_changes_nothing()
    {
        var vm = New(pickedColor: null);
        var before = vm.Current;

        Assert.False(vm.PickColor("Text"));
        Assert.Equal(before, vm.Current);
    }

    [Fact]
    public void A_file_turns_the_background_to_a_file_and_clearing_makes_it_transparent()
    {
        var vm = New();

        vm.SetBackgroundFile(@"C:\fondos\luz.mp4");
        Assert.Equal(BroadcastBackground.File, vm.Current.Background);
        Assert.Equal(@"C:\fondos\luz.mp4", vm.Current.BackgroundFile);

        vm.ClearBackground();
        Assert.Equal(BroadcastBackground.None, vm.Current.Background);
    }
}
