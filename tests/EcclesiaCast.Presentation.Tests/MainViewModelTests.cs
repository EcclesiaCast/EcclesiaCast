using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Timing;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

// The view models keep some static state (MediaLookup, the light-copy status),
// so these tests do not run at the same time as each other.
[Collection("ViewModels")]
public sealed class MainViewModelTests
{
    private static Song TwoSectionSong() => new()
    {
        Title = "Canción de prueba",
        Artist = "Nadie",
        Sections =
        [
            new SongSection { Order = 0, Label = "Verso 1", Text = "Primera línea\nSegunda línea" },
            new SongSection { Order = 1, Label = "Coro", Text = "Cantemos todos\nal Señor" },
        ],
    };

    [Fact]
    public void The_whole_view_model_is_built_from_a_library_and_made_up_services()
    {
        using var world = new ViewModelWorld();

        var vm = world.Build();

        Assert.NotNull(vm.Projection);
        Assert.Empty(vm.Slides);
        // A main screen, a second one and the «only the stream» entry.
        Assert.Equal(3, vm.Displays.Count);
        Assert.Equal("screen-2", vm.SelectedDisplay?.Info.DeviceName);
    }

    [Fact]
    public void Opening_a_song_builds_its_slides()
    {
        using var world = new ViewModelWorld();
        world.Songs.Save(TwoSectionSong());
        var vm = world.Build();

        vm.SelectedSong = vm.Songs.Single();

        Assert.Equal(["Verso 1", "Coro"], vm.Slides.Select(s => s.Label));
        Assert.Equal("Primera línea\nSegunda línea", vm.Slides[0].Slide.MainText.Replace("\r\n", "\n"));
        Assert.True(vm.IsSongOpen);
    }

    [Fact]
    public void Projecting_a_slide_changes_the_state_of_the_projection()
    {
        using var world = new ViewModelWorld();
        world.Songs.Save(TwoSectionSong());
        var vm = world.Build();
        vm.SelectedSong = vm.Songs.Single();
        Assert.Null(vm.Projection.Slide);
        Assert.Equal(OutputState.Clear, vm.Projection.State);

        vm.ProjectSlideCommand.Execute(vm.Slides[1]);

        Assert.Equal(OutputState.Content, vm.Projection.State);
        Assert.Equal(vm.Slides[1].Slide.MainText, vm.Projection.Slide?.MainText);
        Assert.Equal(1, vm.LiveSlideIndex);
    }

    [Fact]
    public void Choosing_a_verse_builds_its_slide_and_projects_it()
    {
        using var world = new ViewModelWorld();
        world.Bibles.Import("Prueba", "PRB", "es", new ParsedBible(
        [
            new ParsedBook(43, "Juan",
            [
                new ParsedVerse(3, 16, "Porque de tal manera amó Dios al mundo."),
                new ParsedVerse(3, 17, "Porque no envió Dios a su Hijo para condenar."),
            ]),
        ]));
        var vm = world.Build();

        vm.SelectedBibleBook = vm.BibleBooksAvailable.Single(b => b.Number == 43);
        vm.SelectChapterCommand.Execute(vm.BibleChapters.Single());

        Assert.Equal(["3:16", "3:17"], vm.Slides.Select(s => s.Label));

        vm.SelectVerseCommand.Execute(vm.BibleVerses.Single(v => v.Number == 17));

        Assert.Contains("no envió Dios", vm.Projection.Slide?.MainText);
        Assert.Contains("Juan 3:17", vm.Projection.Slide?.Caption);
        Assert.Equal(1, vm.LiveSlideIndex);
    }

    [Fact]
    public void A_made_up_clock_moves_the_countdown_of_the_section_on()
    {
        using var world = new ViewModelWorld();
        world.Settings.Set("times.sections", MeetingPlan.Serialize([new MeetingSection("Alabanza", 2)]));
        var vm = world.Build();
        Assert.Equal("Alabanza", vm.CurrentSectionName);
        Assert.Equal(TimeText.Clock(TimeSpan.FromMinutes(2)), vm.SectionClockText);

        vm.ToggleSectionTimerCommand.Execute(null);
        Assert.True(vm.IsSectionRunning);
        var before = vm.SectionClockText;

        // Three more minutes go into the clock itself: the text does not know
        // until the timer ticks, and then it reads the clock again.
        vm.Projection.Clocks.Section.AddMinutes(3);
        Assert.Equal(before, vm.SectionClockText);

        world.Timers.Running(TimeSpan.FromMilliseconds(500)).RaiseTick();

        Assert.NotEqual(before, vm.SectionClockText);
        Assert.False(vm.IsSectionOvertime);
    }
}
