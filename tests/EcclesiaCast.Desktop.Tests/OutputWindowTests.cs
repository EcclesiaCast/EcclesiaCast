using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Core.Timing;
using EcclesiaCast.Desktop.Controls;
using EcclesiaCast.Desktop.Platform;
using EcclesiaCast.Desktop.Services;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// The output and the stage windows with their services, drawn in memory by
/// Avalonia.Headless (nothing opens on the screen): the size on a Retina
/// screen, the system kept awake while projecting, and what happens when the
/// screen of the output is unplugged and plugged in again.
/// </summary>
public sealed class OutputWindowTests
{
    private static readonly DisplayInfo Mac = new("Pantalla integrada", 0, 0, 2880, 1800, IsPrimary: true, Scale: 2);
    private static readonly DisplayInfo Projector = new("Proyector", 2880, 0, 1920, 1080, IsPrimary: false);

    [AvaloniaFact]
    public void The_output_covers_its_screen_in_points_and_keeps_the_system_awake()
    {
        var world = new World(Mac, Projector);

        world.Output.EnsureVisible(Mac);

        var window = world.Output.Window!;
        Assert.True(world.Output.IsOutputVisible);
        Assert.Equal(1440, window.Width);
        Assert.Equal(900, window.Height);
        Assert.False(window.ShowActivated);
        Assert.Equal(1, world.Power.On);

        world.Output.HideOutput();

        Assert.False(world.Output.IsOutputVisible);
        Assert.Equal(1, world.Power.Off);
    }

    [AvaloniaFact]
    public void Unplugging_the_projector_switches_the_output_off_and_says_so()
    {
        var world = new World(Mac, Projector);
        var lost = new List<string>();
        var changes = 0;
        world.Output.OutputScreenLost += (_, name) => lost.Add(name);
        world.Output.VisibilityChanged += (_, _) => changes++;

        world.Output.EnsureVisible(Projector);
        changes = 0;

        world.Screens.Set(Mac);
        Dispatcher.UIThread.RunJobs();

        Assert.False(world.Output.IsOutputVisible);
        Assert.Equal(["Proyector"], lost);
        Assert.True(changes > 0, "El programa no se enteró de que la salida se apagó.");
        Assert.Equal(1, world.Power.Off);

        // Plugged in again: projecting works as always.
        world.Screens.Set(Mac, Projector);
        world.Output.EnsureVisible(Projector);

        Assert.True(world.Output.IsOutputVisible);
        Assert.Equal(2, world.Power.On);
    }

    [AvaloniaFact]
    public void Another_screen_coming_and_going_leaves_the_output_alone()
    {
        var world = new World(Mac, Projector);
        var lost = 0;
        world.Output.OutputScreenLost += (_, _) => lost++;
        world.Output.EnsureVisible(Projector);

        world.Screens.Set(Mac, Projector, new DisplayInfo("Monitor del escenario", 4800, 0, 1280, 720, false));
        world.Screens.Set(Projector);

        Assert.True(world.Output.IsOutputVisible);
        Assert.Equal(0, lost);
    }

    [AvaloniaFact]
    public void The_projector_back_in_another_mode_takes_the_window_with_it()
    {
        var world = new World(Mac, Projector);
        world.Output.EnsureVisible(Projector);

        world.Screens.Set(Mac, Projector with { Width = 1280, Height = 720 });

        Assert.True(world.Output.IsOutputVisible);
        Assert.Equal(1280, world.Output.Window!.Width);
        Assert.Equal(720, world.Output.Window.Height);
    }

    [AvaloniaFact]
    public void Unplugging_the_stage_screen_switches_the_stage_off_and_says_so()
    {
        var world = new World(Mac, Projector);
        var lost = new List<string>();
        world.Stage.StageScreenLost += (_, name) => lost.Add(name);

        world.Stage.ShowOn(Projector, StageLayoutStore.Defaults()[0]);
        Assert.True(world.Stage.IsVisible);

        world.Screens.Set(Mac);

        Assert.False(world.Stage.IsVisible);
        Assert.Equal(["Proyector"], lost);
    }

    [AvaloniaFact]
    public void The_stage_window_draws_every_box_of_its_layout()
    {
        var world = new World(Mac, Projector);
        foreach (var layout in StageLayoutStore.Defaults())
        {
            world.Stage.ShowOn(Projector, layout);

            Assert.True(world.Stage.IsVisible);
            Assert.Equal(layout.Boxes.Count, world.Stage.Window!.View.Boxes.Count);
        }
    }

    /// <summary>The two services over made-up screens.</summary>
    private sealed class World
    {
        public World(params DisplayInfo[] displays)
        {
            Screens.Set(displays);
            var projection = new ProjectionViewModel(new PresentationService(), new MeetingClocks());
            Output = new ProjectionWindowService(projection, new MacNativeOrNothing(), new FirstScreenHoldsTheOperator(), Power, Screens);
            Stage = new StageWindowService(projection, new MacNativeOrNothing(), Screens);
        }

        public FakeScreens Screens { get; } = new();

        public CountingPower Power { get; } = new();

        public ProjectionWindowService Output { get; }

        public StageWindowService Stage { get; }
    }

    internal sealed class FakeScreens : IScreenList
    {
        private IReadOnlyList<DisplayInfo> _displays = [];

        public event EventHandler? Changed;

        public IReadOnlyList<DisplayInfo> GetDisplays() => _displays;

        public void Set(params DisplayInfo[] displays)
        {
            _displays = displays;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    internal sealed class CountingPower : IPowerGuard
    {
        public int On { get; private set; }

        public int Off { get; private set; }

        public void Start()
        {
        }

        public void BeginProjecting() => On++;

        public void EndProjecting() => Off++;
    }

    private sealed class FirstScreenHoldsTheOperator : IOperatorScreen
    {
        public bool Holds(DisplayInfo display) => display.IsPrimary;
    }

    /// <summary>The Mac calls: here they do nothing, on a Mac they find no real window.</summary>
    private sealed class MacNativeOrNothing : IOutputWindowNative
    {
        private readonly EcclesiaCast.Platform.Mac.MacOutputWindowNative _mac = new();

        public void Prepare(NativeWindowHandle window) => _mac.Prepare(window);

        public void Place(NativeWindowHandle window, DisplayInfo display) => _mac.Place(window, display);
    }
}
