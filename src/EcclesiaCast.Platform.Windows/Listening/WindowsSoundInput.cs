using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.Platform.Windows.Listening;

/// <summary>The sound inputs Windows has, behind <see cref="ISoundInput"/>.</summary>
public sealed class WindowsSoundInput : ISoundInput
{
    public IReadOnlyList<SoundInputDevice> Devices() => SoundInput.Devices();
}
