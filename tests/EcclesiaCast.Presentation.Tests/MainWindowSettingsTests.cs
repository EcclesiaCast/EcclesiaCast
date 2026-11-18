using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Presentation.Tests;

public sealed class MainWindowSettingsTests : IDisposable
{
    private readonly ViewModelWorld _world = new();

    private MainWindowSettings New() => new(_world.Settings);

    public void Dispose() => _world.Dispose();

    [Fact]
    public void A_size_is_written_in_whole_pixels_and_read_back()
    {
        var settings = New();

        settings.WriteSize(MainWindowSettings.LibraryWidthKey, 270.4);
        settings.WriteSize(MainWindowSettings.MediaHeightKey, 171.6);

        Assert.Equal("270", _world.Settings.Get("layout.library.width"));
        Assert.Equal(270, settings.ReadSize(MainWindowSettings.LibraryWidthKey));
        Assert.Equal(172, settings.ReadSize(MainWindowSettings.MediaHeightKey));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ancho")]
    [InlineData("0")]
    [InlineData("-30")]
    public void A_size_that_makes_no_sense_reads_as_nothing(string? saved)
    {
        if (saved is not null)
            _world.Settings.Set("layout.preview.width", saved);

        Assert.Null(New().ReadSize(MainWindowSettings.PreviewWidthKey));
    }

    [Fact]
    public void Each_size_has_its_own_setting_name()
    {
        Assert.Equal(
            ["layout.library.width", "layout.biblegrid.width", "layout.preview.width", "layout.playlist.height", "layout.media.height"],
            [MainWindowSettings.LibraryWidthKey, MainWindowSettings.BibleGridWidthKey, MainWindowSettings.PreviewWidthKey,
                MainWindowSettings.PlaylistHeightKey, MainWindowSettings.MediaHeightKey]);
    }

    [Fact]
    public void A_choice_is_saved_as_1_or_0_and_nothing_saved_means_no()
    {
        var settings = New();
        Assert.False(settings.ReadFlag(MainWindowSettings.MirroredKey));

        settings.WriteFlag(MainWindowSettings.MirroredKey, true);
        Assert.Equal("1", _world.Settings.Get("layout.mirrored"));
        Assert.True(settings.ReadFlag(MainWindowSettings.MirroredKey));

        settings.WriteFlag(MainWindowSettings.MirroredKey, false);
        Assert.Equal("0", _world.Settings.Get("layout.mirrored"));
        Assert.False(settings.ReadFlag(MainWindowSettings.MirroredKey));
    }

    [Fact]
    public void Anything_but_1_reads_as_no()
    {
        _world.Settings.Set("layout.media.top", "true");

        Assert.False(New().ReadFlag(MainWindowSettings.MediaTopKey));
    }

    [Fact]
    public void The_fold_names_are_the_ones_saved_by_earlier_versions()
    {
        Assert.Equal(
            [
                "layout.mirrored", "layout.playlist.top", "layout.media.top", "layout.live.bottom", "layout.output.split",
                "layout.library.collapsed", "layout.playlist.collapsed", "layout.media.collapsed",
                "layout.tools.collapsed", "layout.left.rail", "layout.audiolist.collapsed",
            ],
            [
                MainWindowSettings.MirroredKey, MainWindowSettings.PlaylistTopKey, MainWindowSettings.MediaTopKey,
                MainWindowSettings.LiveBottomKey, MainWindowSettings.OutputSplitKey,
                MainWindowSettings.LibraryCollapsedKey, MainWindowSettings.PlaylistCollapsedKey,
                MainWindowSettings.MediaCollapsedKey, MainWindowSettings.ToolsCollapsedKey,
                MainWindowSettings.LeftRailKey, MainWindowSettings.AudioListCollapsedKey,
            ]);
    }

    [Fact]
    public void The_window_is_written_as_size_and_state_and_read_back()
    {
        var settings = New();

        settings.WriteWindow(new WindowBounds(1280.4, 720.6, Maximized: false));
        Assert.Equal("1280;721;normal", _world.Settings.Get("layout.window"));
        Assert.Equal(new WindowBounds(1280, 721, false), settings.ReadWindow());

        settings.WriteWindow(new WindowBounds(1000, 600, Maximized: true));
        Assert.Equal("1000;600;max", _world.Settings.Get("layout.window"));
        Assert.Equal(new WindowBounds(1000, 600, true), settings.ReadWindow());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1280;720")]
    [InlineData("1280;720;normal;extra")]
    [InlineData("ancho;720;normal")]
    [InlineData("1280;alto;max")]
    public void A_window_setting_that_cannot_be_read_gives_nothing(string? saved)
    {
        if (saved is not null)
            _world.Settings.Set("layout.window", saved);

        Assert.Null(New().ReadWindow());
    }

    [Fact]
    public void Any_state_but_max_is_a_normal_window()
    {
        _world.Settings.Set("layout.window", "900;500;whatever");

        Assert.Equal(new WindowBounds(900, 500, false), New().ReadWindow());
    }

    [Fact]
    public void The_layout_mode_is_learning_unless_complex_was_saved()
    {
        var settings = New();
        Assert.False(settings.ReadAdvancedUi());

        settings.WriteAdvancedUi(true);
        Assert.Equal("complex", _world.Settings.Get("ui.mode"));
        Assert.True(settings.ReadAdvancedUi());

        settings.WriteAdvancedUi(false);
        Assert.Equal("learning", _world.Settings.Get("ui.mode"));
        Assert.False(settings.ReadAdvancedUi());
    }
}
