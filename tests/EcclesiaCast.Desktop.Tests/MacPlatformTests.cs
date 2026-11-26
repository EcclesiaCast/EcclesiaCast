using EcclesiaCast.Core.Displays;
using EcclesiaCast.Platform.Mac;
using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// What Platform.Mac does without a window: on any other system its calls
/// return without touching anything; on a Mac, a handle that is not there
/// is noted and left alone (never a crash).
/// </summary>
public sealed class MacPlatformTests
{
    private static readonly DisplayInfo Projector = new("Proyector", 1920, 0, 1920, 1080, IsPrimary: false);

    [Fact]
    public void The_output_native_calls_do_nothing_without_a_window()
    {
        var native = new MacOutputWindowNative();

        native.Prepare(default);
        native.Place(default, Projector);
        native.Place(new NativeWindowHandle(IntPtr.Zero, "NSWindow"), Projector);
        MacOutputWindowNative.Describe(default, "prueba");
    }
}
