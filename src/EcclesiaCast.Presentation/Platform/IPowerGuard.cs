namespace EcclesiaCast.Presentation.Platform;

/// <summary>
/// Keeps the system from slowing the program down or going to sleep while
/// it projects. A two-hour service with nobody touching the computer must
/// not dim the screen, and the output window — which never takes the
/// keyboard, so the program is never «in front» — must keep its pace.
/// </summary>
public interface IPowerGuard
{
    /// <summary>Once, at start-up: what holds for the whole run.</summary>
    void Start();

    /// <summary>The output came on. Calling it again while on does nothing.</summary>
    void BeginProjecting();

    /// <summary>The output went off. Calling it again while off does nothing.</summary>
    void EndProjecting();
}
