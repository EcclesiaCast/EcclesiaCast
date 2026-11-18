using EcclesiaCast.Core.Media;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

// These do not reach the internet: the search itself (the address, the
// answers of the service) belongs to MainViewModel; what is tested here is
// what the window decides around it.
[Collection("ViewModels")]
public sealed class PixabayViewModelTests
{
    private const string Welcome = "Buscá un fondo arriba.";

    private static PixabayHit Video(long id, string? author = "Ana", PixabayFile? file = null) =>
        new(id, "video", 1920, 1080, 12.4, "https://cdn.pixabay.com/t.jpg", author, null, null, file);

    private static PixabayHit Photo(long id) =>
        new(id, "foto", 4000, 3000, 0, "https://cdn.pixabay.com/p.jpg", " ", null, null, null);

    [Fact]
    public void Before_any_search_the_middle_shows_the_welcome_text()
    {
        using var world = new ViewModelWorld();
        var vm = new PixabayViewModel(world.Build(), Welcome);

        Assert.Equal(Welcome, vm.CenterMessage);
        Assert.Equal("Bajar los elegidos", vm.AddText);
        Assert.False(vm.CanAdd);
        Assert.False(vm.HasMore);
    }

    [Fact]
    public async Task A_search_of_less_than_two_letters_only_says_so()
    {
        using var world = new ViewModelWorld();
        var vm = new PixabayViewModel(world.Build(), Welcome);

        await vm.SearchAsync(" a ", photos: false, newSearch: true);

        Assert.Equal("Escribí qué fondo buscás.", vm.StatusText);
        Assert.Equal(Welcome, vm.CenterMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Changing_the_kind_with_nothing_to_search_does_nothing()
    {
        using var world = new ViewModelWorld();
        var vm = new PixabayViewModel(world.Build(), Welcome);

        await vm.KindChangedAsync(" x", photos: true);

        Assert.Equal(string.Empty, vm.StatusText);
    }

    [Fact]
    public void A_card_reads_like_it_always_did()
    {
        var video = new PixabayCardViewModel(Video(1), alreadyHave: true);
        var photo = new PixabayCardViewModel(Photo(2), alreadyHave: false);

        Assert.True(video.HasDuration);
        Assert.Equal("de Ana", video.AuthorLine);
        Assert.Equal("Video de 1920×1080, 0:12. Clic para elegirlo.", video.Tip);
        Assert.True(video.AlreadyHave);
        Assert.Equal("https://cdn.pixabay.com/t.jpg", video.ThumbnailUrl);

        Assert.False(photo.HasDuration);
        Assert.Equal("Pixabay", photo.AuthorLine);
        Assert.Equal("Foto de 4000×3000. Clic para elegirla.", photo.Tip);
    }

    [Fact]
    public void The_button_to_download_follows_the_cards_chosen()
    {
        using var world = new ViewModelWorld();
        var vm = new PixabayViewModel(world.Build(), Welcome);
        var first = new PixabayCardViewModel(Video(1), false);
        var second = new PixabayCardViewModel(Photo(2), false);
        vm.Cards.Add(first);
        vm.Cards.Add(second);
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        first.IsSelected = true;
        Assert.Equal("Bajar 1", vm.AddText);
        Assert.True(vm.CanAdd);

        second.IsSelected = true;
        Assert.Equal("Bajar 2", vm.AddText);
        Assert.Contains(nameof(PixabayViewModel.AddText), changed);

        first.IsSelected = false;
        second.IsSelected = false;
        Assert.Equal("Bajar los elegidos", vm.AddText);
        Assert.False(vm.CanAdd);
    }

    [Fact]
    public async Task Downloading_with_nothing_chosen_does_nothing()
    {
        using var world = new ViewModelWorld();
        var vm = new PixabayViewModel(world.Build(), Welcome);

        await vm.AddSelectedAsync();

        Assert.Equal(string.Empty, vm.StatusText);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task A_result_with_no_file_to_download_counts_as_failed_and_stays_chosen_no_more()
    {
        using var world = new ViewModelWorld();
        var vm = new PixabayViewModel(world.Build(), Welcome);
        var card = new PixabayCardViewModel(Video(1, file: null), false) { IsSelected = true };
        vm.Cards.Add(card);

        await vm.AddSelectedAsync();

        Assert.Equal("No se pudo bajar. Revisá la conexión y probá de nuevo.", vm.StatusText);
        Assert.False(card.IsSelected);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task A_result_already_in_the_library_says_all_of_them_were_there()
    {
        using var world = new ViewModelWorld();
        var main = world.Build();
        var hit = Video(7, file: new PixabayFile("https://cdn.pixabay.com/v.mp4", 1920, 1080));
        // No search was made, so the name is built with an empty query.
        var path = main.PixabayFilePath(hit, string.Empty);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, [1]);
        main.ImportMediaFiles([path]);
        Assert.True(main.HasMediaFile(path));
        var vm = new PixabayViewModel(main, Welcome);
        vm.Cards.Add(new PixabayCardViewModel(hit, true) { IsSelected = true });

        await vm.AddSelectedAsync();

        Assert.Equal("Ya los tenías todos.", vm.StatusText);
    }

    [Fact]
    public void The_file_of_a_result_is_kept_inside_the_library()
    {
        using var world = new ViewModelWorld();
        var main = world.Build();
        var hit = Video(7, file: new PixabayFile("https://cdn.pixabay.com/v.mp4", 1920, 1080));

        var path = main.PixabayFilePath(hit, "mar");

        Assert.Equal("fondos-pixabay", Path.GetFileName(Path.GetDirectoryName(path)));
        Assert.Equal(PixabayBackgrounds.FileName(hit, "mar"), Path.GetFileName(path));
    }
}
