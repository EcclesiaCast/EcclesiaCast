using System.Reflection;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Presentation.Tests;

// Made-up platform services, written by hand. The ones a test looks at (the
// clock, the dispatcher, the folders, the screens) are real classes; the many
// the view model only holds on to (dialogs, players, windows) are Stub.Of<T>().

/// <summary>Runs at once, on the calling thread: no window, no queue.</summary>
internal sealed class InlineDispatcher : IUiDispatcher
{
    public bool CheckAccess() => true;

    public void Post(Action action) => action();

    public void Post(Action action, UiPriority priority) => action();

    public Task InvokeAsync(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    public Task<T> InvokeAsync<T>(Func<T> function) => Task.FromResult(function());
}

/// <summary>A timer that only ticks when the test says so.</summary>
internal sealed class ManualTimer(TimeSpan interval) : IUiTimer
{
    public TimeSpan Interval { get; set; } = interval;

    public bool IsEnabled { get; private set; }

    public event EventHandler? Tick;

    public void Start() => IsEnabled = true;

    public void Stop() => IsEnabled = false;

    public void RaiseTick() => Tick?.Invoke(this, EventArgs.Empty);
}

internal sealed class ManualTimers : IUiTimerFactory
{
    public List<ManualTimer> Created { get; } = [];

    public IUiTimer Create(TimeSpan interval)
    {
        var timer = new ManualTimer(interval);
        Created.Add(timer);
        return timer;
    }

    /// <summary>The timer made with this interval that is running.</summary>
    public ManualTimer Running(TimeSpan interval) =>
        Created.Single(t => t.IsEnabled && t.Interval == interval);
}

/// <summary>Every folder inside one temporary folder; the bundled one does not exist.</summary>
internal sealed class TempPaths(string root) : IPlatformPaths
{
    public string DataFolder { get; } = Path.Combine(root, "data");

    public string DownloadsFolder { get; } = Path.Combine(root, "downloads");

    public string ToolsFolder { get; } = Path.Combine(root, "tools");

    public string ModelsFolder { get; } = Path.Combine(root, "models");

    public string LightCopiesFolder { get; } = Path.Combine(root, "light");

    public string BundledFolder { get; } = Path.Combine(root, "bundled");
}

/// <summary>A main screen and a second one, like a church with a projector.</summary>
internal sealed class TwoScreens : IDisplayProvider
{
    public IReadOnlyList<DisplayInfo> GetDisplays() =>
    [
        new DisplayInfo("screen-1", 0, 0, 1920, 1080, IsPrimary: true),
        new DisplayInfo("screen-2", 1920, 0, 1920, 1080, IsPrimary: false),
    ];
}

/// <summary>No pictures and no fonts: the view model only passes them along.</summary>
internal sealed class NoImages : IImageFactory
{
    public object? FromFile(string path, int? decodeWidth = null) => null;

    public object? FromBytes(byte[] bytes) => null;

    public (int Width, int Height)? ReadSize(byte[] bytes) => null;
}

internal sealed class NoFonts : IFontCatalog
{
    public IReadOnlyList<string> InstalledFamilies() => ["Arial"];
}

/// <summary>Everything fits in one slide, with the labels applied like the real one does.</summary>
internal sealed class FitsInOneSlide : ITextMeasurer
{
    public IReadOnlyList<(string Main, string? Secondary)> SplitToFit(
        SlideContent slide, Func<string, string>? label = null, Func<string, string>? secondaryLabel = null,
        int maxParts = 6) =>
        [(label?.Invoke(slide.MainText) ?? slide.MainText,
            slide.SecondaryText is null ? null : secondaryLabel?.Invoke(slide.SecondaryText) ?? slide.SecondaryText)];
}

/// <summary>The control key is never down.</summary>
internal sealed class NoKeys : IKeyboardState
{
    public bool IsControlDown => false;
}

/// <summary>A list is left as it is: no screen to filter on.</summary>
internal sealed class UnfilteredLists : IListFilter
{
    public void Apply<T>(System.Collections.ObjectModel.ObservableCollection<T> list, Func<T, bool>? filter)
    {
    }
}

/// <summary>
/// An object that implements any interface by answering «nothing» to
/// everything: empty text, empty lists, completed tasks, zero, null. For the
/// services the view model holds but a test never uses.
/// </summary>
public class Stub : DispatchProxy
{
    public static T Of<T>() where T : class => Create<T, Stub>();

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
        Nothing(targetMethod!.ReturnType);

    private static object? Nothing(Type type)
    {
        if (type == typeof(void))
            return null;
        if (type == typeof(string))
            return string.Empty;
        if (type == typeof(Task))
            return Task.CompletedTask;
        if (type.IsArray)
            return Array.CreateInstance(type.GetElementType()!, 0);
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var argument = type.GetGenericArguments()[0];
            if (definition == typeof(Task<>))
            {
                return typeof(Task).GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(argument).Invoke(null, [Nothing(argument)]);
            }

            if (definition == typeof(IReadOnlyList<>) || definition == typeof(IEnumerable<>)
                || definition == typeof(IReadOnlyCollection<>) || definition == typeof(IList<>)
                || definition == typeof(ICollection<>))
                return Array.CreateInstance(argument, 0);
        }

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}

/// <summary>
/// Answers the program's notices with the button the test chose and writes
/// down what was asked. Set it as <c>PlatformServices.Notices</c> (the tests
/// that use it live in the «ViewModels» collection, which never runs in parallel).
/// </summary>
internal sealed class ScriptedNotices : INotices, IDisposable
{
    private readonly Queue<Func<IReadOnlyList<string>, int>> _answers = new();
    private readonly INotices? _before = PlatformServices.Notices;

    public ScriptedNotices() => PlatformServices.Notices = this;

    /// <summary>Every notice shown, in order.</summary>
    public List<(string Message, string? Title, IReadOnlyList<string> Buttons)> Asked { get; } = [];

    /// <summary>The next question is answered «yes» (the last button of a two-button question).</summary>
    public ScriptedNotices Yes() => Answer(buttons => buttons.Count - 1);

    /// <summary>The next question is answered «no» / cancel (the first button).</summary>
    public ScriptedNotices No() => Answer(_ => 0);

    public ScriptedNotices Answer(Func<IReadOnlyList<string>, int> answer)
    {
        _answers.Enqueue(answer);
        return this;
    }

    public int Ask(string message, string? title, NoticeKind kind, IReadOnlyList<string> buttons,
        int defaultIndex, int cancelIndex, bool destructive)
    {
        Asked.Add((message, title, buttons));
        return _answers.Count > 0 ? _answers.Dequeue()(buttons) : cancelIndex;
    }

    public void Dispose() => PlatformServices.Notices = _before;
}

/// <summary>Posters made of nothing: writes down what was asked, and answers with a made-up path.</summary>
internal sealed class RecordingThumbnailer : IThumbnailer
{
    public string Directory => "posters";

    public List<string> Created { get; } = [];

    public List<string?> Deleted { get; } = [];

    public string? Create(string sourcePath, EcclesiaCast.Core.Media.MediaType type, LibVLCSharp.Shared.LibVLC? videoEngine)
    {
        Created.Add(sourcePath);
        return "poster-of-" + Path.GetFileName(sourcePath);
    }

    public void Delete(string? thumbnailPath) => Deleted.Add(thumbnailPath);

    public void DeleteOrphans(IEnumerable<string?> referenced)
    {
    }
}

/// <summary>A colour picker that always picks the same colour (null: the operator cancelled).</summary>
internal sealed class FixedColorPicker(string? picked) : IColorPicker
{
    public string? AskedWith { get; private set; }

    public string Transparent => "transparent";

    public string? Pick(string? currentHex, bool allowTransparent = false)
    {
        AskedWith = currentHex;
        return picked;
    }
}
