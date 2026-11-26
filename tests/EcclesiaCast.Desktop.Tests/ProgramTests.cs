using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Desktop.Platform;
using EcclesiaCast.Desktop.Services;
using EcclesiaCast.Platform.Mac;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace EcclesiaCast.Desktop.Tests;

[CollectionDefinition("Programa", DisableParallelization = true)]
public sealed class ProgramCollection;

/// <summary>
/// The Avalonia program put together without a window (nothing is shown):
/// the composition root makes the real MainViewModel, the one stand-in for
/// the missing dialogs answers without hanging, the Mac folders are where
/// the plan says, and an unplugged projector is chosen again when it comes
/// back.
/// </summary>
[Collection("Programa")]
public sealed class ProgramTests
{
    private const string Variable = "ECCLESIACAST_DATA_DIR";

    private static readonly DisplayInfo Mac = new("Pantalla integrada", 0, 0, 2880, 1800, IsPrimary: true, Scale: 2);
    private static readonly DisplayInfo Projector = new("Proyector", 2880, 0, 1920, 1080, IsPrimary: false);

    [AvaloniaFact]
    public void The_composition_root_makes_the_whole_main_view_model()
    {
        using var program = new TestProgram();

        var vm = program.Services.GetRequiredService<MainViewModel>();

        // The real services, not stand-ins, behind the projection.
        Assert.IsType<ProjectionWindowService>(program.Services.GetRequiredService<IProjectionWindowService>());
        Assert.IsType<StageWindowService>(program.Services.GetRequiredService<IStageWindowService>());
        Assert.Same(vm.Projection, program.Services.GetRequiredService<ProjectionViewModel>());

        // The Bibles and the sample backgrounds that come with the program
        // are in the library from the first start, as in Windows.
        Assert.NotEmpty(vm.BibleVersionOptions);
        var samples = MediaLookup.All().Where(m => m.Type == MediaType.Video).ToList();
        Assert.NotEmpty(samples);
        vm.SelectedMediaTab = samples[0].Category;
        Assert.Contains(vm.MediaItems, m => m.Type == MediaType.Video);

        // The projector is offered first, as the second screen.
        Assert.Equal("Proyector", vm.SelectedDisplay?.Info.DeviceName);
        Assert.Equal(PlatformSystem.Mac == program.Services.GetRequiredService<IPlatformInfo>().System,
            OperatingSystem.IsMacOS());
    }

    [AvaloniaFact]
    public void A_song_goes_on_the_output_and_the_output_goes_off_with_its_screen()
    {
        using var program = new TestProgram();
        var vm = program.Services.GetRequiredService<MainViewModel>();
        var songs = program.Services.GetRequiredService<ISongRepository>();
        var song = new Song { Title = "Prueba" };
        song.Sections.Add(new SongSection { Label = "Estrofa", Text = "Primera línea\nSegunda línea" });
        songs.Save(song);
        vm.SearchText = "Prueba";
        vm.SelectedSong = vm.Songs.Single();

        vm.ProjectSlideCommand.Execute(vm.Slides[0]);

        Assert.True(vm.IsProjecting);
        Assert.Equal("Primera línea\nSegunda línea", vm.Projection.Slide?.MainText);

        program.Screens.Set(Mac);

        Assert.False(vm.IsProjecting);
        Assert.Contains("Se desconectó la pantalla de la salida", vm.StatusText);

        // Back again: chosen by itself, and one click projects again.
        program.Screens.Set(Mac, Projector);
        Assert.Equal("Proyector", vm.SelectedDisplay?.Info.DeviceName);
        vm.ToggleOutputCommand.Execute(null);
        Assert.True(vm.IsProjecting);
    }

    [AvaloniaFact]
    public void The_test_operator_window_drives_the_view_model_and_Esc_switches_the_output_off()
    {
        using var program = new TestProgram();
        var vm = program.Services.GetRequiredService<MainViewModel>();
        var window = new Views.TestOperatorWindow(vm);
        window.Show();
        try
        {
            vm.ToggleOutputCommand.Execute(null);
            Assert.True(vm.IsProjecting);

            window.KeyPressQwerty(Avalonia.Input.PhysicalKey.Escape, Avalonia.Input.RawInputModifiers.None);

            Assert.False(vm.IsProjecting);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void Every_missing_dialog_says_so_and_answers_as_cancelled()
    {
        using var notices = new RecordingNotices();

        Assert.Null(NotOnMacYet.For<ISongEditor>().Edit(null));
        Assert.False(NotOnMacYet.For<IThemeManagerDialog>().Show());
        Assert.Empty(NotOnMacYet.For<IWebSongSearchDialog>().Show());
        Assert.Null(NotOnMacYet.For<IDialogs>().OpenFile(new OpenFileRequest("Elegir", [])));

        var prompt = NotOnMacYet.For<ITextPrompt>();
        Assert.Null(prompt.Ask("Nombre", "¿Cómo se llama?"));
        Assert.Equal(-1, prompt.Choose("Elegir", "¿Cuál?", ["Una", "Otra"]));
        Assert.Equal((-1, false), prompt.ChooseAndRemember("Elegir", "¿Cuál?", ["Una", "Otra"], "Recordar"));
        Assert.Null(prompt.AskTwo("Dos", "Uno", "", "Dos", ""));
        Assert.Null(NotOnMacYet.For<ICountdownDialog>().Show(CountdownSettings.Default, isRunning: false));

        Assert.Equal(9, notices.Shown.Count);
        Assert.All(notices.Shown, message => Assert.Equal(NotOnMacYet.Message, message));
    }

    [Fact]
    public void A_function_that_is_never_offered_answers_without_a_notice()
    {
        using var notices = new RecordingNotices();

        var updates = NotOnMacYet.Silent<IUpdates>();
        updates.PendingChanged += (_, _) => { };
        Assert.Null(updates.Pending);
        Assert.False(updates.CheckOnStartup);
        Assert.True(updates.CheckNowAsync().IsCompletedSuccessfully);
        Assert.False(NotOnMacYet.Silent<ISpeechEar>().HasModel);

        Assert.Empty(notices.Shown);
    }

    [Fact]
    public void The_Mac_library_is_in_Application_Support_unless_the_variable_says_otherwise()
    {
        var before = Environment.GetEnvironmentVariable(Variable);
        try
        {
            Environment.SetEnvironmentVariable(Variable, null);
            var paths = new MacPlatformPaths();
            var support = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            Assert.Equal(Path.Combine(support, "EcclesiaCast"), paths.DataFolder);
            if (OperatingSystem.IsMacOS())
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                Assert.Equal(Path.Combine(home, "Library", "Application Support", "EcclesiaCast"), paths.DataFolder);
                Assert.Equal(Path.Combine(home, "Movies", "EcclesiaCast"), paths.DownloadsFolder);
            }

            var copy = Path.Combine(Path.GetTempPath(), "una-copia");
            Environment.SetEnvironmentVariable(Variable, copy);
            paths = new MacPlatformPaths();
            Assert.Equal(copy, paths.DataFolder);
            Assert.Equal(Path.Combine(copy, "copias-livianas"), paths.LightCopiesFolder);
            // The helper programs are shared by every copy, as on Windows.
            Assert.Equal(Path.Combine(support, "EcclesiaCast", "tools"), paths.ToolsFolder);
        }
        finally
        {
            Environment.SetEnvironmentVariable(Variable, before);
        }
    }

    [Fact]
    public void What_comes_with_the_program_is_in_the_bundle_s_Resources()
    {
        var root = Path.Combine(Path.GetTempPath(), "ecclesiacast-app-" + Guid.NewGuid().ToString("N"));
        var program = Path.Combine(root, "EcclesiaCast.app", "Contents", "MacOS");
        var resources = Path.Combine(root, "EcclesiaCast.app", "Contents", "Resources");
        try
        {
            Directory.CreateDirectory(program);
            Assert.Equal(program, MacPlatformPaths.BundledFolderFor(program));

            Directory.CreateDirectory(resources);
            Assert.Equal(resources, MacPlatformPaths.BundledFolderFor(program));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>The program over a temporary library and made-up screens.</summary>
    private sealed class TestProgram : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "ecclesiacast-desktop-" + Guid.NewGuid().ToString("N"));
        private readonly string? _variableBefore = Environment.GetEnvironmentVariable(Variable);
        private readonly IVideoEngine? _videoBefore = PlatformServices.Video;
        private readonly INotices? _noticesBefore = PlatformServices.Notices;

        public TestProgram()
        {
            Environment.SetEnvironmentVariable(Variable, _root);
            Screens.Set(Mac, Projector);
            var platform = new DesktopPlatform(new MacPlatformPaths(), new MacOutputWindowNative(), new MacPowerGuard(),
                Screens, new PrimaryHoldsTheOperator(), new RecordingNotices(), Video: null);

            var dbPath = Composition.PrepareLibrary(platform.Paths.DataFolder);
            Services = Composition.Build(platform, dbPath);
            Composition.Start(Services);
        }

        public OutputWindowTests.FakeScreens Screens { get; } = new();

        public ServiceProvider Services { get; }

        public void Dispose()
        {
            Services.GetService<MainViewModel>()?.StopRemote();
            Services.Dispose();
            PlatformServices.Video = _videoBefore;
            PlatformServices.Notices = _noticesBefore;
            Environment.SetEnvironmentVariable(Variable, _variableBefore);
            SqliteConnection.ClearAllPools();
            try
            {
                Directory.Delete(_root, recursive: true);
            }
            catch (IOException)
            {
                // A temporary folder left behind is not worth failing a test.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private sealed class PrimaryHoldsTheOperator : IOperatorScreen
    {
        public bool Holds(DisplayInfo display) => display.IsPrimary;
    }

    /// <summary>Answers every notice at once (the first button) and writes down what it said.</summary>
    private sealed class RecordingNotices : INotices, IDisposable
    {
        private readonly INotices? _before = PlatformServices.Notices;

        public RecordingNotices() => PlatformServices.Notices = this;

        public List<string> Shown { get; } = [];

        public int Ask(string message, string? title, NoticeKind kind, IReadOnlyList<string> buttons,
            int defaultIndex, int cancelIndex, bool destructive)
        {
            Shown.Add(message);
            return defaultIndex;
        }

        public void Dispose() => PlatformServices.Notices = _before;
    }
}
