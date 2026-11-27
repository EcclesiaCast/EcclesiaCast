using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

[Collection("ViewModels")]
public sealed class SettingsViewModelTests : IDisposable
{
    private readonly ViewModelWorld _world = new();

    public void Dispose() => _world.Dispose();

    private sealed class Info(PlatformSystem system, bool everything) : IPlatformInfo
    {
        public PlatformSystem System => system;

        public bool IsAvailable(Feature feature) => everything;
    }

    private sealed class Colors(string? picked) : IColorPicker
    {
        public string Transparent => "Transparente";

        public string? Pick(string? currentHex, bool allowTransparent = false) => picked;
    }

    private SettingsViewModel Create(PlatformSystem system, bool everything) =>
        new(_world.Build(), new Info(system, everything));

    [Fact]
    public void On_windows_the_texts_and_sections_are_the_usual_ones()
    {
        var settings = Create(PlatformSystem.Windows, everything: true);

        Assert.True(settings.ShowUpdateCheck);
        Assert.Equal("Actualizaciones", settings.UpdatesTitle);
        Assert.Contains("salida de audio de Windows", settings.SoundText);
        Assert.EndsWith("Los videos de YouTube siguen solos la salida de Windows.", settings.SoundText);
        // The hidden switch of «Seguir la canción» is off in a new library.
        Assert.False(settings.ShowFollowSong);
    }

    [Fact]
    public void On_a_mac_what_it_does_not_have_is_not_offered()
    {
        var settings = Create(PlatformSystem.Mac, everything: false);

        Assert.False(settings.ShowFollowSong);
        Assert.False(settings.ShowUpdateCheck);
        Assert.Equal("Contador de uso", settings.UpdatesTitle);
        Assert.Contains("salida de audio de la Mac", settings.SoundText);
        Assert.DoesNotContain("Windows", settings.SoundText);
        Assert.DoesNotContain("YouTube", settings.SoundText);
    }

    [Theory]
    [InlineData("#102030", "#102030")]
    [InlineData(null, null)]
    public void The_colour_of_the_new_videos_changes_only_when_one_is_picked(string? picked, string? expected)
    {
        var settings = Create(PlatformSystem.Mac, everything: false);
        var before = settings.Main.NewVideoFitColor;

        settings.PickNewVideoColor(new Colors(picked));

        Assert.Equal(expected ?? before, settings.Main.NewVideoFitColor);
    }

    [Fact]
    public void Restarting_the_sound_says_so()
    {
        var main = _world.Build();
        var generation = AudioOutputChanges.Generation;

        main.RestartSound();

        Assert.True(AudioOutputChanges.Generation > generation);
        Assert.StartsWith("Sonido reiniciado", main.StatusText);
    }
}
